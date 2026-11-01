using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

public class UniTaskLinqExample : MonoBehaviour
{
    CancellationTokenSource cts = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RunAsyncLinqExample(cts.Token).Forget();
    }

    async UniTaskVoid RunAsyncLinqExample(CancellationToken token)
    {
        var asyncEnumerable = UniTaskAsyncEnumerable
            .EveryUpdate()
            .Take(10)
            .SelectAwait(async _ =>
            {
                await UniTask.Delay(1000, cancellationToken: token);
                return Time.frameCount;
            })
            .DistinctUntilChanged()
            .ForEachAsync(time => { Debug.Log($"Received time:{time}"); }, token);

        await asyncEnumerable;
        Debug.Log("Async Linq sequence completed");
    }

    void OnDestroy()
    {
        cts.Cancel();
    }
}
