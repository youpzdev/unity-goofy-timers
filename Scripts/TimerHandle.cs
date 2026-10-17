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

    public bool IsActive => active && (target == null || target.TryGetTarget(out var owner) && owner != null);
    public bool IsPaused => IsActive && isPaused;

    internal TimerHandle(Action callback, UnityEngine.Object owner, bool unscaledTime)
    {
        Callback = callback;
        UnscaledTime = unscaledTime;
        target = ReferenceEquals(owner, null) ? null : new WeakReference<UnityEngine.Object>(owner);
    }

    public void Cancel() => active = false;

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
}
