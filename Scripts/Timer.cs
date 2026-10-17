using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class Timer
{
    private static readonly List<TimerHandle> timers = new();

    private static float pauseStartTime;
    private static float pauseStartUnscaledTime;
    private static float pausedTime;
    private static float pausedUnscaledTime;

    private static bool paused;
    private static bool ticking;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CancelAll();
        paused = false;
        pausedTime = pausedUnscaledTime = 0;
        Application.quitting -= Cleanup;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var loop = PlayerLoop.GetCurrentPlayerLoop();
        InjectInto<Update>(ref loop, Tick);
        PlayerLoop.SetPlayerLoop(loop);
        Application.quitting -= Cleanup;
        Application.quitting += Cleanup;
    }

    public static Action After(float delay, Action callback, UnityEngine.Object target = null) {return Schedule(delay, callback, target).Cancel;}

    public static Action Repeat(float interval, Action callback, UnityEngine.Object target = null) {return Every(interval, callback, target).Cancel;}

    public static Action EveryFrame(Action callback, UnityEngine.Object target = null) {return OnUpdate(callback, target).Cancel;}

    public static TimerHandle Schedule(float delay, Action callback, UnityEngine.Object target = null, bool unscaledTime = false) {return Add(delay, callback, target, unscaledTime, false, false);}

    public static TimerHandle Every(float interval, Action callback, UnityEngine.Object target = null, bool unscaledTime = false) {return Add(interval, callback, target, unscaledTime, true, false);}

    public static TimerHandle OnUpdate(Action callback, UnityEngine.Object target = null, bool unscaledTime = false) {return Add(0, callback, target, unscaledTime, true, true);}

    public static void CancelAll()
    {
        foreach (var timer in timers) timer.Cancel();
        if (!ticking) timers.Clear();
    }

    public static void Pause()
    {
        if (paused) return;
        pauseStartTime = Time.time;
        pauseStartUnscaledTime = Time.unscaledTime;
        paused = true;
    }

    public static void Resume()
    {
        if (!paused) return;
        pausedTime += Time.time - pauseStartTime;
        pausedUnscaledTime += Time.unscaledTime - pauseStartUnscaledTime;
        paused = false;
    }

    internal static float GetTime(bool unscaledTime)
    {
        if (unscaledTime) return (paused ? pauseStartUnscaledTime : Time.unscaledTime) - pausedUnscaledTime;

        return (paused ? pauseStartTime : Time.time) - pausedTime;
    }

    internal static bool Restart(TimerHandle timer, float delay)
    {
        ValidateDelay(delay, nameof(delay));
        if (timer.EveryFrame && delay != 0)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Timer.OnUpdate: задержка должна быть равна нулю");
        if (!timer.IsAlive) return false;

        timer.RestartTimer(delay);
        timer.RestartedDuringTick = ticking;
        if (!timers.Contains(timer)) timers.Add(timer);

        return true;
    }

    private static TimerHandle Add(float delay, Action callback, UnityEngine.Object target, bool unscaledTime, bool repeat, bool everyFrame)
    {
        ValidateDelay(delay, repeat ? "interval" : "delay");
        if (callback == null) throw new ArgumentNullException(nameof(callback), "Timer: обработчик не задан");

        var timer = new TimerHandle(callback, target, unscaledTime)
        {
            EndTime = GetTime(unscaledTime) + delay,
            Interval = delay,
            Repeat = repeat,
            EveryFrame = everyFrame
        };
        timers.Add(timer);

        return timer;
    }

    private static void ValidateDelay(float delay, string parameter)
    {
        if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0)
            throw new ArgumentOutOfRangeException(parameter, delay, "Timer: время должно быть конечным и не меньше нуля");
    }

    private static void Tick() {ProcessTimers(GetTime(false), GetTime(true));}

    private static void ProcessTimers(float scaledNow, float unscaledNow)
    {
        if (paused || ticking) return;
        ticking = true;
        int count = timers.Count;

        try
        {
            for (int i = 0; i < count && !paused; i++)
            {
                var timer = timers[i];
                if (!timer.IsActive || timer.IsPaused || timer.RestartedDuringTick) continue;

                float now = timer.UnscaledTime ? unscaledNow : scaledNow;
                if (!timer.EveryFrame && now < timer.EndTime) continue;

                if (timer.Repeat) timer.EndTime = now + timer.Interval;
                else timer.Cancel();

                try {timer.Callback();}
                catch (Exception e) {Debug.LogException(e);}
            }
        }
        finally
        {
            ticking = false;
            timers.RemoveAll(timer => {timer.RestartedDuringTick = false; return !timer.IsActive;});
        }
    }

    private static void Cleanup() {ResetStatics();}

    private static void InjectInto<T>(ref PlayerLoopSystem loop, PlayerLoopSystem.UpdateFunction fn)
    {
        if (loop.subSystemList == null) return;
        for (int i = 0; i < loop.subSystemList.Length; i++)
        {
            if (loop.subSystemList[i].type != typeof(T)) continue;

            var sub = loop.subSystemList[i];
            var list = new List<PlayerLoopSystem>(sub.subSystemList ?? Array.Empty<PlayerLoopSystem>());
            list.RemoveAll(system => system.type == typeof(Timer));
            list.Add(new PlayerLoopSystem { type = typeof(Timer), updateDelegate = fn });
            sub.subSystemList = list.ToArray();
            loop.subSystemList[i] = sub;

            return;
        }
    }
}
