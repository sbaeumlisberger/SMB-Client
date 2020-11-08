using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SMBClient.Models;
using SMBClient.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace SMBClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        [Reactive] public bool IsConnecting { get; set; } = false;

        [Reactive] public string CurrentPath { get; set; } = string.Empty;

        public string Location
        {
            get => Path.Combine("smb://", server, share, CurrentPath).Replace("\\", "/");
            set => SetLocation(value);
        }

        [Reactive] public IReadOnlyList<SMBItemViewModel> FileSystemItems { get; set; } = new List<SMBItemViewModel>();

        public IReadOnlyList<SMBItemViewModel> SelectedFileSystemItems
        {
            get => selectedFileSystemItems;
            set
            {
                this.RaiseAndSetIfChanged(ref selectedFileSystemItems, value);
                MultipleItemsSelected = SelectedFileSystemItems.Count > 1;
                DirectorySelected = SelectedFileSystemItems.Any(vm => vm.IsDirectory);
            }
        }

        [Reactive] public bool MultipleItemsSelected { get; set; } = false;

        [Reactive] public bool DirectorySelected { get; set; } = false;

        public ObservableCollection<BackgroundTaskViewModel> BackgroundTasks { get; } = new ObservableCollection<BackgroundTaskViewModel>();

        private string LastConnectionFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "lastConnection");

        private bool IsConnected => smbFileShare != null;

        private SMBFileShare SMBFileShare => smbFileShare ?? throw new InvalidOperationException("Not connected!");

        private SMBFileShare? smbFileShare;

        private string server = string.Empty;
        private string share = string.Empty;
        private string username = string.Empty;
        private string password = string.Empty;

        private IReadOnlyList<SMBItemViewModel> copiedItems = new List<SMBItemViewModel>();

        private IReadOnlyList<SMBItemViewModel> selectedFileSystemItems = new List<SMBItemViewModel>();

        private readonly CredentialsManager credentialsManager = new CredentialsManager();

        /* design time */
        public MainWindowViewModel()
        {
            FileSystemItems = new List<SMBItemViewModel>()
            {
                new SMBItemViewModel(true, "Directory 01", "", DateTime.Now, DateTime.Now, ""),
                new SMBItemViewModel(true, "Directory 02", "", DateTime.Now, DateTime.Now, ""),
                new SMBItemViewModel(true, "Directory 03", "", DateTime.Now, DateTime.Now, ""),
                new SMBItemViewModel(false, "File 01", "", DateTime.Now, DateTime.Now, "71 MB"),
                new SMBItemViewModel(false, "File 02", "", DateTime.Now, DateTime.Now, "653 kB"),
                new SMBItemViewModel(false, "File 03", "", DateTime.Now, DateTime.Now, "1,52 GB"),
            };
            var bt1Progress = new Progress();
            bt1Progress.Initialize(100);
            bt1Progress.Report(37);
            BackgroundTasks.Add(new BackgroundTaskViewModel(bt1Progress, "Donwload File 01 to clipboard"));
            var bt2Progress = new Progress();
            bt2Progress.Initialize(100);
            bt2Progress.Report(82);
            var bt2 = new BackgroundTaskViewModel(bt2Progress, "Donwload File 02 to clipboard");
            bt2.ErrorMessage = "Failed to read from file: STATUS_INVALID_SMB";
            BackgroundTasks.Add(bt2);
        }

        public MainWindowViewModel(bool autoConnect)
        {
            if (autoConnect && File.Exists(LastConnectionFile))
            {
                SetLocation(File.ReadAllText(LastConnectionFile));
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

        public async Task OpenAsync(SMBItemViewModel itemVM)
        {
            if (itemVM.IsDirectory)
            {
                CurrentPath = itemVM.FilePath;
                this.RaisePropertyChanged(nameof(Location));
                UpdateFileSystemItems();
            }
            else
            {
                await OpenFileAsync(itemVM);
            }
        }

        public async Task OpenWithAsync()
        {
            var itemVM = SelectedFileSystemItems.First();
            await OpenFileAsync(itemVM, showApplicationPicker: true);
        }

        public async Task OpenFileAsync(SMBItemViewModel itemVM, bool showApplicationPicker = false)
        {
            try
            {
                string tmpDirectoryPath = Path.Combine(Path.GetTempPath(), "smb-client");
                Directory.Delete(tmpDirectoryPath, true);
                Directory.CreateDirectory(tmpDirectoryPath);
                string tmpFilePath = Path.Combine(tmpDirectoryPath, itemVM.FileName);
                var copyOperation = new DownloadFileOperation(itemVM.SMBItem, tmpFilePath);
                copyOperation.Execute(SMBFileShare);
                var tmpFile = await StorageFile.GetFileFromPathAsync(tmpFilePath);
                var options = new LauncherOptions()
                {
                    DisplayApplicationPicker = showApplicationPicker
                };
                await Launcher.LaunchFileAsync(tmpFile, options);
            }
            catch (Exception exception)
            {
                await DialogService.ShowDialog(new MessageDialogModel()
                {
                    Title = $"Could not open {itemVM.FileName}",
                    Message = exception.ToString()
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

        public async Task CreateDirectoryAsync()
        {
            try
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
            catch (Exception exception)
            {
                await DialogService.ShowDialog(new MessageDialogModel()
                {
                    Title = $"Could not create directoy",
                    Message = exception.ToString()
                });
            }
        }

        public async Task PasteAsync()
        {
            if (copiedItems.Any())
            {
                await PasteFromAppClipboardAsync();
            }
            else
            {
                await PasteFromSystemClipboardAsync();
            }
        }

        public async Task PasteFromAppClipboardAsync()
        {
            if (!copiedItems.Any())
            {
                return;
            }
            var progress = new Progress();
            try
            {
                var progessDialogModel = new ProgressDialogModel(progress)
                {
                    Title = "Paste files from app clipboard",
                    Message = "Paste files from app clipboard",
                };
                var dialogTask = DialogService.ShowDialog(progessDialogModel);
                var copyTask = new CopyTask(SMBFileShare, copiedItems.Select(itemVM => itemVM.SMBItem), CurrentPath);
                await copyTask.ExecuteAsync(progress).ConfigureAwait(false);
                await dialogTask.ConfigureAwait(false);
                UpdateFileSystemItems();
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
                    Title = "Could not paste from app clipboard",
                    Message = exception.ToString()
                });
            }
        }

        public async Task PasteFromSystemClipboardAsync()
        {
            if (!Clipboard.GetContent().Contains(StandardDataFormats.StorageItems))
            {
                return;
            }
            var progress = new Progress();
            try
            {
                var clipboard = Clipboard.GetContent();
                var progessDialogModel = new ProgressDialogModel(progress)
                {
                    Title = "Paste files from system clipboard",
                    Message = "Paste files from system clipboard",
                };
                var dialogTask = DialogService.ShowDialog(progessDialogModel);
                var fileSystemItems = new List<FileSystemInfo>();
                foreach (IStorageItem storageItem in await clipboard.GetStorageItemsAsync())
                {
                    var fileSystemInfo = storageItem.IsOfType(StorageItemTypes.File)
                        ? (FileSystemInfo)new FileInfo(storageItem.Path)
                        : (FileSystemInfo)new DirectoryInfo(storageItem.Path);
                    fileSystemItems.Add(fileSystemInfo);
                }
                var uploadTask = new UploadTask(SMBFileShare, fileSystemItems, CurrentPath);
                await uploadTask.ExecuteAsync(progress).ConfigureAwait(false);
                await dialogTask.ConfigureAwait(false);
                UpdateFileSystemItems();
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
                    Title = "Could not paste from system clipboard",
                    Message = exception.ToString()
                });
            }
        }

        public async Task RenameAsync()
        {
            var itemVM = SelectedFileSystemItems.First();
            var inputDialogModel = new InputDialogModel();
            inputDialogModel.Title = "Rename";
            inputDialogModel.Message = "Enter a name for the item:";
            inputDialogModel.Value = itemVM.FileName;
            await DialogService.ShowDialog(inputDialogModel);
            if (!inputDialogModel.Canceled && inputDialogModel.Value != itemVM.FileName)
            {
                try
                {
                    string newName = inputDialogModel.Value;
                    SMBFileShare.Rename(itemVM.FilePath, newName);
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

        public void CopyToAppClipboard()
        {
            copiedItems = new List<SMBItemViewModel>(SelectedFileSystemItems);
        }

        public async Task CopyToSystemClipboardAsync()
        {
            var itemVMs = SelectedFileSystemItems.Where(item => !item.IsDirectory);

            if (!itemVMs.Any()) { return; }

            SMBFileShare? smbFileShare = null;
            var preparationBackgroundTask = new BackgroundTaskViewModel(new Progress(), $"Prepare download to clipboard");
            try
            {
                BackgroundTasks.Add(preparationBackgroundTask);
                IPAddress ipAddress = await ResolveIPAdressAsync(server);
                smbFileShare = await SMBFileShare.ConnectAsync(ipAddress, share, username, password); // use seperated connection 
                BackgroundTasks.Remove(preparationBackgroundTask);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Could not prepare download to clipboard: {exception}");
                preparationBackgroundTask.ErrorMessage = exception.Message;
                return;
            }

            var storageItems = new List<StorageFile>();
            int downloadedCount = 0;
            foreach (var itemVM in itemVMs)
            {
                storageItems.Add(await StorageFile.CreateStreamedFileAsync(itemVM.FileName, (dataRequest) =>
                {
                    lock (storageItems) // synchronize smb requests
                    {
                        var progress = new Progress();
                        var backgroundTask = new BackgroundTaskViewModel(progress, $"Download {itemVM.FileName} to clipboard");
                        Dispatcher.UIThread.Post(() => BackgroundTasks.Add(backgroundTask));
                        try
                        {
                            progress.Initialize(itemVM.SMBItem.Size);
                            using var stream = smbFileShare.OpenRead(itemVM.FilePath);
                            stream.CopyTo(dataRequest.AsStreamForWrite(), (int)smbFileShare.MaxReadSize, progress);
                            Dispatcher.UIThread.Post(() => BackgroundTasks.Remove(backgroundTask));
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine($"Could not download file: {exception}");
                            backgroundTask.ErrorMessage = exception.Message;
                        }
                        finally
                        {
                            dataRequest.Dispose();

                            if (++downloadedCount == storageItems.Count)
                            {
                                smbFileShare.Dispose();
                            }
                        }
                    }
                }, null));
            }
            var dataPackage = new DataPackage();
            dataPackage.SetStorageItems(storageItems);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }

        public async Task SaveAsync()
        {
            var openFolderDialogModel = new OpenFolderDialogModel();
            await DialogService.ShowDialog(openFolderDialogModel);
            if (openFolderDialogModel.FolderPath is string dstPath)
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
                    var items = SelectedFileSystemItems.Select(itemVM => itemVM.SMBItem);
                    var downloadTask = new DownloadTask(SMBFileShare, items, dstPath);
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

        public void CloseBackgroundTask(BackgroundTaskViewModel backgroundTask)
        {
            if (!backgroundTask.HasError)
            {
                throw new ArgumentException(null, nameof(backgroundTask));
            }
            BackgroundTasks.Remove(backgroundTask);
        }

        private void SetLocation(string location)
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

                ConnectAsync().ContinueWith(task =>
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

        private async Task<bool> ConnectAsync()
        {
            try
            {
                var loginDialogModel = new LoginDialogModel();
                if (credentialsManager.Retrieve(server) is Credential credential)
                {
                    loginDialogModel.Username = credential.Username;
                    loginDialogModel.Password = credential.Password;
                }
                await DialogService.ShowDialog(loginDialogModel);
                if (loginDialogModel.Canceled)
                {
                    return false;
                }
                IsConnecting = true;
                smbFileShare?.Dispose();
                smbFileShare = null;
                username = loginDialogModel.Username;
                password = loginDialogModel.Password;
                IPAddress ipAddress = await ResolveIPAdressAsync(server);
                smbFileShare = await SMBFileShare.ConnectAsync(ipAddress, share, username, password);
                File.WriteAllText(LastConnectionFile, Location);
                if (loginDialogModel.SaveCredentials && username != string.Empty)
                {
                    credentialsManager.Save(server, new Credential(username, password));
                }
                IsConnecting = false;
                return true;
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

        private async Task<IPAddress> ResolveIPAdressAsync(string server)
        {
            try
            {
                return IPAddress.Parse(server);
            }
            catch (FormatException)
            {
                var result = await Dns.GetHostEntryAsync(server).ConfigureAwait(false);
                return result.AddressList[0];
            }
        }

        private void UpdateFileSystemItems()
        {
            try
            {
                FileSystemItems = SMBFileShare.RetrieveItems(CurrentPath)
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
