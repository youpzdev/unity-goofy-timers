using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class Timer
{
    private class TimerEntry
    {
        public float endTime;
        public float interval;
        public bool repeat;
        public Action callback;
        public bool cancelled;
        public WeakReference<UnityEngine.Object> target;
    }

    private static readonly List<TimerEntry> timers = new();
    private static readonly List<TimerEntry> everyFrameTimers = new();

    private static float pauseStartTime;
    private static bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var loop = PlayerLoop.GetCurrentPlayerLoop();
        InjectInto<Update>(ref loop, Tick);
        PlayerLoop.SetPlayerLoop(loop);
        Application.quitting += Cleanup;
    }

    public static Action After(float delay, Action callback, UnityEngine.Object target = null)
    {
        var entry = new TimerEntry
        {
            endTime = Time.time + delay,
            callback = callback,
            target = target != null ? new WeakReference<UnityEngine.Object>(target) : null
        };
        timers.Add(entry);
        return () => entry.cancelled = true;
    }

    public static Action Repeat(float interval, Action callback, UnityEngine.Object target = null)
    {
        var entry = new TimerEntry
        {
            endTime = Time.time + interval,
            interval = interval,
            repeat = true,
            callback = callback,
            target = target != null ? new WeakReference<UnityEngine.Object>(target) : null
        };
        timers.Add(entry);
        return () => entry.cancelled = true;
    }

    public static Action EveryFrame(Action callback, UnityEngine.Object target = null)
    {
        var entry = new TimerEntry
        {
            callback = callback,
            target = target != null ? new WeakReference<UnityEngine.Object>(target) : null
        };
        everyFrameTimers.Add(entry);
        return () => entry.cancelled = true;
    }

    public static void CancelAll()
    {
        timers.Clear();
        everyFrameTimers.Clear();
    }

    public static void Pause()
    {
        if (paused) return;
        paused = true;
        pauseStartTime = Time.time;
    }

    public static void Resume()
    {
        if (!paused) return;
        float pauseDuration = Time.time - pauseStartTime;
        foreach (var entry in timers) entry.endTime += pauseDuration;
        paused = false;
    }

    private static bool IsAlive(TimerEntry entry)
    {
        if (entry.target == null) return true;
        return entry.target.TryGetTarget(out var obj) && obj != null;
    }

    private static void Tick()
    {
        if (paused) return;

        float now = Time.time;

        for (int i = 0; i < timers.Count; i++)
        {
            var entry = timers[i];

            if (entry.cancelled || !IsAlive(entry))
            {
                timers[i] = timers[timers.Count - 1];
                timers.RemoveAt(timers.Count - 1);
                i--;
                continue;
            }

            if (now < entry.endTime) continue;

            try { entry.callback?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }

            if (entry.repeat)
                entry.endTime = now + entry.interval;
            else
            {
                timers[i] = timers[timers.Count - 1];
                timers.RemoveAt(timers.Count - 1);
                i--;
            }
        }

        for (int i = 0; i < everyFrameTimers.Count; i++)
        {
            var entry = everyFrameTimers[i];

            if (entry.cancelled || !IsAlive(entry))
            {
                everyFrameTimers[i] = everyFrameTimers[everyFrameTimers.Count - 1];
                everyFrameTimers.RemoveAt(everyFrameTimers.Count - 1);
                i--;
                continue;
            }

            try { entry.callback?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private static void Cleanup()
    {
        timers.Clear();
        everyFrameTimers.Clear();
    }

    private static void InjectInto<T>(ref PlayerLoopSystem loop, PlayerLoopSystem.UpdateFunction fn)
    {
        for (int i = 0; i < loop.subSystemList.Length; i++)
        {
            if (loop.subSystemList[i].type != typeof(T)) continue;

            var sub = loop.subSystemList[i];
            var list = new List<PlayerLoopSystem>(sub.subSystemList ?? Array.Empty<PlayerLoopSystem>())
            {
                new PlayerLoopSystem { type = typeof(Timer), updateDelegate = fn }
            };
            sub.subSystemList = list.ToArray();
            loop.subSystemList[i] = sub;
            return;
        }
    }
}