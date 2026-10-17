# Goofy Timers

Static timers for Unity, updated through PlayerLoop on the main thread.
Copy the Scripts folder into Assets. No GameObject or coroutine is needed.
Demo/Scenes/Demo.unity contains the existing example scene.

```csharp
Timer.After(2f, Explode, this);
Action cancel = Timer.Repeat(1f, SpawnEnemy, this);
cancel();

var respawn = Timer.Schedule(3f, Respawn, this);
respawn.Pause();
respawn.Resume();
respawn.Cancel();
bool waiting = respawn.IsActive;

var heartbeat = Timer.Every(1f, Heartbeat, this, unscaledTime: true);
var frame = Timer.OnUpdate(UpdateCountdown, this);
```

After, Repeat and EveryFrame keep their original signatures and Action
cancellation API, including method-group binding to existing delegates. Schedule,
Every and OnUpdate return TimerHandle with Pause, Resume, Cancel, IsActive
and IsPaused. Paused handles remain active. Cancelled and completed handles
cannot be restarted. Calling Pause, Resume or Cancel repeatedly is safe.

Timers use scaled Time.time by default. Pass unscaledTime: true to Schedule
or Every to use Time.unscaledTime, including while timeScale is zero.
EveryFrame and OnUpdate
run once per PlayerLoop update, regardless of timeScale. OnUpdate's unscaledTime
argument only selects the clock used by the handle's pause bookkeeping.

Timer.Pause and Timer.Resume affect all timers, including unscaled timers.
A timer created during the global pause starts its full delay after Resume.
Individual pauses preserve the remaining delay and combine with global pause.

The optional target is a Unity object owner. Destroying it cancels its timers;
disabling it does not. Pass this when the callback belongs to a component.
A timer given an already destroyed owner is inactive immediately.

Callbacks scheduled from another callback start on the next update. Cancel
and CancelAll take effect immediately, including during callback execution.
CancelAll also invalidates existing handles. A callback can schedule a new
timer after CancelAll; the new timer waits until the next update.

A repeating timer executes once when due and schedules its next interval from
that update. Missed intervals are skipped. An interval of zero means once per
update. Zero-delay one-shot timers also wait for the next update. Negative,
NaN or infinite delays and null callbacks throw an argument exception.
Callback exceptions are logged and do not prevent other callbacks from running.

Static state resets when entering Play Mode with domain reload disabled.
PlayerLoop registration replaces the previous Timer callback to prevent
multiple updates after repeated initialization. This API is main-thread only.

Tests/Editor/TimerTests.cs covers callback mutation, owners, cancellation,
individual and global pauses, scaled and unscaled deadlines, and validation.
Tests control only clock inputs; they do not prove PlayerLoop execution in
Play Mode or real audio and scene behavior.

Upstream: https://github.com/youpzdev/unity-goofy-timers

MIT license. See LICENSE.
