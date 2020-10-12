using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SMBClient.ViewModels;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SMBClient.Views
{
    public class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private bool isUpdatingSelection = false;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DialogService.DialogRequested += DialogService_DialogRequested;
            DataContextChanged += MainWindow_DataContextChanged;
        }

        private void MainWindow_DataContextChanged(object? sender, System.EventArgs e)
        {
            ViewModel.PropertyChanged += MainWindow_PropertyChanged;
        }

        private async Task DialogService_DialogRequested(object? sender, object dialogModel)
        {
            if (dialogModel is SaveFileDialogModel saveFileDialogModel)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialFileName = saveFileDialogModel.InitialFileName;
                saveFileDialogModel.FilePath = await saveFileDialog.ShowAsync(this);
            }
            else if (dialogModel is OpenFolderDialogModel openFolderDialoggModel)
            {
                OpenFolderDialog openFolderDialog = new OpenFolderDialog();
                openFolderDialoggModel.FolderPath = await openFolderDialog.ShowAsync(this);

            }
            else if (dialogModel is InputDialogModel inputDialogModel)
            {
                InputDialog inputDialog = new InputDialog();
                inputDialog.DataContext = inputDialogModel;
                await inputDialog.ShowDialog(this);
            }
            else if (dialogModel is LoginDialogModel loginDialogModel)
            {
                LoginDialog loginDialog = new LoginDialog();
                loginDialog.DataContext = loginDialogModel;
                await loginDialog.ShowDialog(this);
            }
            else if (dialogModel is ConfirmDialogModel confirmDialogModel)
            {
                ConfirmDialog confirmDialog = new ConfirmDialog();
                confirmDialog.DataContext = confirmDialogModel;
                await confirmDialog.ShowDialog(this);
            }
        }

        private void MainWindow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedFileSystemItems))
                {
                    isUpdatingSelection = true;
                    var dataGrid = this.Find<DataGrid>("dataGrid");
                    var selectedItems = dataGrid.SelectedItems.Cast<FileSystemItemViewModel>().ToList();
                    foreach (var item in ViewModel.SelectedFileSystemItems.Except(selectedItems))
                    {
                        dataGrid.SelectedItems.Add(item);
                    }
                    foreach (var item in selectedItems.Except(ViewModel.SelectedFileSystemItems))
                    {
                        dataGrid.SelectedItems.Remove(item);
                    }
                    isUpdatingSelection = false;
                }
            });
        }

        public void Location_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.Location = ((TextBox)sender).Text;
            }
        }


        public void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isUpdatingSelection)
            {
                var dataGrid = this.Find<DataGrid>("dataGrid");
                ViewModel.SelectedFileSystemItems = dataGrid.SelectedItems.Cast<FileSystemItemViewModel>().ToList();
            }
        }

        public void DataGrid_DoubleTapped(object sender, RoutedEventArgs e)
        {
            var item = (FileSystemItemViewModel)((Visual)e.Source).DataContext;
            ViewModel.Open(item);
        }

        public void Item_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Right)
            {
                var item = (FileSystemItemViewModel)((Visual)e.Source).DataContext;
                if (!ViewModel.SelectedFileSystemItems.Contains(item))
                {
                    ViewModel.SelectedFileSystemItems = new[] { item };
                }
            }
        }

        public void Item_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var item = (FileSystemItemViewModel)((Visual)e.Source).DataContext;
                if (!ViewModel.SelectedFileSystemItems.Contains(item))
                {
                    ViewModel.SelectedFileSystemItems = new[] { item };
                }
            }
        }
    }
}
