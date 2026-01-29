using System.Collections.Generic;
using UnityEngine;
using System;

namespace Chrono
{
    public enum TimerProcessMode
    {
        Idle,
        Fixed,
    }

    public class Timer
    {
        private event Action Timeout;
        private event Action<float> Updated;

        public TimerProcessMode ProcessMode { get; set; }
        public string Channel { get; private set; }

        public float WaitTime { get; set; }
        public bool OneShot { get; set; }

        public bool IsTemporary { get; private set; }
        public bool Scalable => !unScaled;
        public bool TimeIndependent => independentDeltaTime != null;

        public bool IsPaused => paused;
        public bool IsActive => active && !paused;

        public float Elapsed => WaitTime - remaining;
        public float Progress => 1f - (remaining / WaitTime);

        private float remaining;

        private float? independentDeltaTime = null;

        private bool active;
        private bool paused;
        private bool unScaled;

        private List<Action> timeoutCallbacks = new();
        private List<Action<float>> updatedCallbacks = new(); 

        public Timer(float waitTime = 1f, bool oneShot = true)
        {
            WaitTime = waitTime;
            OneShot = oneShot;
        }

        public Timer UnScaled(bool value = true)
        {
            unScaled = value;
            return this;
        }

        public Timer SetDeltaTime(float? dt)
        {
            independentDeltaTime = dt;
            return this;
        }

        public Timer AddToChannel(string channel)
        {
            Channel = channel;
            TimeSystem.AddToChannel(channel, this);
            return this;
        }

        public Timer AddToChannel<TEnum>(TEnum channel) where TEnum : Enum
        {
            return AddToChannel(channel.ToString());
        }

        public void Start()
        {
            remaining = WaitTime;
            active = true;
            TimeSystem.Add(this);
        }

        public void Stop()
        {
            active = false;
            remaining = 0f;
        }

        public Timer Pause() => TogglePaused(true);
        public Timer Resume() => TogglePaused(false);

        public Timer TogglePaused(bool toggle)
        {
            paused = toggle;
            return this;
        }

        public Timer OnComplete(params Action[] callbacks)
        {   
            foreach (var callback in callbacks)
            {
                Timeout += callback;
                timeoutCallbacks.Add(callback);
            }
            return this;
        }

        public Timer OnUpdate(params Action<float>[] callbacks)
        {
            foreach (var callback in callbacks)
            {
                Updated += callback;
                updatedCallbacks.Add(callback);
            }
            return this;
        }

        public Timer DestroyOnTimeout()
        {
            IsTemporary = true;
            return this;
        }

        public Timer SetProcessMode(TimerProcessMode mode)
        {
            ProcessMode = mode;
            return this;
        }

        public Timer ClearCallbacks()
        {
            if (timeoutCallbacks != null)
            {
                timeoutCallbacks.ForEach(action => Timeout -= action);
                timeoutCallbacks.Clear();
            }

            if (updatedCallbacks != null)
            {
                updatedCallbacks.ForEach(action => Updated -= action);
                updatedCallbacks.Clear();
            }

            Updated = null;
            Timeout = null;
            return this;
        }

        public void Destroy(bool returnToPool = true)
        {
            ClearCallbacks();

            active = false;

            if (returnToPool)
                TimeSystem.TimerToPool(this);
            else
                TimeSystem.Remove(this);

        }

        public Timer Reset()
        {
            ClearCallbacks();
            active = false;
            paused = false;
            remaining = 0f;
            independentDeltaTime = null;
            unScaled = false;
            return this;
        }

        internal void Update()
        {
            if (!active || paused)
                return;
            
            float dt = independentDeltaTime ?? ProcessMode switch
            {
                TimerProcessMode.Idle => unScaled ? Time.unscaledDeltaTime : Time.deltaTime,
                TimerProcessMode.Fixed => unScaled ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime,
                _ => throw new Exception("Unknown TimerProcessMode")
            };

            remaining -= dt;
            Updated?.Invoke(Progress);

            if (remaining <= 0f)
            {
                var wasActive = active;
                var wasRemaining = remaining;
                
                Timeout?.Invoke();

                if (IsTemporary)
                    Destroy();

                else if (OneShot)
                {
                    if (remaining <= 0f)
                    {
                        active = false;
                        remaining = 0f;
                    }

                }
                else
                {
                    if (remaining <= 0f)
                        remaining += WaitTime;
                }
            }
        }
    }
}
