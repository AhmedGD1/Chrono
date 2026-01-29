# Chrono ⏱️

A powerful, flexible timer system for Unity that provides an elegant alternative to coroutines for time-based operations.

[![Unity](https://img.shields.io/badge/Unity-2020.3+-black.svg)](https://unity3d.com/get-unity/download)

## Features

- 🎯 **Simple API** - Intuitive builder pattern for creating timers and sequences
- ⚡ **High Performance** - Built-in object pooling to minimize garbage collection
- 🎮 **Channel System** - Group and control multiple timers together
- 🔄 **Flexible Timing** - Support for scaled, unscaled, and custom delta time
- 🎬 **Sequences** - Chain and synchronize timer events with ease
- ⏸️ **Pause/Resume** - Full control over timer lifecycle
- 🔀 **Parallel Execution** - Run multiple timer branches simultaneously
- 🚀 **Zero Setup** - Automatic singleton initialization, no GameObject required

## Installation

### Unity Package Manager (Git URL)

1. Open the Package Manager window (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL...`
3. Enter: `https://github.com/yourusername/chrono.git`

### Manual Installation

1. Download the latest release
2. Extract to your project's `Assets` folder
3. **That's it!** The `TimeSystem` singleton is automatically created when your game starts - no manual setup required

## Quick Start

### Using TimeSystem Helpers

```csharp
using Chrono;

// Schedule a one-time event
TimeSystem.Schedule(3f, () => Debug.Log("3 seconds later"));

// Repeat an action
TimeSystem.Repeat(1f, () => Debug.Log("Every second"));

// Repeat with a break condition
TimeSystem.RepeatUntil(0.5f, 
    () => DealDamage(), 
    () => enemy.IsDead);

// Create a timer from the pool
var timer = TimeSystem.CreateTimer(5f, oneShot: true);
timer.OnComplete(() => Debug.Log("Done!")).Start();
```

### Timer Sequences

```csharp
TimeSystem.Sequence()
    .Wait(1f).Do(() => Debug.Log("After 1 second"))
    .Wait(2f).Do(() => SpawnEnemy())
    .WaitUntil(() => enemyDefeated)
    .Do(() => Debug.Log("Enemy defeated!"))
    .Start();
```

## Core Concepts

### Timer

The `Timer` class is the foundation of the system. Each timer tracks elapsed time and triggers callbacks.

```csharp
var timer = TimeSystem.CreateTimer(waitTime: 5f, oneShot: true)
    .OnComplete(() => Debug.Log("Done!"))
    .OnUpdate(progress => UpdateProgressBar(progress))
    .Start();

// Control the timer
timer.Pause();
timer.Resume();
timer.Stop();
```

#### Timer Properties

```csharp
timer.WaitTime          // Duration of the timer
timer.Elapsed           // Time elapsed since start
timer.Progress          // Normalized progress (0-1)
timer.IsActive          // Is the timer running?
timer.IsPaused          // Is the timer paused?
```

### Builder Pattern

Chrono uses a fluent builder pattern for configuration:

```csharp
TimeSystem.CreateTimer(3f)
    .UnScaled(true)                          // Use unscaled time
    .SetProcessMode(TimerProcessMode.Fixed)  // Update in FixedUpdate
    .AddToChannel("Combat")                  // Add to channel for group control
    .OnComplete(() => Debug.Log("Boom!"))
    .DestroyOnTimeout()                      // Auto-cleanup
    .Start();
```

### Channels

Channels allow you to group and control multiple timers at once:

```csharp
// Create timers in a channel
TimeSystem.CreateTimer(5f)
    .AddToChannel("Combat")
    .OnComplete(() => SpawnEnemy())
    .Start();

TimeSystem.CreateTimer(10f)
    .AddToChannel("Combat")
    .OnComplete(() => BossPhase())
    .Start();

// Control all timers in the channel
TimeSystem.PauseChannel("Combat");
TimeSystem.ResumeChannel("Combat");
TimeSystem.DestroyChannel("Combat");
```

You can also use enums for type-safe channels:

```csharp
public enum GameChannel
{
    Combat,
    UI,
    Cutscene
}

TimeSystem.CreateTimer(2f)
    .AddToChannel(GameChannel.Combat)
    .Start();

TimeSystem.PauseChannel(GameChannel.Combat);
```

### Timer Sequences

Sequences let you choreograph complex timing patterns:

#### Basic Sequencing

```csharp
TimeSystem.Sequence()
    .Wait(1f).Do(() => FadeIn())
    .Wait(2f).Do(() => ShowTitle())
    .Wait(3f).Do(() => StartGame())
    .Start();
```

#### Conditional Waits

```csharp
TimeSystem.Sequence()
    .Wait(0.5f).Do(() => StartLoading())
    .WaitUntil(() => loadingComplete)
    .Do(() => Debug.Log("Loading finished!"))
    .Start();
```

#### Parallel Branches

```csharp
TimeSystem.Sequence()
    .Parallel(
        seq => seq.Wait(2f).Do(() => PlaySound()),
        seq => seq.Wait(1.5f).Do(() => ShowEffect()),
        seq => seq.Wait(3f).Do(() => ShakeCamera())
    )
    .Wait(1f).Do(() => Debug.Log("All parallel tasks done!"))
    .Start();
```

#### Frame-Perfect Timing

```csharp
TimeSystem.Sequence()
    .WaitFrame()  // Wait exactly one frame
    .Do(() => Debug.Log("Next frame!"))
    .Start();
```

## Advanced Usage

### Custom Delta Time

Override delta time for specific timers:

```csharp
float customDeltaTime = CalculateCustomDelta();

TimeSystem.CreateTimer(5f)
    .SetDeltaTime(customDeltaTime)
    .Start();
```

### Process Modes

Control when timers update:

```csharp
// Update in regular Update() - affected by Time.timeScale
timer.SetProcessMode(TimerProcessMode.Idle);

// Update in FixedUpdate() - for physics-based timing
timer.SetProcessMode(TimerProcessMode.Fixed);
```

### Unscaled Time

Use unscaled time for UI or pause menus:

```csharp
TimeSystem.CreateTimer(2f)
    .UnScaled(true)  // Ignores Time.timeScale
    .OnComplete(() => ClosePauseMenu())
    .Start();
```

### Object Pooling

Chrono automatically pools timers and sequences to reduce allocations:

```csharp
// Recommended: Get from pool with CreateTimer
var timer = TimeSystem.CreateTimer(5f, oneShot: true);
timer.OnComplete(() => Debug.Log("Done!")).Start();

// Manual pool access
var manualTimer = TimeSystem.GetTimerPool();
manualTimer.WaitTime = 3f;

// Return to pool (happens automatically with DestroyOnTimeout)
timer.Destroy(returnToPool: true);

// Clear pools if needed
TimeSystem.ClearTimerPool();
```

### Reusable Timers

```csharp
var cooldownTimer = TimeSystem.CreateTimer(5f);

// Use multiple times
void OnAttack()
{
    cooldownTimer
        .OnComplete(() => EnableAttack())
        .Start();
}

void OnSpecialMove()
{
    cooldownTimer
        .ClearCallbacks()
        .OnComplete(() => EnableSpecialMove())
        .Start();
}
```

## Common Patterns

### Cooldown System

```csharp
public class Weapon
{
    private Timer cooldownTimer;

    void Start()
    {
        cooldownTimer = TimeSystem.CreateTimer(attackCooldown, oneShot: true);
    }

    public void Attack()
    {
        if (cooldownTimer.IsActive) return;

        PerformAttack();
        cooldownTimer.Start();
    }
}
```

### Ability Timer with UI

```csharp
TimeSystem.CreateTimer(abilityCooldown)
    .OnUpdate(progress => {
        cooldownImage.fillAmount = 1f - progress;
    })
    .OnComplete(() => {
        abilityButton.interactable = true;
    })
    .Start();
```

### Delayed Cleanup

```csharp
void OnEnemyDeath()
{
    TimeSystem.Schedule(3f, () => Destroy(enemyGameObject));
}
```

### Timed Spawner

```csharp
TimeSystem.Repeat(spawnInterval, () => {
    SpawnEnemy();
}, timer => {
    timer.AddToChannel("GamePlay");
});
```

### Damage Over Time (DoT) Effect

```csharp
// Deal damage every 0.5 seconds until target is dead
TimeSystem.RepeatUntil(0.5f, 
    () => target.TakeDamage(damagePerTick),
    () => target.IsDead || !target.HasDebuff);
```

### Cutscene Sequence

```csharp
TimeSystem.Sequence()
    .Wait(0.5f).Do(() => FadeToBlack())
    .Wait(1f).Do(() => camera.transform.DOMove(targetPos, 2f))
    .Wait(2f).Do(() => PlayDialogue("Welcome..."))
    .WaitUntil(() => dialogueFinished)
    .Wait(0.5f).Do(() => FadeFromBlack())
    .OnComplete(() => StartGameplay())
    .Start();
```

### Combo System

```csharp
private Timer comboTimer;
private int comboCount;

void OnHit()
{
    comboCount++;
    
    comboTimer?.Stop();
    comboTimer = TimeSystem.CreateTimer(comboWindow)
        .OnComplete(() => {
            Debug.Log($"Combo: {comboCount}!");
            comboCount = 0;
        })
        .DestroyOnTimeout()
        .Start();
}
```

## API Reference

### Timer Class

#### Constructor
```csharp
Timer(float waitTime = 1f, bool oneShot = true)
```

#### Configuration Methods
```csharp
Timer UnScaled(bool value = true)
Timer SetDeltaTime(float? dt)
Timer AddToChannel(string channel)
Timer AddToChannel<TEnum>(TEnum channel) where TEnum : Enum
Timer SetProcessMode(TimerProcessMode mode)
Timer DestroyOnTimeout()
```

#### Callback Methods
```csharp
Timer OnComplete(params Action[] callbacks)
Timer OnUpdate(params Action<float>[] callbacks)  // Receives progress (0-1)
```

#### Control Methods
```csharp
void Start()
void Stop()
Timer Pause()
Timer Resume()
Timer TogglePaused(bool toggle)
void Destroy(bool returnToPool = true)
Timer Reset()
Timer ClearCallbacks()
```

### TimerSequence Class

#### Building Methods
```csharp
TimerSequence Wait(float duration)
TimerSequence Wait(float duration, float deltaTime)
TimerSequence WaitFrame()
TimerSequence WaitUntil(Func<bool> condition, float timeout = ∞)
TimerSequence Do(params Action[] callbacks)
TimerSequence Parallel(params Action<TimerSequence>[] branches)
TimerSequence OnComplete(Action callback)
TimerSequence SetProcessMode(TimerProcessMode mode)
```

#### Control Methods
```csharp
void Start()
void Pause()
void Resume()
```

### TimeSystem Static Methods

#### Timer Management
```csharp
static Timer CreateTimer(float waitTime = 1f, bool oneShot = true)  // Get timer from pool
static void Schedule(float duration, Action callback)
static void Repeat(float interval, Action callback, Action<Timer> config = null)
static void RepeatUntil(float interval, Action callback, Func<bool> breakCondition)
static void Add(Timer timer)
static void Remove(Timer timer)
static int GetActiveTimerCount()
```

#### Sequence Management
```csharp
static TimerSequence Sequence()
static TimerSequence Sequence(string channel)
static TimerSequence Sequence<TEnum>(TEnum channel) where TEnum : Enum
```

#### Channel Management
```csharp
static void AddToChannel(string channel, Timer timer)
static void PauseChannel(string channelName)
static void ResumeChannel(string channelName)
static void StopChannel(string channelName)
static void DestroyChannel(string channelName)
static void DestroyChannel(string channelName, float delay)
static int GetChannelCount(string channelName)
```

#### Object Pooling
```csharp
static Timer CreateTimer(float waitTime = 1f, bool oneShot = true)  // Recommended way to get from pool
static Timer GetTimerPool()         // Manual pool access
static TimerSequence GetSequencePool()
static void TimerToPool(Timer timer)
static void SequenceToPool(TimerSequence sequence)
static void ClearTimerPool()
```

## Performance Considerations

- ✅ Use `DestroyOnTimeout()` for one-time timers to enable automatic pooling
- ✅ Use `TimeSystem.CreateTimer()` instead of `new Timer()` to leverage object pooling
- ✅ Reuse timer objects when possible instead of creating new ones
- ✅ Use channels to batch-control related timers
- ✅ The system uses object pooling by default to minimize GC allocations
- ⚠️ Clear callbacks with `ClearCallbacks()` if reusing timers with different logic
- ⚠️ Be mindful of creating many parallel sequences - consider limiting concurrent sequences

## Why Chrono over Coroutines?

| Feature | Chrono | Coroutines |
|---------|--------|------------|
| Pause/Resume | ✅ Built-in | ❌ Requires manual tracking |
| Group Control | ✅ Channels | ❌ Manual management |
| Progress Tracking | ✅ Built-in | ❌ Manual calculation |
| Object Pooling | ✅ Automatic | ❌ Manual implementation |
| Readable Syntax | ✅ Fluent API | ⚠️ Can be verbose |
| Complex Async Logic | ⚠️ Limited | ✅ Excellent |

**Use Chrono for:**
- Timers, delays, and cooldowns
- Scheduled events
- Timed animations
- UI countdown timers
- Ability systems
- Grouped timer management

**Use Coroutines for:**
- Complex state machines
- Deep async/await patterns
- Unity API integration (WWW, AssetBundles)
- Frame-by-frame procedural generation

## Examples

Check out the [Examples](Examples/) folder for complete sample scenes:

- **Basic Timers** - Simple timer usage patterns
- **Combat System** - Cooldowns and ability timers
- **Cutscene System** - Complex sequences with parallel execution
- **UI Animations** - Progress bars and timed UI elements
- **Spawner System** - Timed object spawning with channels

## Requirements

- Unity 2020.3 or later

## Acknowledgments

- Inspired by Godot's Timer and Tween systems

## Support

- 📧 Email: aag75yssar@gmail.com
- 🐛 Issues: [GitHub Issues](https://github.com/AhmedGD1/chrono/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/AhmedGD1/chrono/discussions)

---

**Made [Ahmed GD]**
