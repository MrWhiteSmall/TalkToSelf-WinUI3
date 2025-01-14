using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace demo1
{
    class ConfigSaveFolder
    {
        private string SaveDocumentFolder = "D:\\DesktopDocument";
        private string SavePictureFolder = "D:\\DesktopImages"; // DesktopImages
        private string SaveAudioFolder = "D:\\DesktopAudios";
        private string SaveVideoFolder = "D:\\DesktopVideos";
        public async Task<StorageFolder> GetOrCreateStorageFolderAsync(string folderPath)
        {
            try
            {
                // 尝试获取指定路径的 StorageFolder
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                return folder; // 如果文件夹存在，返回它
            }
            catch (FileNotFoundException)
            {
                // 如果文件夹不存在，则创建它
                try
                {
                    // 获取父文件夹
                    var parentFolderPath = System.IO.Path.GetDirectoryName(folderPath);
                    StorageFolder parentFolder = await StorageFolder.GetFolderFromPathAsync(parentFolderPath);

                    // 创建新文件夹
                    StorageFolder newFolder = await parentFolder.CreateFolderAsync(System.IO.Path.GetFileName(folderPath), CreationCollisionOption.OpenIfExists);
                    return newFolder; // 返回新创建的文件夹
                }
                catch (Exception ex)
                {
                    // 处理其他异常，例如路径无效
                    System.Diagnostics.Debug.WriteLine($"Error creating folder: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // 处理其他异常，例如路径无效
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<StorageFolder> GetOrCreateDocumentFolderAsync()
        {
            return await GetOrCreateStorageFolderAsync(SaveDocumentFolder);
        }

        public async Task<StorageFolder> GetOrCreatePictureFolderAsync()
        {
            return await GetOrCreateStorageFolderAsync(SavePictureFolder);
        }

        public async Task<StorageFolder> GetOrCreateAudioFolderAsync()
        {
            return await GetOrCreateStorageFolderAsync(SaveAudioFolder);
        }

        public async Task<StorageFolder> GetOrCreateVideoFolderAsync()
        {
            return await GetOrCreateStorageFolderAsync(SaveVideoFolder);
        }


        public async Task<StorageFolder> ChooseAndSaveFolderAsync()
        {
            // 使用 FolderPicker 选择文件夹
            FolderPicker folderPicker = new FolderPicker();
            // 获取当前窗口的句柄（在 WinUI 或桌面应用中）
            IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            // 将文件夹选择器与窗口句柄关联
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.ViewMode = PickerViewMode.List;
            folderPicker.FileTypeFilter.Add("*");

            // 获取文件夹
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                // 保存文件夹路径到应用的设置中，避免下次再次选择
                ApplicationData.Current.LocalSettings.Values["DownloadFolder"] = folder.Path;
            }
            return folder;
        }
        public async Task<StorageFolder> GetLocalStorageFolder()
        {
            // 读取之前保存的文件夹路径
            StorageFolder folder = null;

            while (true)
            {
                if (folder != null)
                {
                    break;
                }
                else
                {
                    folder = await ChooseAndSaveFolderAsync();
                }
            }

            return folder; // 返回新创建的文件夹
        }

    }


}
