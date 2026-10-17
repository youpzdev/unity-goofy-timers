# Goofy Timers

Static timers for Unity using PlayerLoop. Copy Scripts/Timer.cs into Assets.
Demo/Scenes/Demo.unity contains the example scene.

```csharp
Timer.After(2f, Explode, this);
Action cancel = Timer.Repeat(1f, SpawnEnemy, this);
cancel();
```

After and Repeat use scaled Time.time. Pass an owner to cancel callbacks
when its Unity object is destroyed. The runtime requires the Unity main
thread. Known lifecycle issues are recorded in the workspace code review.

Upstream: https://github.com/youpzdev/unity-goofy-timers

MIT license. See LICENSE.
