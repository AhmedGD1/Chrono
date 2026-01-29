using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Chrono
{
    public class TimerSequence
    {
        public TimerProcessMode ProcessMode { get; set; }
        public string Channel { get; set; }

        private List<SequenceStep> steps = new();
        private Timer timer;

        private Action Completed;

        private int stepsCompleted;

        private class SequenceStep
        {
            public float Duration { get; set; } = 1f;
            public float DeltaTime { get; set; } = -1f;

            public Func<bool> Condition { get; set; } = null;
            public Action[] Callbacks { get; set; } = new Action[] { };

            public List<TimerSequence> ParallelBranches { get; set; }

            public bool ValidDeltaTime => DeltaTime != -1f;
            public bool IsParallel => ParallelBranches != null;
        }

        public TimerSequence Wait(float duration)
        {
            return Wait(duration, -1f);
        }
        
        public TimerSequence Wait(float duration, float deltaTime)
        {
            var step = new SequenceStep { Duration = duration };

            if (deltaTime != -1f)
                step.DeltaTime = deltaTime;
            steps.Add(step);
            return this;
        }

        public TimerSequence WaitFrame()
        {
            steps.Add(new SequenceStep { Duration = 0f }); 
            return this;
        }

        public TimerSequence WaitUntil(Func<bool> condition, float timeout = float.PositiveInfinity)
        {
            steps.Add(new SequenceStep { Condition = condition, Duration = timeout });
            return this;
        }

        public TimerSequence Do(params Action[] callback)
        {
            if (steps.Count == 0)
                throw new InvalidOperationException("Cannot call Do() before Wait/Condition/Parallel");

            var step = steps[^1];
            step.Callbacks = callback;
            return this;
        }

        public TimerSequence Parallel(params Action<TimerSequence>[] branches)
        {
            var parallelStep = new SequenceStep 
            { 
                Duration = float.PositiveInfinity, 
                ParallelBranches = new() 
            };

            foreach (var branchBuilder in branches)
            {
                var branch = TimeSystem.GetSequencePool();
                branch.Channel = Channel;
                branch.ProcessMode = ProcessMode;
                
                branchBuilder(branch);
                parallelStep.ParallelBranches.Add(branch);
            }
            steps.Add(parallelStep);
            return this;
        }

        public TimerSequence SetProcessMode(TimerProcessMode mode)
        {
            ProcessMode = mode;
            return this;
        }

        public TimerSequence OnComplete(Action callback)
        {
            Completed = callback;
            return this;
        }

        public void Pause() => timer.Pause();

        public void Resume() => timer.Resume();

        public void Start()
        {
            if (steps.Count == 0)
            {
                Debug.LogError("Invalid Empty Sequence");
                return;
            }

            var initialStep = steps[0];
            timer = TimeSystem.CreateTimer(initialStep.Duration)
                .SetDeltaTime(initialStep.ValidDeltaTime ? initialStep.DeltaTime : null);

            if (!string.IsNullOrEmpty(Channel))
                timer.AddToChannel(Channel);

            timer.OnUpdate(progress =>
            {
                if (stepsCompleted >= steps.Count)
                    return;
                var step = steps[stepsCompleted];
                if (step.Condition?.Invoke() ?? false)
                {
                    InvokeCallbacks(step); 
                    StartNextStep();
                }
            });

            timer.OnComplete(OnTimerCompleted).SetProcessMode(ProcessMode).Start();
        }

        private void OnTimerCompleted()
        {
            var currentStep = steps[stepsCompleted];

            if (currentStep.Condition != null)
                return;
            InvokeCallbacks(currentStep);
            StartNextStep();
        }

        private void StartNextStep()
        {
            stepsCompleted++;

            if (stepsCompleted >= steps.Count)
            {
                Completed?.Invoke();
                TimeSystem.SequenceToPool(this);
                return;
            }

            var step = steps[stepsCompleted];

            if (step.IsParallel)
            {
                RunParallelBranches(step);
                return;
            }

            timer.WaitTime = step.Duration;

            timer.SetDeltaTime(step.ValidDeltaTime ? step.DeltaTime : null);
            timer.Start();
        }

        private void InvokeCallbacks(SequenceStep step)
        {
            foreach (var callback in step.Callbacks)
                callback?.Invoke();   
        }

        private void RunParallelBranches(SequenceStep step)
        {
            var branches = step.ParallelBranches.ToList();
            int remaining = step.ParallelBranches.Count;

            foreach (var branch in branches)
            {
                branch.OnComplete(() =>
                {
                    remaining--;
                    if (remaining == 0)
                        StartNextStep();
                    TimeSystem.SequenceToPool(branch);
                });

                branch.Start();
            }
        }

        public void Reset()
        {
            timer?.Destroy();
            
            ProcessMode = TimerProcessMode.Idle;
            Completed = null;

            steps.ForEach(ClearParallelBranches);
            steps.Clear();

            stepsCompleted = 0;
            timer = null;
        }

        private void ClearParallelBranches(SequenceStep step)
        {
            if (step.ParallelBranches == null) return;
            foreach (var branch in step.ParallelBranches)
                TimeSystem.SequenceToPool(branch);
            step.ParallelBranches.Clear();
        }
    }   
}

