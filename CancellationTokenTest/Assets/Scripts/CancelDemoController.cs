using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CancelDemoController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button StartCancellableButton;
    [SerializeField] private Button CancelButton;
    [SerializeField] private TMP_Text logText;

    private CancellationTokenSource cts;

    private void Start()
    {
        startButton.onClick.AddListener(() => RunTask().Forget());
        StartCancellableButton.onClick.AddListener(() => RunCancellableTask().Forget());
        CancelButton.onClick.AddListener(CancelRunningTask);
    }

    void Log(string message)
    {
        Debug.Log(message);
        logText.text += message + "\n";
    }

    private async UniTask RunTask()
    {
        Log("Task started. Click Cancel to see Register fire on its own.");
        try
        {
            for (int i = 1; i <= 10; i++)
            {
                await UniTask.Delay(500);
                Log($"Tick {i}/10");
            }
            Log("Task finished naturally.");
        }
        catch (OperationCanceledException)
        {
            Log("Task loop exited via exception.");
        }
    }
    private async UniTask RunCancellableTask()
    {
        cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        Log("Cancellable task started. Click Cancel to see Register fire on its own.");
        try
        {
            for (int i = 1; i <= 10; i++)
            {
                await UniTask.Delay(500, cancellationToken: token);
                Log($"Tick {i}/10");
            }
            Log("Cancellable task finished naturally.");
        }
        catch (OperationCanceledException)
        {
            Log("Cancellable task loop exited via exception.");
        }
    }
    private async void CancelRunningTask()
    {
        if (cts != null)
        {
            cts.Cancel();
            Log("Cancellation requested.");
            cts.Dispose();
            cts = null;
        }
        else
        {
            Log("No cancellable task is running.");
        }
    }
}