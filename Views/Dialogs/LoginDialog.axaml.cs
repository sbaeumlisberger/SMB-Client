using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SMBClient.ViewModels;
using System;

namespace SMBClient.Views
{
    public class LoginDialog : Window
    {
        public LoginDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            Activated += LoginDialog_Activated;
            AvaloniaXamlLoader.Load(this);
        }

        private void LoginDialog_Activated(object? sender, EventArgs e)
        {
            if (((LoginDialogModel)DataContext).Username != string.Empty)
            {
                this.Find<TextBox>("passwordTextBox").Focus();
            }
            else
            {
                this.Find<TextBox>("usernameTextBox").Focus();
            }
        }

        public void Window_KeyDown(object sender, KeyEventArgs e) 
        {
            if (e.Key == Key.Enter)
            {
                ((LoginDialogModel)DataContext).Canceled = false;
                Close();
            }
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ((LoginDialogModel)DataContext).Canceled = false;
            Close();
        }
    }
}
