using System.Collections.Generic;
using UnityEngine;
using System;

namespace Chrono
{
    public partial class TimeSystem : MonoBehaviour
    {
        public static TimeSystem Instance { get; private set; }

        private readonly static Dictionary<string, List<Timer>> timerChannels = new();
        
        private readonly static List<Timer> idleTimers = new();
        private readonly static List<Timer> fixedTimers = new();
        private readonly static HashSet<Timer> pendingRemoval = new();

        private readonly static Stack<TimerSequence> sequencePool = new();
        private readonly static Stack<Timer> timerPool = new();

        private const int MAX_TIMER_POOL_SIZE = 500;
        private const int MAX_SEQUENCE_POOL_SIZE = 50;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;

            var go = new GameObject("[TimeSystem]");
            Instance = go.AddComponent<TimeSystem>();
            DontDestroyOnLoad(go);
            
            WarmupPools();
        }

        private static void WarmupPools()
        {
            for (int i = 0; i < 20; i++)
            {
                timerPool.Push(new Timer());
                sequencePool.Push(new TimerSequence());
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Add & Remove
        public static void AddToChannel(string channel, Timer timer)
        {
            if (!timerChannels.ContainsKey(channel))
                timerChannels[channel] = new();
            timerChannels[channel].Add(timer);
        }

        public static void AddToChannel<TEnum>(TEnum channel, Timer timer) where TEnum : Enum
        {
            AddToChannel(GetEnumString(channel), timer);
        }

        public static void Add(Timer timer)
        {
            var targetList = timer.ProcessMode == TimerProcessMode.Idle ? idleTimers : fixedTimers;
            
            if (targetList.Contains(timer))
                return;

            targetList.Add(timer);
        }

        public static void Remove(Timer timer)
        {
            pendingRemoval.Add(timer);
        }

        public static bool RemoveFromChannel(Timer timer)
        {
            if (timer == null || string.IsNullOrEmpty(timer.Channel))
                return false;
            
            if (!timerChannels.TryGetValue(timer.Channel, out var channelList))
                return false;

            channelList.Remove(timer);

            if (channelList.Count == 0)
                timerChannels.Remove(timer.Channel);
            
            return true;
        }
        #endregion

        #region Timers Update
        private void Update()
        {
            ProcessPendingRemovals();
            UpdateTimerList(idleTimers);
        }

        private void FixedUpdate()
        {
            ProcessPendingRemovals();
            UpdateTimerList(fixedTimers);
        }

        private void ProcessPendingRemovals()
        {
            if (pendingRemoval.Count == 0)
                return;

            idleTimers.RemoveAll(t => pendingRemoval.Contains(t));
            fixedTimers.RemoveAll(t => pendingRemoval.Contains(t));
            
            foreach (var timer in pendingRemoval)
            {
                if (!string.IsNullOrEmpty(timer.Channel) && 
                    timerChannels.TryGetValue(timer.Channel, out var list))
                {
                    list.Remove(timer);
                    if (list.Count == 0)
                        timerChannels.Remove(timer.Channel);
                }
            }
            
            pendingRemoval.Clear();
        }

        private void UpdateTimerList(List<Timer> timerList)
        {
            for (int i = 0; i < timerList.Count; i++)
            {
                timerList[i].Update();
            }
        }
        #endregion

        #region Sequence Pool
        public static void SequenceToPool(TimerSequence sequence)
        {
            if (sequencePool.Count >= MAX_SEQUENCE_POOL_SIZE)
                return;
            
            sequence.Reset();
            sequencePool.Push(sequence);
        }

        public static TimerSequence GetSequencePool()
        {
            return sequencePool.Count == 0 ? new TimerSequence() : sequencePool.Pop();
        }
        #endregion

        #region Timer Pool
        public static Timer GetTimerPool()
        {
            return timerPool.Count == 0 ? new Timer() : timerPool.Pop();
        }

        public static void TimerToPool(Timer timer)
        {
            if (timerPool.Count >= MAX_TIMER_POOL_SIZE)
                return;
            
            timer.Reset();
            timerPool.Push(timer);
        }

        public static void ClearTimerPool() => timerPool.Clear();
        #endregion

        #region Enum String Caching
        private static readonly Dictionary<Enum, string> enumStringCache = new();

        private static string GetEnumString<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            if (!enumStringCache.TryGetValue(enumValue, out string cached))
            {
                cached = enumValue.ToString();
                enumStringCache[enumValue] = cached;
            }
            return cached;
        }
        #endregion

        #region Debug Utilities
#if UNITY_EDITOR
        public static void LogPerformanceStats()
        {
            Debug.Log($"[TimeSystem Performance Stats]\n" +
                     $"Idle Timers: {idleTimers.Count}\n" +
                     $"Fixed Timers: {fixedTimers.Count}\n" +
                     $"Total Timers: {idleTimers.Count + fixedTimers.Count}\n" +
                     $"Channels: {timerChannels.Count}\n" +
                     $"Timer Pool Size: {timerPool.Count}\n" +
                     $"Sequence Pool Size: {sequencePool.Count}\n" +
                     $"Pending Removals: {pendingRemoval.Count}");
        }
#endif
        #endregion
    }
}