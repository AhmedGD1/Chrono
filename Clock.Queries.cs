using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chrono
{
    public partial class Clock
    {
        #region Id Control
        public static bool HasTimer(object id)
        {
            foreach (var timer in activeTimers)
                if (timer.Id != null && timer.Id.Equals(id))
                    return true;
            return false;
        }

        public static void CancelTimer(object id)
        {
            if (id == null) return;

            foreach (var timer in activeTimers)
                if (timer.Id != null && timer.Id.Equals(id))
                    timer.Cancel();   
        }

        public static void CancelAll()
        {
            foreach (var timer in activeTimers)
                timer.Cancel();
        }

        public static void PauseAll()
        {
            foreach (var timer in activeTimers)
                timer.Pause();
        }

        public static void ResumeAll()
        {
            foreach (var timer in activeTimers)
                timer.Resume();
        }
        #endregion

        #region Group Control
        private static bool ValidateGroup(string group, out HashSet<Timer> timers)
        {
            if (string.IsNullOrEmpty(group))
            {
                timers = default;
                return false;
            }
            
            if (groups.TryGetValue(group, out timers))
                return true;
            
            timers = default;
            return false;
        }

        public static void CancelGroup(string group)
        {
            if (!ValidateGroup(group, out var timers))
                return;
            
            foreach (var timer in timers)
                timer.Cancel();
        }

        public static void PauseGroup(string group)
        {
            if (!ValidateGroup(group, out var timers))
                return;
            
            foreach (var timer in timers)
                timer.Pause();
        }

        public static void ResumeGroup(string group)
        {
            if (!ValidateGroup(group, out var timers))
                return;
            
            foreach (var timer in timers)
                timer.Resume();
        }
        #endregion

        public static Timer Schedule(float duration, Action callback)
        {
            return GetPool(duration, true).OnComplete(callback).Start();
        }

        public static Timer Interval(float duration, Action callback)
        {
            return GetPool(duration, false).OnLoop(callback).Start();
        }

        public static void WaitUntil(Func<bool> condition, Action callback)
        {
            pollers.Add(new Poller(condition, callback));
        }

        public static Timer Repeat(int count, float duration, Action callback, Action onComplete = null)
        {
            Timer timer = GetPool(duration, false);
            int current = 0;

            timer.OnLoop(() =>
            {
                callback?.Invoke();
                current++;

                if (current >= count)
                {
                    onComplete?.Invoke();
                    timer.Cancel();
                }
            });

            return timer.Start();
        }

        public static Action Throttle(float duration, Action callback)
        {
            bool ready = true;

            return () =>
            {
                if (!ready) return;

                ready = false;
                callback?.Invoke();

                Schedule(duration, () => ready = true);
            };
        }

        public static Action Debounce(float duration, Action callback)
        {
            Timer timer = CreateTimer(duration, true);
            timer.OnComplete(callback);

            return () =>
            {
                timer.Cancel();
                timer.Start();
            };
        }
    }
}

