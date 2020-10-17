using Avalonia.Controls;
using ReactiveUI;
using SharpGen.Runtime;
using SMBClient.Models;
using SMBClient.Utils;
using SMBLibrary;
using SMBLibrary.SMB1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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

        public IReadOnlyList<SMBItemViewModel> FileSystemItems
        {
            get => _fileSystemItems;
            private set => this.RaiseAndSetIfChanged(ref _fileSystemItems, value);
        }

        public IReadOnlyList<SMBItemViewModel> SelectedFileSystemItems
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

        private bool IsConnected => smbFileShare != null;

        private SMBFileShare SMBFileShare => smbFileShare ?? throw new InvalidOperationException("Not connected!");

        private SMBFileShare? smbFileShare;

        private string server = string.Empty;
        private string share = string.Empty;

        private IReadOnlyList<SMBItemViewModel> copiedItems = new List<SMBItemViewModel>();

        #region backing fields

        private bool _isConnecting = false;
        public string _currentPath = string.Empty;
        private IReadOnlyList<SMBItemViewModel> _fileSystemItems = new List<SMBItemViewModel>();
        private IReadOnlyList<SMBItemViewModel> _selectedFileSystemItems = new List<SMBItemViewModel>();

        #endregion

        /* design time */
        public MainWindowViewModel()
        {
            FileSystemItems = new List<SMBItemViewModel>()
            {
                new SMBItemViewModel(true, "Directory 01", "", DateTime.Now, ""),
                new SMBItemViewModel(true, "Directory 02", "", DateTime.Now, ""),
                new SMBItemViewModel(true, "Directory 03", "", DateTime.Now, ""),
                new SMBItemViewModel(false, "File 01", "", DateTime.Now, "71 MB"),
                new SMBItemViewModel(false, "File 02", "", DateTime.Now, "653 kB"),
                new SMBItemViewModel(false, "File 03", "", DateTime.Now, "1,52 GB"),
            };
        }

        public MainWindowViewModel(bool autoConnect)
        {
            if (autoConnect && File.Exists(LastConnectionFile))
            {
                string lastConnection = File.ReadAllText(LastConnectionFile);
                string[] lines = lastConnection.Split("\n");
                SetLocation(location: lines[0], username: lines[1]);
            }
        }

        public void Refresh()
        {
            if (IsConnected)
            {
                UpdateFileSystemItems();
            }
            else
            {
                FileSystemItems = new List<SMBItemViewModel>();
            }
        }

        public void Open(SMBItemViewModel item)
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
                SMBFileShare.CopyFile(item.FilePath, tmpPath, new Progress());
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
            var progress = new Progress();
            try
            {
                if (copiedItems.Any())
                {
                    PasteFromApp();
                    UpdateFileSystemItems();
                }
                else if (Clipboard.GetContent().Contains(StandardDataFormats.StorageItems))
                {
                    await PasteFromClipboardAsync(progress);
                    UpdateFileSystemItems();
                }
            }
            catch (OperationCanceledException)
            {
                // canceled by user
                UpdateFileSystemItems();
            }
            catch (Exception exception)
            {
                progress.Fail();
                UpdateFileSystemItems();
                await DialogService.ShowDialog(new MessageDialogModel()
                {
                    Title = "Could not paste",
                    Message = exception.ToString()
                });
            }
        }

        // TODO: progress
        private void PasteFromApp()
        {
            foreach (SMBItemViewModel item in copiedItems)
            {
                string dstPath = Path.Combine(CurrentPath, item.FileName);
                if (item.IsDirectory)
                {
                    SMBFileShare.CopyDirectory(item.FilePath, dstPath, new Progress());
                }
                else
                {
                    SMBFileShare.CopyFile(item.FilePath, dstPath, new Progress());
                }
            }
        }

        private async Task PasteFromClipboardAsync(Progress progress)
        {
            var clipboard = Clipboard.GetContent();
            var progessDialogModel = new ProgressDialogModel(progress)
            {
                Title = "Paste files from clipboard",
                Message = "Paste files from clipboard",
            };
            var dialogTask = DialogService.ShowDialog(progessDialogModel);
            // TODO use single uploadTask
            foreach (IStorageItem storageItem in await clipboard.GetStorageItemsAsync())
            {
                string dstPath = Path.Combine(CurrentPath, Path.GetFileName(storageItem.Path));
                var fileSystemInfo = storageItem.IsOfType(StorageItemTypes.File)
                    ? (FileSystemInfo)new FileInfo(storageItem.Path)
                    : (FileSystemInfo)new DirectoryInfo(storageItem.Path);
                var uploadTask = new UploadTask(SMBFileShare, fileSystemInfo, dstPath);
                await uploadTask.ExecuteAsync(progress).ConfigureAwait(false);
            }
            await dialogTask;
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
                try
                {
                    string newPath = Path.Combine(CurrentPath, inputDialogModel.Value);
                    SMBFileShare.Rename(item.FilePath, newPath);
                    UpdateFileSystemItems();
                }
                catch (Exception exception)
                {
                    await DialogService.ShowDialog(new MessageDialogModel()
                    {
                        Title = "Could not rename",
                        Message = exception.ToString()
                    });
                }
            }
        }

        public async void Copy()
        {
            copiedItems = SelectedFileSystemItems.ToList();

            var storageItems = new List<StorageFile>();
            foreach (var item in SelectedFileSystemItems.Where(item => !item.IsDirectory))
            {
                storageItems.Add(await StorageFile.CreateStreamedFileAsync(item.FileName, dataRequest =>
                {
                    try
                    {
                        SMBFileShare.ReadFile(item.FilePath, dataRequest.AsStreamForWrite(), Progress.None);
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($"Could not stream file: {exception}");
                    }
                    finally
                    {
                        dataRequest.Dispose();
                    }
                }, null));
            }
            if (storageItems.Any())
            {
                var dataPackage = new DataPackage();
                dataPackage.SetStorageItems(storageItems);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
        }

        public async Task SaveAsync()
        {
            DownloadTask? downloadTask = null;

            var itemVM = SelectedFileSystemItems.First();
            if (itemVM.IsDirectory)
            {
                var openFolderDialogModel = new OpenFolderDialogModel();
                await DialogService.ShowDialog(openFolderDialogModel);
                if (openFolderDialogModel.FolderPath is string dstPath)
                {
                    downloadTask = new DownloadTask(SMBFileShare, true, itemVM.SMBItem, Path.Combine(dstPath, itemVM.FileName));
                }
            }
            else
            {
                var saveFileDialogModel = new SaveFileDialogModel();
                saveFileDialogModel.InitialFileName = itemVM.FileName;
                await DialogService.ShowDialog(saveFileDialogModel);
                if (saveFileDialogModel.FilePath is string dstPath)
                {
                    downloadTask = new DownloadTask(SMBFileShare, false, itemVM.SMBItem, dstPath);
                }
            }

            if (downloadTask != null)
            {
                Progress progress = new Progress();
                var progessDialogModel = new ProgressDialogModel(progress)
                {
                    Title = "Save files",
                    Message = "Save files",
                };
                var progessDialogTask = DialogService.ShowDialog(progessDialogModel);
                try
                {
                    await downloadTask.ExecuteAsync(progress);
                    await progessDialogTask;
                }
                catch (OperationCanceledException)
                {
                    // canceled by user
                }
                catch (Exception exception)
                {
                    progress.Fail();
                    await DialogService.ShowDialog(new MessageDialogModel()
                    {
                        Title = "Could not save files",
                        Message = exception.ToString()
                    });
                }
            }
        }

        // TODO: progress
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
                    smbFileShare?.Dispose();
                    smbFileShare = null;
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
                await DialogService.ShowDialog(new MessageDialogModel()
                {
                    Title = "Could not connect",
                    Message = exception.ToString()
                });
                IsConnecting = false;
                return false;
            }
        }

        private void UpdateFileSystemItems()
        {
            try
            {
                FileSystemItems = SMBFileShare.GetFilesAndDirectories(CurrentPath)
                    .Select(smbItem => new SMBItemViewModel(smbItem))
                    .OrderByDescending(item => item.IsDirectory)
                    .ThenBy(vm => vm.FileName)
                    .ToList();
                SelectedFileSystemItems = new List<SMBItemViewModel>();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Could not fetch files and directories: {exception}");
            }
        }
    }
}
