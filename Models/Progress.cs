using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SMBClient.Models
{
    public class Progress : ReactiveObject
    {
        public static Progress None = new Progress();

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

        public double Percent
        {
            get => _percent;
            private set => this.RaiseAndSetIfChanged(ref _percent, value);
        }

        public bool IsCanceled => State == ProgressState.Canceled;

        private long target;
        private long value;

        private ProgressState _state = ProgressState.Running;
        private double _percent = 0;

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

            if (Percent == 100)
            {
                State = ProgressState.Finished;
            }
        }

    }
}
