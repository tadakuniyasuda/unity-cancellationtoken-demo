using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Cancellation lesson: one script, three parts. Self-contained.
///
///   Part A: Token basics. Cancel() is only a REQUEST; work must listen.
///   Part B: UniTaskCompletionSource. A task YOU complete by hand (TrySetResult / TrySetException / TrySetCanceled).
///   Part C: race in miniature: reproduce the R1 hang, then fix it two ways.
///
/// Add to an empty GameObject, press Play, use the buttons. Console: Collapse OFF.
/// </summary>
public class CancellationLesson : MonoBehaviour
{
    // ==================================================================
    // Part A: token basics
    // ==================================================================

    CancellationTokenSource _workCts;

    void StartWork(bool respectsToken)
    {
        _workCts?.Cancel();
        _workCts?.Dispose();
        _workCts = new CancellationTokenSource();
        WorkAsync(respectsToken, _workCts.Token).Forget();
    }

    void CancelWork()
    {
        Warn(">>> A: Cancel() pressed <<<");
        _workCts?.Cancel();
    }

    async UniTaskVoid WorkAsync(bool respectsToken, CancellationToken ct)
    {
        ResetClock();
        Log($"A: work START ({(respectsToken ? "passes ct to Delay" : "IGNORES ct")})");
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                await UniTask.Delay(1000, cancellationToken: respectsToken ? ct : CancellationToken.None);
                Log($"A: step {i}/5   (ct.IsCancellationRequested = {ct.IsCancellationRequested})");
            }
            Log("A: work COMPLETED");
        }
        catch (OperationCanceledException)
        {
            Warn("A: OperationCanceledException thrown at the await -> work stopped");
        }
    }

    // ==================================================================
    // Part B: UniTaskCompletionSource
    // ==================================================================

    UniTaskCompletionSource<int> _tcs;

    void CreateTcsAndWaiter()
    {
        ResetClock();
        _tcs = new UniTaskCompletionSource<int>();
        WaitOnTcsAsync(_tcs).Forget();
    }

    async UniTaskVoid WaitOnTcsAsync(UniTaskCompletionSource<int> tcs)
    {
        Log("B: waiter is now awaiting tcs.Task ... (it waits until SOMEONE completes it)");
        try
        {
            int value = await tcs.Task;
            Log($"B: waiter woke up with RESULT {value}");
        }
        catch (OperationCanceledException)
        {
            Warn("B: waiter woke up with OperationCanceledException");
        }
        catch (Exception ex)
        {
            Error($"B: waiter woke up with EXCEPTION: {ex.Message}");
        }
    }

    void TcsSetResult()
    {
        if (_tcs == null) return;
        bool ok = _tcs.TrySetResult(42);
        Log($"B: TrySetResult(42) returned {ok}");
    }

    void TcsSetException()
    {
        if (_tcs == null) return;
        bool ok = _tcs.TrySetException(new InvalidOperationException("simulated network error"));
        Log($"B: TrySetException(...) returned {ok}");
    }

    void TcsSetCanceled()
    {
        if (_tcs == null) return;
        bool ok = _tcs.TrySetCanceled();
        Log($"B: TrySetCanceled() returned {ok}");
    }

    // ==================================================================
    // Part C: Race, in miniature
    // ==================================================================

    enum RaceMode
    {
        Buggy,                     // round 1 OCE swallowed, nobody told
        WithTrySetCanceled,        // round 2: command runner reports global cancel
        WithAttachExternal,        // + main loop can stop WAITING even if the command ignores ct
        WithAttachExternalAndCancel // + main loop cancels the losers itself before leaving
    }

    const int CommandSteps = 8;    // command takes 8s
    const int TimerMs = 6000;      // timer fires at 6s -> normally the timer wins (TIME UP)

    CancellationTokenSource _globalCts = new CancellationTokenSource();   // = currentScenarioCts
    bool _commandIgnoresToken;

    void GlobalReset()
    {
        Warn(">>> GLOBAL RESET (ResetToTitle) <<<");
        _globalCts.Cancel();
        _globalCts.Dispose();
        _globalCts = new CancellationTokenSource();
    }

    async UniTaskVoid RunRaceAsync(RaceMode mode, CancellationToken ct)
    {
        ResetClock();
        bool ignores = _commandIgnoresToken;
        Log($"=== RACE START  mode={mode}  command {(ignores ? "IGNORES" : "respects")} its token ===");

        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var raceTcs = new UniTaskCompletionSource<int>();
        bool cleanedUp = false;

        // ---- runner 1: the command ----
        async UniTaskVoid FireCommandAsync()
        {
            try
            {
                await FakeCommandAsync(ignores ? CancellationToken.None : raceCts.Token);
                cleanedUp = true;
                bool ok = raceTcs.TrySetResult(0);
                Log($"Command runner: finished -> TrySetResult(0) returned {ok}");
            }
            catch (OperationCanceledException)
            {
                cleanedUp = true;
                if (mode != RaceMode.Buggy && ct.IsCancellationRequested)
                {
                    bool ok = raceTcs.TrySetCanceled(ct);
                    Log($"Command runner: caught OCE, global cancel -> TrySetCanceled returned {ok}");
                }
                else
                {
                    Warn("Command runner: caught OCE and told NOBODY");
                }
            }
        }

        // ---- runner 2: the timer ----
        async UniTaskVoid FireTimerAsync()
        {
            try
            {
                await UniTask.Delay(TimerMs, cancellationToken: raceCts.Token);
                bool ok = raceTcs.TrySetResult(1);
                Log($"Timer runner: time up -> TrySetResult(1) returned {ok}");
            }
            catch (OperationCanceledException)
            {
                Log("Timer runner: cancelled (lost the race, or reset) -> told nobody");
            }
        }

        FireCommandAsync().Forget();
        FireTimerAsync().Forget();

        // ---- main loop ----
        try
        {
            UniTask<int> waitForWinner = raceTcs.Task;
            if (mode == RaceMode.WithAttachExternal || mode == RaceMode.WithAttachExternalAndCancel)
                waitForWinner = waitForWinner.AttachExternalCancellation(ct);   // stop WAITING on reset

            Log("Main: awaiting raceTcs.Task ...");
            int winner = await waitForWinner;
            Log(winner == 0 ? "Main: command won" : "Main: TIME UP (timer won)");

            raceCts.Cancel();   // stop the loser

            bool waitCanceled = await UniTask.WaitUntil(() => cleanedUp, cancellationToken: ct)
                                             .SuppressCancellationThrow();
            Log(waitCanceled
                ? "Main: stopped waiting for cleanup (SuppressCancellationThrow returned true)"
                : "Main: command cleanup confirmed");

            ct.ThrowIfCancellationRequested();
            Log("=== RACE FINISHED -> next command ===");
        }
        catch (OperationCanceledException)
        {
            if (mode == RaceMode.WithAttachExternalAndCancel)
                raceCts.Cancel();   // stop the losers ourselves before leaving (don't rely on the link)

            Warn("=== Main: RACE ABORTED -> back to title ===");
        }
    }

    async UniTask FakeCommandAsync(CancellationToken ct)
    {
        try
        {
            for (int i = 1; i <= CommandSteps; i++)
            {
                await UniTask.Delay(1000, cancellationToken: ct);
                Log($"Command: working {i}/{CommandSteps}");
            }
        }
        finally
        {
            Log("Command: finally (cleanup) ran");
        }
    }

    void OnDestroy()
    {
        _workCts?.Cancel();
        _workCts?.Dispose();
        _globalCts.Cancel();
        _globalCts.Dispose();
    }

    // ==================================================================
    // UI (IMGUI, no setup)
    // ==================================================================

    Vector2 _scroll;
    GUIStyle _h, _b, _t;

    void OnGUI()
    {
        if (_b == null)
        {
            _h = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _b = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            _t = new GUIStyle(GUI.skin.toggle) { fontSize = 17 };
        }

        GUILayout.BeginArea(new Rect(16, 16, 560, Screen.height - 32));
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("A: token basics", _h);
        if (Btn("A1: Start work (respects token)")) StartWork(true);
        if (Btn("A2: Start work (ignores token)")) StartWork(false);
        if (Btn("A: Cancel")) CancelWork();

        GUILayout.Space(12);
        GUILayout.Label("B: UniTaskCompletionSource", _h);
        if (Btn("B: Create TCS + start waiter")) CreateTcsAndWaiter();
        if (Btn("B: TrySetResult(42)")) TcsSetResult();
        if (Btn("B: TrySetException")) TcsSetException();
        if (Btn("B: TrySetCanceled")) TcsSetCanceled();

        GUILayout.Space(12);
        GUILayout.Label("C: race", _h);
        _commandIgnoresToken = GUILayout.Toggle(_commandIgnoresToken, "  Command ignores its token (like ConnectAsync)", _t);
        if (Btn("C1: Run race (BUGGY: OCE swallowed)")) RunRaceAsync(RaceMode.Buggy, _globalCts.Token).Forget();
        if (Btn("C2: Run race (+ TrySetCanceled)")) RunRaceAsync(RaceMode.WithTrySetCanceled, _globalCts.Token).Forget();
        if (Btn("C3: Run race (+ AttachExternalCancellation)")) RunRaceAsync(RaceMode.WithAttachExternal, _globalCts.Token).Forget();
        if (Btn("C4: Run race (+ AttachExternal + cancel losers)")) RunRaceAsync(RaceMode.WithAttachExternalAndCancel, _globalCts.Token).Forget();
        if (Btn("GLOBAL RESET (ResetToTitle)")) GlobalReset();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    bool Btn(string text) => GUILayout.Button(text, _b, GUILayout.Height(40));

    // ==================================================================
    // Logging: [elapsed | frame]
    // ==================================================================

    readonly Stopwatch _clock = Stopwatch.StartNew();
    void ResetClock() => _clock.Restart();
    string Stamp(string m) => $"[{_clock.Elapsed.TotalSeconds:F2}s | f{Time.frameCount}] {m}";
    void Log(string m) => Debug.Log(Stamp(m));
    void Warn(string m) => Debug.LogWarning(Stamp(m));
    void Error(string m) => Debug.LogError(Stamp(m));
}
