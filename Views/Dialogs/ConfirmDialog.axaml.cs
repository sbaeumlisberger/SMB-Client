using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SMBClient.ViewModels;

namespace SMBClient.Views
{
    public class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {           
            Close();
        }

        public void OkButton_Click(object sender, RoutedEventArgs e) 
        {
            ((ConfirmDialogModel)DataContext).Canceled = false;
            Close();
        }
    }
}
