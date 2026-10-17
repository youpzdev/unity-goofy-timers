using System;
using UnityEngine;

public class TimerHandle
{
    private readonly WeakReference<UnityEngine.Object> target;

    private bool active = true;
    private bool isPaused;

    private float remaining;

    internal readonly Action Callback;
    internal readonly bool UnscaledTime;
    internal float EndTime;
    internal float Interval;
    internal bool Repeat;
    internal bool EveryFrame;
    internal bool RestartedDuringTick;

    internal bool IsAlive => target == null || target.TryGetTarget(out var owner) && owner != null;

    public bool IsActive => active && IsAlive;
    public bool IsPaused => IsActive && isPaused;
    public float Remaining => !IsActive || EveryFrame ? 0 : isPaused ? remaining : Mathf.Max(0, EndTime - Timer.GetTime(UnscaledTime));

    internal TimerHandle(Action callback, UnityEngine.Object owner, bool unscaledTime)
    {
        Callback = callback;
        UnscaledTime = unscaledTime;
        target = ReferenceEquals(owner, null) ? null : new WeakReference<UnityEngine.Object>(owner);
    }

    public void Cancel() {active = false;}

    public bool Restart() {return Restart(Interval);}
    public bool Restart(float delay) {return Timer.Restart(this, delay);}

    public void Pause()
    {
        if (!IsActive || isPaused) return;
        remaining = Mathf.Max(0, EndTime - Timer.GetTime(UnscaledTime));
        isPaused = true;
    }

    public void Resume()
    {
        if (!IsActive || !isPaused) return;
        EndTime = Timer.GetTime(UnscaledTime) + remaining;
        isPaused = false;
    }

    internal void RestartTimer(float delay)
    {
        active = true;
        Interval = delay;
        EndTime = Timer.GetTime(UnscaledTime) + delay;
        if (isPaused) remaining = delay;
    }
}
