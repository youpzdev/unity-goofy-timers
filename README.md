# ⏱️ Goofy Timers

> No coroutines. No MonoBehaviour. No shitty code. Just timers.

**by [youpzdev](https://github.com/youpzdev)**

---

## What is this

A static timer system for Unity built on PlayerLoop. Replaces coroutines and Invoke for delayed and repeated calls — one line, no setup.

```csharp
Timer.After(2f, () => Explode());
Timer.Repeat(1f, SpawnEnemy);
Timer.EveryFrame(UpdateUI);
```

---

## Install

Import `Goofy-Timer.unitypackage` into your project or copy `Timer.cs` into your `Assets/` folder.

Initializes automatically via `RuntimeInitializeOnLoadMethod`. No prefabs, no managers, nothing to configure.

---

## Usage

```csharp
// ── One-shot delay ─────────────────────────────────────────
Timer.After(3f, () => Debug.Log("3 seconds later"));

// ── Interval ───────────────────────────────────────────────
Timer.Repeat(1f, SpawnEnemy);

// ── Every frame ────────────────────────────────────────────
Timer.EveryFrame(TrackTarget);

// ── Cancel ─────────────────────────────────────────────────
Action cancel = Timer.Repeat(1f, SpawnEnemy);
cancel();

// ── Auto-cancel when object is destroyed ───────────────────
Timer.After(5f, () => enemy.Die(), enemy);
Timer.Repeat(1f, UpdateUI, this);
// if enemy/this is destroyed — cancels automatically, no NullReferenceException

// ── Pause / Resume ─────────────────────────────────────────
Timer.Pause();
Timer.Resume(); // endTime shifts by pause duration, no burst on resume

// ── Cancel everything ──────────────────────────────────────
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

| | |
|---|---|
| 🔁 **Loop** | Injects into `PlayerLoop` at `Update` — no MonoBehaviour needed |
| ⚡ **Removal** | Swap-and-pop on List — O(1) delete |
| 🚩 **Cancel** | Via flag, not search — O(1) |
| 🛡️ **Null-safety** | `WeakReference` on target — auto-cancels if object is destroyed |
| 🔒 **Resilience** | `try/catch` per callback — one broken timer doesn't kill the rest |
| ⏸️ **Pause** | Shifts `endTime` on all entries — no burst fire after resume |

---

## Limitations

- Timers run on `Time.time` — affected by `Time.timeScale`. Use `Time.unscaledTime` manually if needed
- Not thread-safe — call from main thread only (same as all Unity API)
- `CancelAll` cancels everything globally — use cancel actions for granular control

---

## Part of the Goofy Tools collection

| | |
|---|---|
| [**goofy-pooling**](https://github.com/youpzdev/unity-goofy-pooling) | 🐟 Zero-config object pooling |
| **goofy-timers** | ⏱️ You are here |
| [**goofy-eventbus**](https://github.com/youpzdev/unity-goofy-eventbus) | 📡 Type-safe event bus |
| [**goofy-saves**](https://github.com/youpzdev/unity-goofy-saves) | 💾 AES-256 encrypted save system |

---

## License

MIT — do whatever you want.
