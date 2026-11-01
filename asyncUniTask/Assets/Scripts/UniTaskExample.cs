using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UniTaskExample : MonoBehaviour
{
    [SerializeField] TMP_Text progressText;
    [SerializeField] TMP_Text progressText2;
    [SerializeField] TMP_Text progressText3;
    [SerializeField] float progressSpeed = 0.05f;
    float progress = 0f;

    CancellationTokenSource cts = new CancellationTokenSource();

    void Awake()
    {
        progressText.text = "0%";
    }

    void OnDestroy()
    {
        cts.Cancel();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        var token = this.GetCancellationTokenOnDestroy();
        {
            UniTask.WhenAny(
                ExampleUniTask("A", 100, token),
                ExampleAsync(token).AsUniTask(),
                ExampleCoroutine().WithCancellation(token)

                ).Forget();

            Debug.Log("Task completed");
        }
    }

    async UniTask ExampleUniTask(String A, int iterations, CancellationToken token)
    {
        Debug.Log("A");
        for (int i = 0; i < iterations; i++)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Delay(50, cancellationToken:token);
            progress += 1f;
            progressText.text = $"{progress:F2}%";

        }
    }

    async Task ExampleAsync(CancellationToken token)
    {
        for (int i = 0; i < 100; i++)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(50, token);
            progress += 1f;
            progressText2.text = $"{progress:F2}%";

        }
    }

    async Task SomeLongRunningOperationAsync(CancellationToken token)
    {
        for (int i = 0; i < 10; i++)
        {
            token.ThrowIfCancellationRequested();

            Debug.Log($"Processing step {i + 1}");
            await Task.Delay(1000, token);
        }
        Debug.Log("Task completed");
    }

    async Task<int> PerformCalculationAsync(int input)
    {
        await Task.Delay(3000);
        return input * 2;
    }

    int ExpensiveCalculations()
    {
        int result = 0;
        for (int i = 0; i < 100000000; i++)
        {
            result += i;
        }
        return result;
    }

    IEnumerator ExampleCoroutine()
    {
        for (float i = 0; i <= 5f; i += 0.01f)
        {
            

            yield return new WaitForSeconds(progressSpeed);
            progressText3.text = $"{i:F2}%";
            
        }
    }
}
