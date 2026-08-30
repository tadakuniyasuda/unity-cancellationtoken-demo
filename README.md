# Unity CancellationTokenSource Demo

A minimal Unity project demonstrating `CancellationTokenSource` and `CancellationToken` with UniTask — two loops, one Cancel button, side by side.

Written to accompany the blog post: [CancellationTokenSource in Unity: Why "Stop" Doesn't Always Stop](https://www.ta-dah.tech/blog/cancellationtokensource-in-unity-why-stop-doesnt-always-stop) 

## What this shows

- A loop with **no cancellation path** — clicking Cancel does nothing, it always runs to completion.
- A loop that **checks a `CancellationToken`** at two points — before each tick, and inside the delay itself — and stops immediately when cancelled.
- The difference between `CancellationTokenSource` (the thing you call `.Cancel()` on) and `CancellationToken` (the read-only value you pass into async methods).

## How to run

1. Open the project in Unity (tested on `6.0(6000.0.77f1)`).
2. Open the sample scene: `Assets/Scenes/CancellationTokenSource-Demo.unity`.
3. Press Play.
4. Click **Start Uncancellable Loop**, then click **Cancel** — notice it keeps running.
5. Click **Start Cancellable Loop**, then click **Cancel** — notice it stops immediately.

## Try breaking it

In `CancelDemoController.cs`, find this line inside `RunCancellableLoop()`:

```csharp
await UniTask.Delay(500, cancellationToken: token);
```

Remove the `cancellationToken: token` argument and re-run the cancellable loop. Cancellation still technically works, but only at the top of the next iteration — up to 500ms late. That gap is the entire lesson this project exists to show.

## Requirements

- Unity `6.0(6000.0.77f1)`
- [UniTask](https://github.com/Cysharp/UniTask)

## License

MIT — use however you like.
