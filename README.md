# ⏱️ Goofy Timers

> No coroutines. No MonoBehaviour. No shitty code. Just timers.

**by [youpzdev](https://github.com/youpzdev)**

---

## What is this

A static timer system for Unity built on PlayerLoop. Replaces coroutines and Invoke for delayed and repeated calls - one line, no setup.

```csharp
Timer.After(2f, () => Explode());
Timer.Repeat(1f, SpawnEnemy);
Timer.EveryFrame(UpdateUI);
```

---

## Install

Import `Goofy-Timer.unitypackage` into your project or copy `Timer.cs` anywhere into your `Assets/` folder.

Initializes automatically via `RuntimeInitializeOnLoadMethod`. No prefabs, no managers, nothing to configure.

---

## Usage

### After - one-shot delay
```csharp
Timer.After(3f, () => Debug.Log("3 seconds later"));
```

### Repeat - interval
```csharp
Timer.Repeat(1f, SpawnEnemy);
```

### EveryFrame - runs every Update
```csharp
Timer.EveryFrame(TrackTarget);
```

### Cancel
```csharp
Action cancel = Timer.Repeat(1f, SpawnEnemy);
cancel(); // stop
```

### Auto-cancel when object is destroyed
```csharp
Timer.After(5f, () => enemy.Die(), enemy);
Timer.Repeat(1f, UpdateUI, this);
// if enemy/this is destroyed - timer cancels automatically, no NullReferenceException
```

### Pause / Resume
```csharp
Timer.Pause();
Timer.Resume(); // endTime shifts by pause duration, no shitty burst on resume
```

### CancelAll
```csharp
Timer.CancelAll();
```

---

## Benchmarks

Coroutine vs Timer, 1000 delayed calls:

| | Allocations | Setup |
|---|---|---|
| Coroutine | ~100 bytes each | `StartCoroutine` per call |
| **Goofy Timers** | **0kb GC** | one PlayerLoop injection |

Zero allocations in runtime. One PlayerLoop injection on startup, runs alongside Unity's own Update.

---

## How it works

- Injects into Unity's `PlayerLoop` at `Update` - no MonoBehaviour needed
- Tracks entries in a `List` with swap-and-pop removal - O(1) delete
- Cancel via flag, not search - O(1)
- `WeakReference` on target object - auto-cancels if object is destroyed
- `try/catch` per callback - one broken timer doesn't kill the rest
- Pause shifts `endTime` on all entries - no burst fire after resume

---

## Limitations

- Timers run on `Time.time` - affected by `Time.timeScale`. Pass `Time.unscaledTime` manually if needed
- Not thread-safe - call from main thread only (same as all Unity API)
- `CancelAll` cancels everything globally - use cancel actions for granular control

---

## License

MIT - do whatever you want.