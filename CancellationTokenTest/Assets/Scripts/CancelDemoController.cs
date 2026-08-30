using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CancelDemoController : MonoBehaviour
{
    [SerializeField] private Button startUncancellableButton;
    [SerializeField] private Button startCancellableButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text logText;

    private CancellationTokenSource cts;

    private void Start()
    {
        startUncancellableButton.onClick.AddListener(() => RunUncancellableLoop().Forget());
        startCancellableButton.onClick.AddListener(() => RunCancellableLoop().Forget());
        cancelButton.onClick.AddListener(CancelRunningLoop);
    }

    private void Log(string message)
    {
        Debug.Log(message);
        logText.text += message + "\n";
    }

    // デモA: キャンセル手段なし。一度始めたら最後まで止まらない。
    private async UniTask RunUncancellableLoop()
    {
        Log("Uncancellable loop started. Try clicking Cancel — nothing happens.");

        for (int i = 1; i <= 10; i++)
        {
            await UniTask.Delay(500);
            Log($"Uncancellable tick {i}/10");
        }

        Log("Uncancellable loop finished naturally.");
    }

    // デモB: CancellationTokenを渡すことで、外部から止められる。
    private async UniTask RunCancellableLoop()
    {
        cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        Log("Cancellable loop started. Click Cancel anytime to stop it.");

        try
        {
            for (int i = 1; i <= 10; i++)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Delay(500, cancellationToken: token);
                Log($"Cancellable tick {i}/10");
            }

            Log("Cancellable loop finished naturally.");
        }
        catch (OperationCanceledException)
        {
            Log("Cancellable loop was cancelled early.");
        }
    }

    private void CancelRunningLoop()
    {
        if (cts == null)
        {
            Log("Nothing to cancel yet.");
            return;
        }

        cts.Cancel();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}