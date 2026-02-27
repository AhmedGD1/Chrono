using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;

namespace Chrono
{
    public partial class Clock : MonoBehaviour
    {
        private struct Poller
        {
            public Func<bool> Condition { get; }
            public Action Callback { get; }

            public Poller(Func<bool> condition, Action callback)
            {
                Condition = condition;
                Callback = callback;
            }
        }

        private static Clock instance;

        private readonly static List<Timer> toRemove        = new();
        private readonly static Stack<Timer> timerPool      = new();
        private readonly static HashSet<Timer> activeTimers = new();

        private readonly static Dictionary<string, HashSet<Timer>> groups = new();
        private readonly static List<Poller> pollers                      = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            if (instance != null) return;
            var go = new GameObject("[Chrono]");
            go.AddComponent<Clock>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var timer in activeTimers)
                if (!timer.Persistent)
                    timer.Cancel();
        }

        private void Update()
        {
            foreach (var timer in activeTimers)
            {
                float dt = timer.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                timer.Tick(dt);

                if (timer.IsCompleted)
                {
                    if (timer.Pooled && !timer.IsLocked)
                        ReturnToPool(timer);
                    toRemove.Add(timer);
                }
            }

            for (int i = pollers.Count - 1; i >= 0; i--)
            {
                if (pollers[i].Condition())
                {
                    pollers[i].Callback?.Invoke();
                    pollers.RemoveAt(i);
                }
            }

            foreach (var timer in toRemove)
                activeTimers.Remove(timer);
            
            toRemove.Clear();
        }

        public static Timer CreateTimer(float duration, bool oneShot)
        {
            return new Timer(duration, oneShot, pooled: false);
        }

        public static void TryRegisterTimer(Timer timer)
        {
            if (!activeTimers.Contains(timer))
                activeTimers.Add(timer);
        }

        private static Timer GetPool(float duration, bool oneShot)
        {
            if (timerPool.Count > 0)
            {
                Timer timer = timerPool.Pop();
                timer.WaitTime = duration;
                timer.OneShot  = oneShot;
                return timer;
            }
            return new Timer(duration, oneShot, pooled: true);
        }

        private static void ReturnToPool(Timer timer)
        {
            timer.Release();
            timerPool.Push(timer);
        }

        public static void AddTimerToGroup(Timer timer, string group)
        {
            if (timer.Group != null)
            {
                RemoveTimerFromGroup(timer, timer.Group);
            }

            if (!groups.ContainsKey(group))
                groups[group] = new();
            
            groups[group].Add(timer);
        }

        public static void RemoveTimerFromGroup(Timer timer, string group)
        {
            groups[group].Remove(timer);

            if (groups[group].Count == 0)
                groups.Remove(group);
        }
    }
}
