using Avalonia.Controls;
using ReactiveUI;
using SharpGen.Runtime;
using SMBClient.Models;
using SMBClient.Utils;
using SMBLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SMBClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public bool IsConnecting
        {
            get => _isConnecting;
            private set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
        }

        public string CurrentPath
        {
            get => _currentPath;
            private set => this.RaiseAndSetIfChanged(ref _currentPath, value);
        }

        public string Location
        {
            get => Path.Combine("smb://", server, share, CurrentPath).Replace("\\", "/");
            set => SetLocation(value);
        }

        public IReadOnlyList<FileSystemItemViewModel> FileSystemItems
        {
            get => _fileSystemItems;
            private set => this.RaiseAndSetIfChanged(ref _fileSystemItems, value);
        }

        public IReadOnlyList<FileSystemItemViewModel> SelectedFileSystemItems
        {
            get => _selectedFileSystemItems;
            set
            {
                bool countChanged = value.Count != _selectedFileSystemItems.Count;
                this.RaiseAndSetIfChanged(ref _selectedFileSystemItems, value);
                if (countChanged)
                {
                    this.RaisePropertyChanged(nameof(MultipleItemsSelected));
                }
            }
        }

        public bool MultipleItemsSelected => SelectedFileSystemItems.Count > 1;

        private string LastConnectionFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "lastConnection");

        private SMBFileShare SMBFileShare => smbFileShare ?? throw new InvalidOperationException("Not connected!");

        private SMBFileShare? smbFileShare;

        private string server = string.Empty;
        private string share = string.Empty;

        private IReadOnlyList<FileSystemItemViewModel> copiedItems = new List<FileSystemItemViewModel>();

        #region backing fields

        private bool _isConnecting = false;
        public string _currentPath = string.Empty;
        private IReadOnlyList<FileSystemItemViewModel> _fileSystemItems = new List<FileSystemItemViewModel>();
        private IReadOnlyList<FileSystemItemViewModel> _selectedFileSystemItems = new List<FileSystemItemViewModel>();

        #endregion

        public MainWindowViewModel() { /* design time */ }

        public MainWindowViewModel(bool autoConnect)
        {
            if (autoConnect && File.Exists(LastConnectionFile))
            {
                string lastConnection = File.ReadAllText(LastConnectionFile);
                string[] lines = lastConnection.Split("\n");
                SetLocation(location: lines[0], username: lines[1]);
            }
        }

        public void Open(FileSystemItemViewModel item)
        {
            if (item.IsDirectory)
            {
                CurrentPath = item.FilePath;
                this.RaisePropertyChanged(nameof(Location));
                UpdateFileSystemItems();
            }
            else
            {
                string tmpDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "tmp");
                Directory.CreateDirectory(tmpDirectory);
                string tmpPath = Path.Combine(tmpDirectory, item.FileName);
                SMBFileShare.CopyFile(item.FilePath, tmpPath);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = tmpPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
            }
        }

        public void DirectoryUp()
        {
            if (Path.GetDirectoryName(CurrentPath) is string parentPath)
            {
                CurrentPath = parentPath;
                this.RaisePropertyChanged(nameof(Location));
                UpdateFileSystemItems();
            }
        }

        public async Task CreateDirectory()
        {
            var inputDialogModel = new InputDialogModel();
            inputDialogModel.Title = "Create Directory";
            inputDialogModel.Message = "Enter a name for the directory:";
            await DialogService.ShowDialog(inputDialogModel);
            if (!inputDialogModel.Canceled)
            {
                SMBFileShare.CreateDirectory(Path.Combine(CurrentPath, inputDialogModel.Value));
                UpdateFileSystemItems();
            }
        }

        public async Task PasteAsync()
        {
            if (copiedItems.Any())
            {
                foreach (FileSystemItemViewModel item in copiedItems)
                {
                    string dstPath = Path.Combine(CurrentPath, item.FileName);
                    if (item.IsDirectory)
                    {
                        SMBFileShare.CopyDirectory(item.FilePath, dstPath);
                    }
                    else
                    {
                        SMBFileShare.CopyFile(item.FilePath, dstPath);
                    }
                }
                UpdateFileSystemItems();
            }
            else
            {
                var clipboard = Clipboard.GetContent();
                if (clipboard.Contains(StandardDataFormats.StorageItems))
                {
                    foreach (IStorageItem storageItem in await clipboard.GetStorageItemsAsync())
                    {
                        string dstPath = Path.Combine(CurrentPath, Path.GetFileName(storageItem.Path));
                        if (storageItem is IStorageFolder)
                        {
                            SMBFileShare.UploadDirectory(storageItem.Path, dstPath);
                        }
                        else
                        {
                            SMBFileShare.UploadFile(storageItem.Path, dstPath);
                        }
                    }
                    UpdateFileSystemItems();
                }
            }            
        }

        public async Task RenameAsync()
        {
            var item = SelectedFileSystemItems.First();
            var inputDialogModel = new InputDialogModel();
            inputDialogModel.Title = "Rename Directory";
            inputDialogModel.Message = "Enter a name for the directory:";
            inputDialogModel.Value = item.FileName;
            await DialogService.ShowDialog(inputDialogModel);
            if (!inputDialogModel.Canceled && inputDialogModel.Value != item.FileName)
            {
                string newPath = Path.Combine(CurrentPath, inputDialogModel.Value);
                SMBFileShare.Rename(item.FilePath, newPath);
                UpdateFileSystemItems();
            }
        }

        public void Copy()
        {
            copiedItems = SelectedFileSystemItems.ToList();
        }

        public async Task SaveAsync()
        {
            var item = SelectedFileSystemItems.First();
            if (item.IsDirectory)
            {
                var openFolderDialogModel = new OpenFolderDialogModel();
                await DialogService.ShowDialog(openFolderDialogModel);
                if (openFolderDialogModel.FolderPath is string dstPath)
                {
                    SMBFileShare.DownloadDirectory(item.FilePath, Path.Combine(dstPath, item.FileName));
                }
            }
            else
            {
                var saveFileDialogModel = new SaveFileDialogModel();
                saveFileDialogModel.InitialFileName = item.FileName;
                await DialogService.ShowDialog(saveFileDialogModel);
                if (saveFileDialogModel.FilePath is string dstPath)
                {
                    SMBFileShare.DownloadFile(item.FilePath, dstPath);
                }
            }
        }

        public async Task DeleteAsync()
        {
            var confirmDialogModel = new ConfirmDialogModel();
            confirmDialogModel.Title = "Delete Items";
            confirmDialogModel.Message = "Are you sure that you want to delete the following items?\n\n";
            confirmDialogModel.Message += $"{string.Join("\n", SelectedFileSystemItems.Select(item => "  " + item.FileName))}";
            await DialogService.ShowDialog(confirmDialogModel);
            if (!confirmDialogModel.Canceled)
            {
                foreach (var item in SelectedFileSystemItems)
                {
                    if (item.IsDirectory)
                    {
                        SMBFileShare.DeleteDirectory(item.FilePath);
                    }
                    else
                    {
                        SMBFileShare.DeleteFile(item.FilePath);
                    }
                }
                UpdateFileSystemItems();
            }
        }

        private void SetLocation(string location, string username = "")
        {
            string[] parts = location.RemovePrefix("smb://").Split("/");

            if (parts.Length < 2)
            {
                return; // invalid location
            }

            string server = parts[0];
            string share = parts[1];
            string path = parts.Length > 2 ? parts[2] : string.Empty;

            if (smbFileShare is null || server != this.server || share != this.share)
            {
                this.server = server;
                this.share = share;
                CurrentPath = path;

                ConnectAsync(username).ContinueWith(task =>
                {
                    if (task.IsCompleted && task.Result)
                    {
                        UpdateFileSystemItems();
                    }
                });
            }
            else if (path != CurrentPath)
            {
                CurrentPath = path;
                UpdateFileSystemItems();
            }
        }

        private async Task<bool> ConnectAsync(string lastUsername = "")
        {
            try
            {
                IsConnecting = true;
                var loginDialogModel = new LoginDialogModel();
                loginDialogModel.Username = lastUsername;
                await DialogService.ShowDialog(loginDialogModel);
                if (loginDialogModel.Canceled)
                {
                    IsConnecting = false;
                    return false;
                }
                else
                {
                    string username = loginDialogModel.Username;
                    string password = loginDialogModel.Password;
                    smbFileShare = await SMBFileShare.ConnectAsync(IPAddress.Parse(server), share, username, password);
                    File.WriteAllText(LastConnectionFile, Path.Combine("smb://", server, share).Replace("\\", "/") + "\n" + username);
                    IsConnecting = false;
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception); // TODO show dialog
                IsConnecting = false;
                return false;
            }
        }

        private void UpdateFileSystemItems()
        {
            FileSystemItems = SMBFileShare.GetFilesAndDirectories(CurrentPath)
                .Select(fdi => new FileSystemItemViewModel(CurrentPath, fdi))
                .OrderByDescending(item => item.IsDirectory)
                .ThenBy(item => item.FileName)
                .ToList();
            SelectedFileSystemItems = new List<FileSystemItemViewModel>();
        }
    }
}
