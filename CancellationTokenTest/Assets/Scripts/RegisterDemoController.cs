using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegisterDemoController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private Button registerAfterCancelButton;
    [SerializeField] private TMP_Text logText;

    private CancellationTokenSource cts;
    private IDisposable registration;

    private void Start()
    {
        startButton.onClick.AddListener(() => RunTask().Forget());
        cancelButton.onClick.AddListener(CancelRunningTask);
        registerAfterCancelButton.onClick.AddListener(RegisterAfterCancelTest);
    }

    private void Log(string message)
    {
        Debug.Log(message);
        logText.text += message + "\n";
    }

    private async UniTask RunTask()
    {
        cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // ここがRegisterの核心:
        // 「キャンセルされたら、このコードを自動で呼んで」と事前に予約しておく
        registration = token.Register(() =>
        {
            Log("Register callback fired — cleanup ran automatically.");
        });

        Log("Task started. Click Cancel to see Register fire on its own.");

        try
        {
            for (int i = 1; i <= 10; i++)
            {
                await UniTask.Delay(500, cancellationToken: token);
                Log($"Tick {i}/10");
            }
            Log("Task finished naturally.");
        }
        catch (OperationCanceledException)
        {
            Log("Task loop exited via exception.");
        }
        finally
        {
            registration.Dispose();
        }
    }

    private void CancelRunningTask()
    {
        if (cts == null)
        {
            Log("Nothing running yet.");
            return;
        }

        cts.Cancel();
    }

    private void RegisterAfterCancelTest()
    {
        if (cts == null)
        {
            Log("No token exists yet. Start and Cancel a task first.");
            return;
        }

        Log("Calling Register on an already-cancelled token...");
        cts.Token.Register(() => Log("Late Register fired!"));
        Log("Register() call has returned.");
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}