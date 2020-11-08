using ReactiveUI.Fody.Helpers;
using SMBClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SMBClient.ViewModels
{
    public class BackgroundTaskViewModel : ViewModelBase
    {
        public Progress Progress { get; }
        public string Description { get; set; }
        [Reactive] public string ErrorMessage { get; set; } = string.Empty;
        [Reactive] public bool HasError { get; set; } = false;

        public BackgroundTaskViewModel(Progress progress, string description)
        {
            Progress = progress;
            Description = description;
            PropertyChanged += BackgroundTaskViewModel_PropertyChanged;
        }

        private void BackgroundTaskViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ErrorMessage))
            {
                HasError = ErrorMessage != string.Empty;
            }
        }
    }
}
