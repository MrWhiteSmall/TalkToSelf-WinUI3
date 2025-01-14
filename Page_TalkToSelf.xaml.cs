using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.Services.Store;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Input;
using Microsoft.UI.Input.DragDrop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace demo1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Page_TalkToSelf : Page
    {
        public static string TAG = "TalkToSelf";
        public string testImgPath { get; set; } = "Assets/StoreLogo.png";
        public ObservableCollection<string> imgPaths = new ObservableCollection<string>();

        ConfigSaveFolder config;
        public ObservableCollection<ItemMessageDetail> Messages { get; } = [];
        // 初始化消息
        public int InitializeMessage()
        {
            var msgs = new ItemMessage_DB().GetMessageFromDB();
            foreach (ItemMessage msg in msgs)
            {
                ItemMessageDetail itemMessageDetail = new ItemMessageDetail(msg);
                Messages.Add(itemMessageDetail);
            }
            return Messages.Count;
        }
        // 初始化RefreshContainer
        public void InitializeRefreshContainer(object sender, RoutedEventArgs e)
        {
            this.Loaded -= InitializeRefreshContainer;
            RefreshContainer.RefreshRequested += RefreshContainer_RefreshRequested;
            RefreshContainer.Visualizer.RefreshStateChanged += Visualizer_RefreshStateChanged;
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            RefreshContainer.RequestRefresh();
        }

        private async void RefreshContainer_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            // Respond to a request by performing a refresh and using the deferral object.
            using (var RefreshCompletionDeferral = args.GetDeferral())
            {
                // Do some async operation to refresh the content

                //await FetchAndInsertItemsAsync(3);
                Debug.WriteLine("RefreshContainer_RefreshRequested");

                // 获取Message最早的ID
                long lastId = Messages[0].Id;
                var extraMessages = new ItemMessage_DB().GetExtraMessageFromDB(lastId);
                // 把额外的消息插入到Messages中
                foreach (ItemMessage msg in extraMessages)
                {
                    ItemMessageDetail itemMessageDetail = new ItemMessageDetail(msg);
                    Messages.Insert(0, itemMessageDetail);
                }

                // The 'using' statement ensures the deferral is marked as complete.
                // Otherwise, you'd call
                // RefreshCompletionDeferral.Complete();
                // RefreshCompletionDeferral.Dispose();
            }
        }

        private void Visualizer_RefreshStateChanged(RefreshVisualizer sender, RefreshStateChangedEventArgs args)
        {
            // Respond to visualizer state changes.
            // Disable the refresh button if the visualizer is refreshing.
            if (args.NewState == RefreshVisualizerState.Refreshing)
            {
                RefreshButton.IsEnabled = false;
            }
            else
            {
                RefreshButton.IsEnabled = true;
            }
        }




        // 方法：滚动到底部
        private void InitializeScrollView(object sender, RoutedEventArgs e)
        {
            // 在页面加载完成后，滚动到ScrollViewer的底部
            ScrollViewMessage.ChangeView(null, ScrollViewMessage.ScrollableHeight, null);
        }
        private void UpdateScrollViewAfterSend()
        {
            ScrollViewMessage.ChangeView(null, ScrollViewMessage.ScrollableHeight, null);
        }
        public Page_TalkToSelf()
        {
            this.InitializeComponent();

            //new ItemMessage_DB().DeleteMessageFromDB(15);
            new DataAccess().InitializeDatabase();
            new ItemMessage_DB().CreateTable();
            InitializeMessage();
            this.Loaded += InitializeRefreshContainer;
            this.Loaded += InitializeScrollView;

            config = new ConfigSaveFolder();

        }

        // 方法：发送消息
        private void _SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                ItemMessage msg = new ItemMessage(InputTextBox.Text, MessageType.Text);
                // save message
                long insertedID = new ItemMessage_DB().AddMessageToDB(msg);
                msg.Id = insertedID;
                ItemMessageDetail msgDetail = new ItemMessageDetail(msg);
                Messages.Add(msgDetail); // Add the user's message
                InputTextBox.Text = string.Empty; // Clear the input box
                UpdateScrollViewAfterSend();
            }
        }
        private void SendMessage(object sender, RoutedEventArgs e)
        {
            _SendMessage();
        }

        // 方法：文本框 拖拽 时，赋予copy操作
        private void InputTextBox_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        // 方法：拖拽 到 文本框
        // 2024 12.xx Text
        // 2024 12.31 Image
        // Audo Video File ...
        private async void InputTextBox_Drop(object sender, DragEventArgs e)
        {

            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                string text = await e.DataView.GetTextAsync();
                InputTextBox.Text = text;
            }
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                int itemCount = items.Count;
                if (itemCount > 0)
                {
                    imgPaths.Clear();
                    foreach (StorageFile item in items)
                    {
                        string savePath = await SaveImageToMediaFolder(item);
                        imgPaths.Add(savePath);
                        Debug.WriteLine($"saved ! {savePath}");
                    }
                    ItemMessage msg = new ItemMessage(string.Join(";", imgPaths), MessageType.Image);

                    // save message
                    long insertedID = new ItemMessage_DB().AddMessageToDB(msg);
                    msg.Id = insertedID;
                    ItemMessageDetail msgDetail = new ItemMessageDetail(msg);

                    Messages.Add(msgDetail); // Add the user's message
                }
            }

        }
        // 保存图片到 media 文件夹
        private async Task<string> SaveImageToMediaFolder(StorageFile file)
        {
            // 获取应用的本地存储文件夹
            //StorageFolder saveFolder = await config.GetLocalStorageFolder();
            StorageFolder saveFolder = await config.GetOrCreatePictureFolderAsync();
            if (saveFolder == null)
            {
                Debug.WriteLine("Error: Could not create or access the media folder");
                return null;
            }

            // 生成唯一的文件名：uuid_时间.png
            var uuid = Guid.NewGuid().ToString();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // 获取图片的扩展名
            var fileExtension = file.FileType.ToLower(); // 获取文件扩展名（如 .jpg, .png）
            var fileName = $"{uuid}_{timestamp}{fileExtension}";

            string dstPath = Path.Combine(saveFolder.Path, fileName);
            // 创建文件并复制数据 C:\Users\server\AppData\Local\Packages\1feb55ca-01cd-4c8c-8e06-71214ae36130_eh8yxnpcfzfbc\LocalState\media\a3f7dac2-92a3-4e76-9ccf-108cae4c033d_20241231_094440.png
            //await file.CopyAsync(saveFolder, fileName,NameCollisionOption.ReplaceExisting);
            File.Copy(file.Path, dstPath);

            // 返回保存的文件路径
            return Path.Combine(saveFolder.Path, fileName);
        }

        // 检测ctrol shift是否被点击
        private bool _IsControlPressed()
        {
            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

        }
        private bool _IsShiftPressed()
        {
            var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
            return (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

        }
        // 统一处理图片发送问题
        private async Task<List<string>> _SendImageMessage(List<StorageFile> files)
        {
            List<string> saveImagePaths = new List<string>();
            foreach (StorageFile file in files)
            {
                string path = await SaveImageToMediaFolder(file);
                saveImagePaths.Add(path);
            }
            ItemMessage msg = new ItemMessage(string.Join(";", saveImagePaths),
                                                MessageType.Image);

            // save message
            long insertedID = new ItemMessage_DB().AddMessageToDB(msg);
            msg.Id = insertedID;
            ItemMessageDetail msgDetail = new ItemMessageDetail(msg);
            Messages.Add(msgDetail); // Add the user's message

            return saveImagePaths;
        }
        private enum ClipboardContentType
        {
            Text,
            Image,
            Audio,
            Video,
            Document,
            Other,
        }
        /*
         剪切板 各种文件 处理方法 处理ctrl + V
         */
        private async Task<ClipboardContentType> _PossessClipboardContent()
        {

            var clipboardContent = Clipboard.GetContent();
            if (clipboardContent.Contains(StandardDataFormats.Text))
            {
                Debug.WriteLine("Clipboard contains text");

                return ClipboardContentType.Text;
            }
            if (clipboardContent.Contains(StandardDataFormats.StorageItems))
            {
                // 1 check file extention
                // 2 return ClipboardContentType by extention
                Debug.WriteLine("Clipboard contains StorageItems");
                var storageItems = clipboardContent.GetStorageItemsAsync().AsTask().Result;

                List<StorageFile> storageImage = new List<StorageFile>();
                List<StorageFile> storageAudio = new List<StorageFile>();
                List<StorageFile> storageVideo = new List<StorageFile>();
                List<StorageFile> storageDocument = new List<StorageFile>();
                // 自动对文件类型分类 根据不同类型的文件，返回不同的枚举值
                foreach (StorageFile item in storageItems)
                {
                    if (item.FileType == ".jpg" || item.FileType == ".png" ||
                        item.FileType == ".bmp" || item.FileType == ".gif")
                    {
                        Debug.WriteLine($"Clipboard contains StorageItems {item.Path}");
                        storageImage.Add(item);
                    }
                    else if (item.FileType == ".mp3" || item.FileType == ".wav" || item.FileType == ".flac")
                    {
                        Debug.WriteLine($"Clipboard contains StorageItems {item.Path}");
                        storageAudio.Add(item);
                    }
                    else if (item.FileType == ".mp4" || item.FileType == ".avi" || item.FileType == ".mov")
                    {
                        Debug.WriteLine($"Clipboard contains StorageItems {item.Path}");
                        storageVideo.Add(item);
                    }
                    else
                    {
                        Debug.WriteLine($"Clipboard contains StorageItems {item.Path}");
                        storageDocument.Add(item);
                    }
                }

                // check file extention ,
                //  if image return Image
                //  if audio return Audio 
                //  if video return Video
                //  if other extention , all return document

                List<string> saveImagePaths = await _SendImageMessage(storageImage);

                //SendAudioMessage(storageAudio);
                //SendVideoMessage(storageVideo);
                //SendDocumentMessage(storageDocument);

                return ClipboardContentType.Document;
            }
            if (clipboardContent.Contains(StandardDataFormats.Bitmap))
            {
                Debug.WriteLine("Clipboard contains Bitmap");
                // 处理粘贴的图片
                var bitmap = clipboardContent.GetBitmapAsync().AsTask().Result;

                var stream = bitmap.OpenReadAsync().AsTask().Result;

                // 保存位图到文件
                // 创建一个文件并写入位图数据
                StorageFile file = KnownFolders.PicturesLibrary.CreateFileAsync("tmp.png",
                                    CreationCollisionOption.ReplaceExisting).AsTask().Result;

                using (var fileStream = file.OpenAsync(FileAccessMode.ReadWrite).AsTask().Result)
                {
                    await RandomAccessStream.CopyAsync(stream, fileStream);
                }

                string savePath = await SaveImageToMediaFolder(file);
                // send message
                ItemMessage msg = new ItemMessage(savePath, MessageType.Image);
                // save message
                long insertedId = new ItemMessage_DB().AddMessageToDB(msg);
                msg.Id = insertedId;

                ItemMessageDetail msgDetail = new ItemMessageDetail(msg);
                Messages.Add(msgDetail); // Add the user's message

                // delete tmp.png
                await file.DeleteAsync();

                return ClipboardContentType.Image;
            }
            if (clipboardContent.Contains(StandardDataFormats.Html))
            {
                Debug.WriteLine("Clipboard contains Html");
            }
            if (clipboardContent.Contains(StandardDataFormats.Rtf))
            {
                Debug.WriteLine("Clipboard contains Rtf");
            }
            if (clipboardContent.Contains(StandardDataFormats.WebLink))
            {
                Debug.WriteLine("Clipboard contains WebLink");
            }
            if (clipboardContent.Contains(StandardDataFormats.ApplicationLink))
            {
                Debug.WriteLine("Clipboard contains ApplicationLink");
            }

            return ClipboardContentType.Other;

        }
        // 输入框的快捷键方法
        private async void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {

            bool isShiftPressed = _IsShiftPressed();
            bool isCtrlPressed = _IsControlPressed();
            // shift enter = warp
            if (isShiftPressed && e.Key == Windows.System.VirtualKey.Enter)
            {
                // 插入换行符
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 获取光标位置
                    int caretIndex = textBox.SelectionStart;

                    // 在光标位置插入换行符
                    textBox.Text = textBox.Text.Insert(caretIndex, "\n");

                    // 更新光标位置
                    textBox.SelectionStart = caretIndex + 1;

                    // 取消事件的默认处理
                    e.Handled = true;
                }
            }
            // enter
            else if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _SendMessage();
            }
            // ctrl + v
            else if (isCtrlPressed && e.Key == Windows.System.VirtualKey.V)
            {
                Debug.WriteLine("Ctrl + V");
                await _PossessClipboardContent();
                //Process other...

                UpdateScrollViewAfterSend();

            }
        }

        // 图片点击发送按钮
        public async Task<StorageFile> OpenImageFileSelected(List<string> available_image_extention)
        {
            var filePicker = new FileOpenPicker();

            // 获取当前窗口的句柄（在 WinUI 或桌面应用中）
            IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            // 将文件夹选择器与窗口句柄关联
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail; // 以缩略图视图显示文件

            foreach (var extention in available_image_extention)
            {
                filePicker.FileTypeFilter.Add(extention);
            }

            var file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                Debug.WriteLine($"selected Image path {file.Path}");
                return file;
            }
            else
            {
                Debug.WriteLine($"{file} is Null");
                return null;
            }
        }
        private async void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> available_image_extention =
            [
                ".jpg",
                ".png",
                ".bmp",
                ".gif",
            ];
            // 1 open folder filtered by image file
            // 2 get returned image file
            // 3 save and send 
            var file = await OpenImageFileSelected(available_image_extention);
            if (file != null)
            {
                string savePath = await SaveImageToMediaFolder(file);
                ItemMessage msg = new ItemMessage(savePath, MessageType.Image);
                // save message
                long insertedId = new ItemMessage_DB().AddMessageToDB(msg);
                msg.Id = insertedId;
                ItemMessageDetail msgDetail = new ItemMessageDetail(msg);
                Messages.Add(msgDetail); // Add the user's message


                UpdateScrollViewAfterSend();
            }
        }

        // 双击修改文本历史记录 dialog
        private async void ShowDialogForEditMessage(ItemMessageDetail messageDetail)
        {
            DialogEditMessage dialogEditMessage = new DialogEditMessage(messageDetail)
            {
                XamlRoot = XamlRoot
            };
            await dialogEditMessage.ShowAsync();


            if (dialogEditMessage.Result == EditResult.EditOK)
            {
                // Sign in was successful.
                // get Message Detail
                // update Messages  && update DB
                // if messages.Content == "" , delete message
                // else update message
                var index = Messages.IndexOf(messageDetail);
                ItemMessageDetail editResult = dialogEditMessage.MessageDetail;
                if (index >= 0)
                {
                    if (editResult.Content == "")
                    {
                        // show new dialog for confirm delete
                        Debug.WriteLine("Show new dialog for confirm delete");

                        Messages.RemoveAt(index);
                        new ItemMessage_DB().DeleteMessageFromDB(editResult.Id);
                    }
                    else
                    {
                        Debug.WriteLine($"Edit Message: {editResult.Content}");

                        Messages[index] = editResult;
                        ItemMessage itemMessage = new ItemMessage(editResult.Content, MessageType.Text);
                        new ItemMessage_DB().UpdateMessageToDB(itemMessage, editResult.Id);
                    }
                }

            }
            else
            {
                // Sign in failed.
                // nothing
            }
        }
        // 双击触发上面的dialog
        private void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                Debug.WriteLine("TextBlock_DoubleTapped");
                ShowDialogForEditMessage((ItemMessageDetail)textBlock.DataContext);
            }
        }
        // 修改后更新历史数据
        private void UpdateAndSaveImageMessage(ItemMessageDetail messageDetail, List<string> savePaths)
        {
            var index = Messages.IndexOf(messageDetail);

            if (index >= 0)
            {
                var willUpdateMessage = Messages[index];
                var willUpdateImgPaths = new List<string>();
                foreach (var itemImage in willUpdateMessage.itemImages)
                {
                    willUpdateImgPaths.Add(itemImage.ImagePath);
                }
                foreach (var path in savePaths)
                {
                    willUpdateImgPaths.Add(path);
                }

                // update db
                ItemMessage msg = new ItemMessage(string.Join(";", willUpdateImgPaths), MessageType.Image);
                msg.Id = willUpdateMessage.Id;
                msg.Time = willUpdateMessage.Time;
                msg.From_Who = willUpdateMessage.From_Who;
                new ItemMessage_DB().UpdateMessageToDB(msg, messageDetail.Id);

                // update message
                ItemMessageDetail msgDetail = new ItemMessageDetail(msg);

                Messages[index] = msgDetail; // update message
            }
        }


        // 拖拽时 赋予copy操作
        private void GriViewForImage_DragOver(object sender, DragEventArgs e)
        {
            // 检查拖放的数据类型
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy; // 允许复制操作
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None; // 不接受其他操作
            }
        }
        // 拖拽图片 修改历史图片记录 append
        private async void GriViewForImage_Drop(object sender, DragEventArgs e)
        {
            if (sender is GridView gridView)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    List<string> savePaths = new List<string>();
                    // 检查文件类型是图片,才允许复制操作
                    var storageItems = await e.DataView.GetStorageItemsAsync();
                    foreach (var item in storageItems)
                    {
                        if (item is StorageFile file && _isImageFile(file))
                        {
                            var savePath = await SaveImageToMediaFolder(file);
                            savePaths.Add(savePath);
                        }
                    }
                    if (savePaths.Count > 0)
                    {
                        // 更改Messages
                        // update db
                        UpdateAndSaveImageMessage((ItemMessageDetail)gridView.DataContext, savePaths);
                    }
                }
            }



        }
        // 拖拽时 检查 是否是 图片文件
        private bool _isImageFile(StorageFile file)
        {
            if (file != null)
            {
                if (file.FileType == ".jpg" || file.FileType == ".png" ||
                        file.FileType == ".bmp" || file.FileType == ".gif")
                {
                    return true;
                }
            }
            return false;
        }


        // 2025-1-6
        // 图片双击 放大
        // 图片右键 删除 -》 双击gridview Dialog包含图片多选，可删除
        // gridview 右键全部删除
        private void OpenLargeImageWindow(string imagePath)
        {
            // 检查文件是否存在
            if (System.IO.File.Exists(imagePath))
            {
                // 启动默认的图片查看器
                //建立新的系统进程    
                System.Diagnostics.Process process = new System.Diagnostics.Process();

                //设置图片的真实路径和文件名    
                process.StartInfo.FileName = imagePath;

                //设置进程运行参数，这里以最大化窗口方法显示图片。    
                process.StartInfo.Arguments = "rundl132.exe C://WINDOWS//system32//shimgvw.dll,ImageView_Fullscreen";

                //此项为是否使用Shell执行程序，因系统默认为true，此项也可不设，但若设置必须为true    
                process.StartInfo.UseShellExecute = true;

                //此处可以更改进程所打开窗体的显示样式，可以不设    
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.Start();
                process.Close();
            }
            else
            {
                // 处理文件不存在的情况
                Console.WriteLine("文件不存在: " + imagePath);
            }
        }
        private void MessageImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine($"Message Item double tapped");
            // 获取被双击的 Image 控件
            var image = sender as Image;
            if (image != null)
            {
                var data = image.DataContext as ItemImage;
                // 获取图像路径
                var imagePath = data.ImagePath; // 这里假设 Source 是一个 Uri

                // 打开大图窗口
                OpenLargeImageWindow(imagePath);
            }
        }

        private void MenuImageDelete_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_Delete_Click");
            // 1 取出 这一层的 dataContext 以及 这一个的ImagePath
            // 2 将这一层的dataContext 删掉 ImagePath
            // 3 检查删完之后 len == 0 则 删掉此消息
            //               len != 0 则 更新消息

            var menuForImageItem = Resources["MenuFlyout_ImageItem"] as MenuFlyout;
            var image = menuForImageItem.Target as Image;

            if (image != null)
            {
                var data = image.DataContext as ItemImage;
                var imagePath = data.ImagePath; // will delete
                var id = data.Id; // find the messageDetail by id in Messages

                var messageDetail = Messages.FirstOrDefault(x => x.Id == id);
                if (messageDetail != null)
                {
                    var findItemImage = messageDetail.itemImages.Find(x => x.ImagePath == imagePath);
                    var index = messageDetail.itemImages.IndexOf(findItemImage);
                    if (index >= 0)
                    {
                        messageDetail.itemImages.RemoveAt(index);
                        if (messageDetail.itemImages.Count == 0)
                        {
                            Messages.Remove(messageDetail);
                            new ItemMessage_DB().DeleteMessageFromDB(messageDetail.Id);
                        }
                        else
                        {
                            var willUpateImagePaths = messageDetail.itemImages.Select(x => x.ImagePath);
                            // update db
                            ItemMessage msg = new ItemMessage(string.Join(";", willUpateImagePaths),
                                                MessageType.Image);
                            msg.Id = messageDetail.Id;
                            msg.Time = messageDetail.Time;
                            msg.From_Who = messageDetail.From_Who;
                            new ItemMessage_DB().UpdateMessageToDB(msg, messageDetail.Id);
                            // update message
                            ItemMessageDetail msgDetail = new ItemMessageDetail(msg);

                            var indexInMessages = Messages.IndexOf(messageDetail);
                            Messages[indexInMessages] = msgDetail; // update message

                            // 最后删除这个路径的文件
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }
                }
                
            }
        }

        private void MenuItemsDelete_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Border_RightTapped");

            var menuForItems = Resources["MenuFlyout_Items"] as MenuFlyout;
            if (menuForItems != null)
            {
                var view = menuForItems.Target as Border;
                if (view != null)
                {
                    var data = view.DataContext as ItemMessageDetail;
                    // if 文本数据 ： 删除这个消息，并更新db
                    // if 文件数据（图片、音频、视频、文档）： 删除这个消息，并遍历删除这些文件，并更新db
                    if (data != null)
                    {
                        if (data.Type == MessageType.Text)
                        {
                            Messages.Remove(data);
                            new ItemMessage_DB().DeleteMessageFromDB(data.Id);
                        }
                        else
                        {
                            // 删除这个消息，并更新db
                            Messages.Remove(data);
                            new ItemMessage_DB().DeleteMessageFromDB(data.Id);
                            // 遍历删除这些文件，并更新db
                            foreach (var itemImage in data.itemImages)
                            {
                                if (System.IO.File.Exists(itemImage.ImagePath))
                                {
                                    System.IO.File.Delete(itemImage.ImagePath);
                                }
                            }
                            foreach (var itemAudio in data.itemAudios)
                            {
                                if (System.IO.File.Exists(itemAudio.AudioPath))
                                {
                                    System.IO.File.Delete(itemAudio.AudioPath);
                                }
                            }
                            foreach (var itemVideo in data.itemVideos)
                            {
                                if (System.IO.File.Exists(itemVideo.VideoPath))
                                {
                                    System.IO.File.Delete(itemVideo.VideoPath);
                                }
                            }
                            foreach (var itemDocument in data.itemDocuments)
                            {
                                if (System.IO.File.Exists(itemDocument.DocumentPath))
                                {
                                    System.IO.File.Delete(itemDocument.DocumentPath);
                                }
                            }
                        }
                    }
                }
            }


            
        }

        // 2025-1-7 (未完成)
        // 图片可以拖拽到其他应用中，或者拖拽到本地
        //    可以拖拽到文本框中，再次发送
        // 图片可以使用快捷键复制或者右键复制到剪贴板中
        private void MessageImage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Pointer ptr = e.Pointer;
            if (ptr.PointerDeviceType == PointerDeviceType.Mouse)
            {
                PointerPoint ptrPt = e.GetCurrentPoint(this);
                if (ptrPt.Properties.IsLeftButtonPressed)
                {
                    Debug.WriteLine("MessageImage_PointerPressed");
                    var image = sender as Image;
                    if (image != null)
                    {
                        var itemImage = image.DataContext as ItemImage;
                        DataPackage data = new DataPackage();
                        data.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(itemImage.ImagePath)));
                    }

                    

                }
            }

        }

        private void ScrollViewMessage_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // 1 检查是否滚动到顶部
            // 2 从数据库select 额外 10条消息，加入到Messages中
            // 3 更新滚动条位置
            if (sender is ScrollViewerView)
            {
                var scrollViewer = sender as ScrollViewer;
                Debug.WriteLine($"Scrolling to top,{scrollViewer.VerticalOffset}");
                if (scrollViewer.VerticalOffset == 0)
                {
                    Debug.WriteLine($"Scroll to top,{scrollViewer.VerticalOffset}");

                    // 获取Message最早的ID
                    long lastId = Messages[0].Id;
                    var extraMessages = new ItemMessage_DB().GetExtraMessageFromDB(lastId);
                    // 把额外的消息插入到Messages中
                    foreach (ItemMessage msg in extraMessages)
                    {
                        ItemMessageDetail itemMessageDetail = new ItemMessageDetail(msg);
                        Messages.Insert(0, itemMessageDetail);
                    }
                    
                }
            }
            }

        // 2025-1-13
        // 下拉刷新新消息（10条/次）

    }
}
