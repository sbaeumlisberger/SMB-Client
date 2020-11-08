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

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
