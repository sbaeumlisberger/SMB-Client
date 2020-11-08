using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;

namespace SMBClient.ViewModels
{
    public class InputDialogModel : ViewModelBase
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        [Reactive] public string Value { get; set; } = string.Empty;
        [Reactive] public bool ValueMustBeNotEmpty { get; set; } = true;
        [Reactive] public bool IsOKButtonEnabled { get; private set; } = false;
        public bool Canceled { get; set; } = true;

        public InputDialogModel() 
        {
            PropertyChanged += InputDialogModel_PropertyChanged;
        }

        private void InputDialogModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsOKButtonEnabled = !ValueMustBeNotEmpty || !string.IsNullOrWhiteSpace(Value);
        }
    }
}
