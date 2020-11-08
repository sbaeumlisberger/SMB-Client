using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SMBClient.Models
{
    public class Progress : ReactiveObject
    {
        public event EventHandler<EventArgs>? StateChanged;

        public ProgressState State
        {
            get => _state;
            private set
            {
                if (value != _state)
                {
                    this.RaiseAndSetIfChanged(ref _state, value);
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Reactive] public double Percent { get; set; } = 0;

        [Reactive] public TimeSpan RemainingDuration { get; set; } = TimeSpan.Zero;

        [Reactive] public bool IsInitialized { get; set; } = false;

        public bool IsCanceled => State == ProgressState.Canceled;

        private long target;
        private long value;

        private DateTime startTime;

        private ProgressState _state = ProgressState.Running;

        public void Cancel()
        {
            if (State != ProgressState.Running)
            {
                throw new InvalidOperationException($"Must be in state {ProgressState.Running}");
            }
            State = ProgressState.Canceled;
        }

        public void Fail()
        {
            if (State != ProgressState.Running)
            {
                throw new InvalidOperationException($"Must be in state {ProgressState.Running}");
            }
            State = ProgressState.Failed;
        }

        public void Initialize(long target)
        {
            this.target = target;
            startTime = DateTime.Now;
            IsInitialized = true;
        }

        public void Report(long amount)
        {
            if (target <= 0)
            {
                throw new InvalidOperationException($"Must be in initialized with a valid target");
            }

            if (IsCanceled)
            {
                throw new OperationCanceledException();
            }

            value += amount;

            Percent = (value / ((double)target)) * 100;

            RemainingDuration = (DateTime.Now - startTime) * ((100 / Percent) - 1);

            if (value == target)
            {
                State = ProgressState.Finished;
            }
        }
    }
}
