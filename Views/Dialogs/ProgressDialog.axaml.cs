using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SMBClient.Models;
using SMBClient.ViewModels;

namespace SMBClient.Views
{
    public class ProgressDialog : Window
    {
        private ProgressDialogModel ViewModel => (ProgressDialogModel) DataContext;

        public ProgressDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            DataContextChanged += ProgressDialog_DataContextChanged;
            AvaloniaXamlLoader.Load(this);            
        }

        private void ProgressDialog_DataContextChanged(object? sender, System.EventArgs e)
        {
            ViewModel.Progess.StateChanged += Progess_StateChanged;
        }

        private void Progess_StateChanged(object? sender, System.EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ViewModel.Progess.State != ProgressState.Running)
                {
                    Close();
                }
            });
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Progess.Cancel();
        }
    }
}
