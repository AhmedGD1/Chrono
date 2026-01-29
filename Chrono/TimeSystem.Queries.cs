using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chrono
{
    public partial class TimeSystem
    {
        #region Helper Methods
        private static bool ValidateChannel(string channelName, out List<Timer> channel)
        {
            if (!timerChannels.TryGetValue(channelName, out channel))
            {
                Debug.LogError($"Invalid Channel Name: {channelName}");
                return false;
            }
            return true;
        }

        private static void ForEachChannel(string channelName, Action<Timer> action)
        {
            if (!ValidateChannel(channelName, out var channel))
                return;
            channel.ForEach(action);
        }
        #endregion

        #region Timer Methods
        public static Timer CreateTimer(float waitTime = 1f, bool oneShot = true)
        {
            var timer = GetTimerPool();
            timer.WaitTime = waitTime;
            timer.OneShot = oneShot;

            return timer;
        }

        public static void Schedule(float duration, Action callback)
        {
            CreateTimer(duration).OnComplete(callback).DestroyOnTimeout().Start();
        }

        public static void Repeat(float interval, Action callback, Action<Timer> config = null)
        {
            var timer = CreateTimer(interval).OnComplete(callback);
            config?.Invoke(timer);
            timer.Start();
        }

        public static void RepeatUntil(float interval, Action callback, Func<bool> breakAction)
        {
            Repeat(interval, callback, timer => timer.OnUpdate(progress =>
            {
                if (breakAction())
                    timer.Destroy();
            }));
        }

        public static int GetActiveTimerCount() => idleTimers.Count + fixedTimers.Count;

        public static int GetChannelCount(string channelName)
        {
            if (ValidateChannel(channelName, out var channel))
                return channel.Count;
            Debug.LogError("Invalid Channel Name");
            return -1;
        }

        public int GetChannelCount<TEnum>(TEnum channelName) where TEnum : Enum => 
            GetChannelCount(GetEnumString(channelName));
        #endregion

        #region Sequence
        public static TimerSequence Sequence()
        {
            return GetSequencePool();
        }

        public static TimerSequence Sequence(string channel)
        {
            var sequence = GetSequencePool();
            sequence.Channel = channel;
            return sequence;
        }

        public static TimerSequence Sequence<TEnum>(TEnum channel) where TEnum : Enum =>
            Sequence(channel.ToString());
        #endregion

        #region Channel Pause
        public static void PauseChannel(string channelName) => ToggleChannelPaused(channelName, true);
        public static void ResumeChannel(string channelName) => ToggleChannelPaused(channelName, false);

        public static void PauseChannel<TEnum>(TEnum channelName) where TEnum : Enum => 
            ToggleChannelPaused(GetEnumString(channelName), true);

        public static void ResumeChannel<TEnum>(TEnum channelName) where TEnum : Enum => 
            ToggleChannelPaused(GetEnumString(channelName), false);

        public static void ToggleChannelPaused(string channelName, bool toggle) => 
            ForEachChannel(channelName, timer => timer.TogglePaused(toggle));

        public static void ToggleChannelPaused<TEnum>(TEnum channelName, bool toggle) where TEnum : Enum => 
            ToggleChannelPaused(GetEnumString(channelName), toggle);
        #endregion

        #region Channel Destruction
        public static void DestroyChannel(string channelName) =>
            ForEachChannel(channelName, timer => timer.Destroy());

        public static void DestroyChannel(string channelName, float interval) =>
            Schedule(interval, () => DestroyChannel(channelName));

        public static void DestroyChannel<TEnum>(TEnum channelName) where TEnum : Enum =>
            DestroyChannel(GetEnumString(channelName));

        public static void DestroyChannel<TEnum>(TEnum channelName, float interval) where TEnum : Enum =>
            DestroyChannel(GetEnumString(channelName), interval);
        #endregion

        #region Channel Stop
        public static void StopChannel(string channelName) => 
            ForEachChannel(channelName, timer =>  timer.Stop());
        
        public static void StopChannel<TEnum>(TEnum channelName) where TEnum : Enum => 
            StopChannel(GetEnumString(channelName));
        #endregion
    }
}

