# Chrono
A simple timer library for Unity. No coroutines, no boilerplate.

---

## Why
Coroutines work, but they're annoying to manage. Cancelling them requires storing a reference, pausing isn't built in, and grouping multiple coroutines together is a mess. Chrono replaces most coroutine use cases with a cleaner API.

---

## Installation
Add via Unity Package Manager using the git URL:
```
https://github.com/AhmedGD1/chrono.git
```

Or drop the `Chrono` folder directly into your project. No setup required — Chrono creates its own GameObject at runtime automatically.

---

## Usage

### One-shot delay
```csharp
Clock.Schedule(2f, () => Debug.Log("2 seconds later"));
```

### Repeating interval
```csharp
Clock.Interval(1f, () => Debug.Log("every second"));
```

### Repeat N times
```csharp
Clock.Repeat(3, 1f,
    onLoop:     () => Debug.Log("tick"),
    onComplete: () => Debug.Log("done")
);
```

### Wait until a condition is true
```csharp
Clock.WaitUntil(() => isReady, () => Debug.Log("ready!"));
```

---

## Timer Control

### Pause & Resume
```csharp
Timer timer = Clock.Schedule(5f, callback);
timer.Pause();
timer.Resume();
```

### Cancel
```csharp
timer.Cancel();
// or by id
Clock.CancelTimer("my-timer");
```

### Cancel / Pause everything
```csharp
Clock.CancelAll();
Clock.PauseAll();
Clock.ResumeAll();
```

---

## Groups
Group timers together to control them all at once.

```csharp
Clock.Schedule(2f, callback).SetGroup("enemies");
Clock.Interval(1f, callback).SetGroup("enemies");

Clock.PauseGroup("enemies");
Clock.ResumeGroup("enemies");
Clock.CancelGroup("enemies");
```

---

## Chaining
Run timers one after another.

```csharp
Timer first  = Clock.CreateTimer(2f, oneShot: true);
Timer second = Clock.CreateTimer(3f, oneShot: true);

first.OnComplete(() => Debug.Log("first done"));
second.OnComplete(() => Debug.Log("second done"));

first.Chain(second);
first.Start();
```

---

## Callbacks
```csharp
Clock.Schedule(3f, callback)
    .OnStart(()        => Debug.Log("started"))
    .OnUpdate(progress => Debug.Log($"{progress * 100}%"))
    .OnComplete(()     => Debug.Log("done"));
```

---

## Utilities

### Throttle
Limits how often a callback can fire. Useful for shooting, button presses, ability cooldowns.
```csharp
Action shoot = Clock.Throttle(0.5f, () => Shoot());

void Update()
{
    if (Input.GetKey(KeyCode.Space))
        shoot(); // fires at most once every 0.5s
}
```

### Debounce
Waits until calls stop before firing. Useful for search inputs or save-on-change.
```csharp
Action search = Clock.Debounce(0.3f, () => Search(input));

void OnInputChanged() => search(); // resets every call, fires after 0.3s of silence
```

---

## Options

```csharp
Clock.Schedule(2f, callback)
    .SetId("my-timer")        // identify the timer for later control
    .SetGroup("ui")           // add to a group
    .SetUnscaled()            // use unscaled time (survives Time.timeScale = 0)
    .SetPersistent();         // survive scene transitions
```

---

## User-managed Timers
If you want to own the timer yourself and reuse it:

```csharp
private Timer _timer;

void Start()
{
    _timer = Clock.CreateTimer(2f, oneShot: true);
    _timer.OnComplete(() => Debug.Log("done"));
}

void OnSomethingHappened()
{
    _timer.Start();
}
```

---

## Coroutine Equivalents

| Coroutine | Chrono |
|---|---|
| `yield return new WaitForSeconds(2f)` | `Clock.Schedule(2f, callback)` |
| `yield return new WaitForSecondsRealtime(2f)` | `Clock.Schedule(2f, callback).SetUnscaled()` |
| `yield return new WaitUntil(() => condition)` | `Clock.WaitUntil(() => condition, callback)` |
| `while(true) yield return new WaitForSeconds(1f)` | `Clock.Interval(1f, callback)` |

| `StopCoroutine(...)` | `timer.Cancel()` |
