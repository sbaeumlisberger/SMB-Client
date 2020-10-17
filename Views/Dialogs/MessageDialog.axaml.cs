using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SMBClient.ViewModels;

namespace SMBClient.Views
{
    public class MessageDialog : Window
    {
        public MessageDialog()
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

        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Close();
            }
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
