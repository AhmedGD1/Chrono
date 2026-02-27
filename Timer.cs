using System;
using UnityEngine;

namespace Chrono
{
    public class Timer
    {
        public object Id    { get; private set; }
        public string Group { get; private set; }

        public float WaitTime { get; set; }
        public bool OneShot   { get; set; }
        
        public float Elapsed   => WaitTime - timeLeft;
        public float Remaining => timeLeft;
        public bool IsPaused   => paused;
        public bool Unscaled   => unscaled;
        public bool Persistent => persistent;

        internal bool Pooled      => pooled;
        internal bool IsCompleted => completed;
        internal bool IsLocked    => locked;

        private float timeLeft;

        private bool paused;
        private bool completed;
        private bool unscaled;
        private bool persistent;

        private readonly bool pooled;

        private Action<float> onUpdate;
        private Action onComplete;
        private Action onLoop;
        private Action onStart;

        private bool locked;
        private Timer chainedTimer;

        public Timer(float duration, bool oneShot, bool pooled)
        {
            WaitTime = duration;
            OneShot = oneShot;
            this.pooled = pooled;
        }

        public Timer Start()
        {
            timeLeft = WaitTime;
            completed = false;

            onStart?.Invoke();
            Clock.TryRegisterTimer(this);
            return this;
        }

        public Timer Cancel()
        {
            completed = true;
            return this;
        }

        public Timer Pause()
        {
            paused = true;
            return this;
        }

        public Timer Resume()
        {
            paused = false;
            return this;
        }

        public Timer SetId(object id)
        {
            Id = id;
            return this;
        }

        public Timer SetGroup(string group)
        {
            Group = group;
            Clock.AddTimerToGroup(this, group);
            return this;
        }

        public Timer SetUnscaled(bool unscaled = true)
        {
            this.unscaled = unscaled;
            return this;
        }
    
        public Timer SetPersistent(bool persistent = true)
        {
            this.persistent = persistent;
            return this;
        }

        public Timer OnComplete(Action callback)
        {
            onComplete = () =>
            {
                try { callback?.Invoke(); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Chrono] OnComplete callback failed — did you forget to cancel the timer? {e.Message}");
                }
            };
            return this;
        }

        public Timer OnLoop(Action callback)
        {
            onLoop = callback;
            return this;
        }

        public Timer OnStart(Action callback)
        {
            onStart = callback;
            return this;
        }

        public Timer OnUpdate(Action<float> callback)
        {
            onUpdate = callback;
            return this;
        }

        public Timer Chain(Timer timer)
        {
            if (!timer.OneShot)
            {
                Debug.LogWarning("[Chrono] Can't chain timers when their oneShot property is false (infinite loop)");
                return timer;
            }

            chainedTimer = timer;
            chainedTimer.Lock();
            return chainedTimer;
        }

        internal void Tick(float dt)
        {
            if (completed || paused)
                return;
            
            timeLeft -= dt;

            float progress = Mathf.Clamp01((WaitTime - timeLeft) / WaitTime);
            onUpdate?.Invoke(progress);

            if (timeLeft <= 0f)
            {
                if (OneShot)
                {
                    onComplete?.Invoke();
                    chainedTimer?.Unlock();
                    timeLeft  = 0f;
                    completed = true;
                    return;
                }

                onLoop?.Invoke();
                timeLeft = WaitTime;
            }
        }

        internal void Release()
        {
            if (Group != null)
                Clock.RemoveTimerFromGroup(this, Group);
            
            completed  = false;
            paused     = false;
            OneShot    = false;
            locked     = false;
            unscaled   = false;
            persistent = false;

            timeLeft  = 0f;
            WaitTime  = 0f;

            onComplete = null;
            onLoop     = null;
            onUpdate   = null;
            onStart    = null;

            Group = null;
            Id    = null;
        }

        internal void Lock()
        {
            locked = true;
            Cancel();
        }

        internal void Unlock()
        {
            Start();
            locked = false;
        }
    }
}
