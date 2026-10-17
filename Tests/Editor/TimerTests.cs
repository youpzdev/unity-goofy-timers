using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TimerTests
{
    private static readonly MethodInfo tick = typeof(Timer).GetMethod("Tick", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo process = typeof(Timer).GetMethod("ProcessTimers", BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void SetUp() {Timer.CancelAll(); Timer.Resume();}

    [TearDown]
    public void TearDown() {Timer.CancelAll(); Timer.Resume();}

    [Test]
    public void CancelAllInsideAfterStopsPendingCallbacksWithoutBreakingTick()
    {
        int calls = 0;
        Timer.After(0, Timer.CancelAll);
        Timer.After(0, () => calls++);

        Assert.DoesNotThrow(Tick, "CancelAll from a callback must not invalidate the active traversal");
        Assert.That(calls, Is.Zero, "CancelAll must cancel later callbacks in the same frame");
    }

    [Test]
    public void CallbackAddedDuringTickWaitsUntilNextTick()
    {
        int calls = 0;
        Timer.After(0, () => Timer.After(0, () => calls++));

        Tick();
        Assert.That(calls, Is.Zero, "A newly scheduled callback must wait until the next frame");
        Tick();
        Assert.That(calls, Is.EqualTo(1), "The pending callback must execute exactly once next frame");
    }

    [Test]
    public void FrameCallbackAddedByAfterWaitsUntilNextTick()
    {
        int calls = 0;
        Timer.After(0, () => Timer.EveryFrame(() => calls++));

        Tick();
        Assert.That(calls, Is.Zero, "Frame callbacks created by After must also wait until the next frame");
        Tick();
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void CancelActionIsIdempotentAndPreventsCallback()
    {
        int calls = 0;
        Action cancel = Timer.After(0, () => calls++);
        cancel();
        cancel();

        Tick();
        Assert.That(calls, Is.Zero, "A cancelled callback must not execute");
    }

    [Test]
    public void LegacyMethodGroupsKeepTheirOriginalDelegateSignatures()
    {
        Func<float, Action, UnityEngine.Object, Action> after = Timer.After;
        Func<float, Action, UnityEngine.Object, Action> repeat = Timer.Repeat;
        Func<Action, UnityEngine.Object, Action> everyFrame = Timer.EveryFrame;
        int calls = 0;
        var cancelAfter = after(0, () => calls++, null);
        var cancelRepeat = repeat(0, () => calls++, null);
        var cancelFrame = everyFrame(() => calls++, null);
        cancelAfter();
        cancelRepeat();
        cancelFrame();

        Tick();
        Assert.That(calls, Is.Zero, "Legacy delegate bindings must retain working cancellation actions");
    }

    [Test]
    public void DestroyedOwnerCancelsTimedAndFrameCallbacks()
    {
        int calls = 0;
        var owner = new GameObject("Timer test owner");
        Timer.After(0, () => calls++, owner);
        Timer.EveryFrame(() => calls++, owner);
        UnityEngine.Object.DestroyImmediate(owner);

        Tick();
        Assert.That(calls, Is.Zero, "Callbacks must not outlive their destroyed Unity owner");
    }

    [Test]
    public void CancellingAnotherTimerDuringCallbackPreventsItsExecution()
    {
        int calls = 0;
        Action cancel = null;
        Timer.After(0, () => cancel());
        cancel = Timer.After(0, () => calls++);

        Tick();
        Assert.That(calls, Is.Zero, "Cancellation must take effect before the next callback in this frame");
    }

    [Test]
    public void HandleCompletesOnceAndCannotBeResumedAfterCompletion()
    {
        int calls = 0;
        var timer = Timer.Schedule(0, () => calls++);
        Assert.That(timer.IsActive, Is.True);

        Tick();
        timer.Pause();
        timer.Resume();
        Tick();

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(timer.IsActive, Is.False);
        Assert.That(timer.IsPaused, Is.False);
    }

    [Test]
    public void HandlePauseFreezesRemainingDelayAndResumeIsIdempotent()
    {
        int calls = 0;
        var timer = Timer.Schedule(5, () => calls++);
        timer.Pause();
        timer.Pause();
        Assert.That(timer.IsPaused, Is.True);
        float remaining = (float)typeof(TimerHandle).GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(timer);
        var offset = typeof(Timer).GetField("pausedTime", BindingFlags.Static | BindingFlags.NonPublic);
        float originalOffset = (float)offset.GetValue(null);

        try
        {
            offset.SetValue(null, originalOffset - 100);
            Process(float.MaxValue, float.MaxValue);
            Assert.That(calls, Is.Zero, "An individually paused timer cannot run at any deadline");
            timer.Resume();
            float deadline = Deadline(timer);
            timer.Resume();
            Assert.That(Deadline(timer), Is.EqualTo(deadline), "Repeated Resume must not move the deadline");
            Process(deadline - 0.01f, 0);
            Assert.That(calls, Is.Zero);
            Process(deadline, 0);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(remaining, Is.InRange(4.9f, 5.01f));
        }
        finally {offset.SetValue(null, originalOffset);}
    }

    [Test]
    public void UnscaledTimersUseTheirOwnClock()
    {
        int scaled = 0;
        int unscaled = 0;
        var normal = Timer.Schedule(5, () => scaled++);
        var realtime = Timer.Schedule(5, () => unscaled++, unscaledTime: true);

        Process(Deadline(normal) - 1, Deadline(realtime));
        Assert.That(scaled, Is.Zero, "Scaled time has not reached its deadline");
        Assert.That(unscaled, Is.EqualTo(1), "Unscaled time must run independently of scaled time");
        Process(Deadline(normal), Deadline(realtime));
        Assert.That(scaled, Is.EqualTo(1));
    }

    [Test]
    public void CreatedDuringGlobalPauseKeepsFullDelayAfterResume()
    {
        Timer.Pause();
        var start = typeof(Timer).GetField("pauseStartTime", BindingFlags.Static | BindingFlags.NonPublic);
        start.SetValue(null, Time.time - 100);
        int calls = 0;
        var timer = Timer.Schedule(5, () => calls++);
        Process(float.MaxValue, float.MaxValue);
        Assert.That(calls, Is.Zero);

        Timer.Resume();
        float now = (float)typeof(Timer).GetMethod("GetTime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { false });
        Assert.That(Deadline(timer) - now, Is.EqualTo(5).Within(0.05f), "Creation during pause must not add time elapsed before creation");
        Process(now + 4.9f, 0);
        Assert.That(calls, Is.Zero);
        Process(now + 5.1f, 0);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void RepeatSkipsMissedIntervalsWithoutBurstingAndCanCancelItself()
    {
        int calls = 0;
        TimerHandle timer = null;
        timer = Timer.Every(2, () => {calls++; if (calls == 2) timer.Cancel();});
        float now = Deadline(timer) + 100;

        Process(now, now);
        Process(now, now);
        Assert.That(calls, Is.EqualTo(1), "A late timer must run once, not replay missed intervals");
        Process(now + 2, now + 2);
        Process(now + 4, now + 4);
        Assert.That(calls, Is.EqualTo(2));
        Assert.That(timer.IsActive, Is.False);
    }

    [Test]
    public void FrameHandleCanPauseResumeAndCancel()
    {
        int calls = 0;
        var timer = Timer.OnUpdate(() => calls++);
        Tick();
        timer.Pause();
        Tick();
        Assert.That(calls, Is.EqualTo(1));
        timer.Resume();
        Tick();
        timer.Cancel();
        timer.Resume();
        Tick();
        Assert.That(calls, Is.EqualTo(2));
        Assert.That(timer.IsActive, Is.False);
    }

    [Test]
    public void CancelAllAllowsNewTimerForNextTick()
    {
        int calls = 0;
        Timer.After(0, () => {Timer.CancelAll(); Timer.After(0, () => calls++);});
        Timer.After(0, () => calls += 100);

        Tick();
        Assert.That(calls, Is.Zero);
        Tick();
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void ReentrantTickDoesNotReenterRepeatingCallback()
    {
        int calls = 0;
        Timer.Every(0, () => {if (++calls < 3) Tick();});

        Tick();
        Assert.That(calls, Is.EqualTo(1), "The same frame must not recursively execute the timer list");
    }

    [Test]
    public void ResetInvalidatesHandlesAndClearsGlobalPause()
    {
        var timer = Timer.Schedule(0, () => Assert.Fail("Old timer survived static reset"));
        Timer.Pause();
        typeof(Timer).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Assert.That(timer.IsActive, Is.False);
        int calls = 0;
        Timer.After(0, () => calls++);

        Tick();
        Assert.That(calls, Is.EqualTo(1), "Static reset must clear the previous session's global pause");
    }

    [Test]
    public void AlreadyDestroyedOwnerCannotCreateAnUnownedTimer()
    {
        var owner = new GameObject("Timer destroyed owner");
        UnityEngine.Object.DestroyImmediate(owner);
        int calls = 0;
        var timer = Timer.Schedule(0, () => calls++, owner);

        Tick();
        Assert.That(calls, Is.Zero);
        Assert.That(timer.IsActive, Is.False);
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void InvalidDelaysAreRejected(float delay)
    {
        var scheduled = Assert.Throws<ArgumentOutOfRangeException>(() => Timer.Schedule(delay, () => {}));
        var repeating = Assert.Throws<ArgumentOutOfRangeException>(() => Timer.Every(delay, () => {}));
        Assert.That(scheduled.ParamName, Is.EqualTo("delay"));
        Assert.That(repeating.ParamName, Is.EqualTo("interval"));
    }

    [Test]
    public void NullCallbacksAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => Timer.Schedule(0, null));
        Assert.Throws<ArgumentNullException>(() => Timer.Every(0, null));
        Assert.Throws<ArgumentNullException>(() => Timer.OnUpdate(null));
    }

    private static float Deadline(TimerHandle timer) {return (float)typeof(TimerHandle).GetField("EndTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(timer);}
    private static void Process(float scaledNow, float unscaledNow) {process.Invoke(null, new object[] { scaledNow, unscaledNow });}

    private static void Tick() {tick.Invoke(null, null);}
}
