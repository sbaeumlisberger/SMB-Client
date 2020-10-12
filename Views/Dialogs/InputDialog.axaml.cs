using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SMBClient.ViewModels;

namespace SMBClient.Views
{
    public class InputDialog : Window
    {
        public InputDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            Activated += InputDialog_Activated;
            AvaloniaXamlLoader.Load(this);            
        }

        private void InputDialog_Activated(object? sender, System.EventArgs e)
        {
            this.Find<TextBox>("inputTextBox").Focus();
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void OkButton_Click(object sender, RoutedEventArgs e) 
        {
            ((InputDialogModel)DataContext).Canceled = false;
            Close();
        }
    }
}
