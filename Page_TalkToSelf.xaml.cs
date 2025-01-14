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
        // ��ʼ����Ϣ
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
        // ��ʼ��RefreshContainer
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

                // ��ȡMessage�����ID
                long lastId = Messages[0].Id;
                var extraMessages = new ItemMessage_DB().GetExtraMessageFromDB(lastId);
                // �Ѷ������Ϣ���뵽Messages��
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




        // �������������ײ�
        private void InitializeScrollView(object sender, RoutedEventArgs e)
        {
            // ��ҳ�������ɺ󣬹�����ScrollViewer�ĵײ�
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

        // ������������Ϣ
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

        // �������ı��� ��ק ʱ������copy����
        private void InputTextBox_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        // ��������ק �� �ı���
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
        // ����ͼƬ�� media �ļ���
        private async Task<string> SaveImageToMediaFolder(StorageFile file)
        {
            // ��ȡӦ�õı��ش洢�ļ���
            //StorageFolder saveFolder = await config.GetLocalStorageFolder();
            StorageFolder saveFolder = await config.GetOrCreatePictureFolderAsync();
            if (saveFolder == null)
            {
                Debug.WriteLine("Error: Could not create or access the media folder");
                return null;
            }

            // ����Ψһ���ļ�����uuid_ʱ��.png
            var uuid = Guid.NewGuid().ToString();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // ��ȡͼƬ����չ��
            var fileExtension = file.FileType.ToLower(); // ��ȡ�ļ���չ������ .jpg, .png��
            var fileName = $"{uuid}_{timestamp}{fileExtension}";

            string dstPath = Path.Combine(saveFolder.Path, fileName);
            // �����ļ����������� C:\Users\server\AppData\Local\Packages\1feb55ca-01cd-4c8c-8e06-71214ae36130_eh8yxnpcfzfbc\LocalState\media\a3f7dac2-92a3-4e76-9ccf-108cae4c033d_20241231_094440.png
            //await file.CopyAsync(saveFolder, fileName,NameCollisionOption.ReplaceExisting);
            File.Copy(file.Path, dstPath);

            // ���ر�����ļ�·��
            return Path.Combine(saveFolder.Path, fileName);
        }

        // ���ctrol shift�Ƿ񱻵��
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
        // ͳһ����ͼƬ��������
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
         ���а� �����ļ� ������ ����ctrl + V
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
                // �Զ����ļ����ͷ��� ���ݲ�ͬ���͵��ļ������ز�ͬ��ö��ֵ
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
                // ����ճ����ͼƬ
                var bitmap = clipboardContent.GetBitmapAsync().AsTask().Result;

                var stream = bitmap.OpenReadAsync().AsTask().Result;

                // ����λͼ���ļ�
                // ����һ���ļ���д��λͼ����
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
        // �����Ŀ�ݼ�����
        private async void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {

            bool isShiftPressed = _IsShiftPressed();
            bool isCtrlPressed = _IsControlPressed();
            // shift enter = warp
            if (isShiftPressed && e.Key == Windows.System.VirtualKey.Enter)
            {
                // ���뻻�з�
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // ��ȡ���λ��
                    int caretIndex = textBox.SelectionStart;

                    // �ڹ��λ�ò��뻻�з�
                    textBox.Text = textBox.Text.Insert(caretIndex, "\n");

                    // ���¹��λ��
                    textBox.SelectionStart = caretIndex + 1;

                    // ȡ���¼���Ĭ�ϴ���
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

        // ͼƬ������Ͱ�ť
        public async Task<StorageFile> OpenImageFileSelected(List<string> available_image_extention)
        {
            var filePicker = new FileOpenPicker();

            // ��ȡ��ǰ���ڵľ������ WinUI ������Ӧ���У�
            IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            // ���ļ���ѡ�����봰�ھ������
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail; // ������ͼ��ͼ��ʾ�ļ�

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

        // ˫���޸��ı���ʷ��¼ dialog
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
        // ˫�����������dialog
        private void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                Debug.WriteLine("TextBlock_DoubleTapped");
                ShowDialogForEditMessage((ItemMessageDetail)textBlock.DataContext);
            }
        }
        // �޸ĺ������ʷ����
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


        // ��קʱ ����copy����
        private void GriViewForImage_DragOver(object sender, DragEventArgs e)
        {
            // ����Ϸŵ���������
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy; // �����Ʋ���
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None; // ��������������
            }
        }
        // ��קͼƬ �޸���ʷͼƬ��¼ append
        private async void GriViewForImage_Drop(object sender, DragEventArgs e)
        {
            if (sender is GridView gridView)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    List<string> savePaths = new List<string>();
                    // ����ļ�������ͼƬ,�������Ʋ���
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
                        // ����Messages
                        // update db
                        UpdateAndSaveImageMessage((ItemMessageDetail)gridView.DataContext, savePaths);
                    }
                }
            }



        }
        // ��קʱ ��� �Ƿ��� ͼƬ�ļ�
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
        // ͼƬ˫�� �Ŵ�
        // ͼƬ�Ҽ� ɾ�� -�� ˫��gridview Dialog����ͼƬ��ѡ����ɾ��
        // gridview �Ҽ�ȫ��ɾ��
        private void OpenLargeImageWindow(string imagePath)
        {
            // ����ļ��Ƿ����
            if (System.IO.File.Exists(imagePath))
            {
                // ����Ĭ�ϵ�ͼƬ�鿴��
                //�����µ�ϵͳ����    
                System.Diagnostics.Process process = new System.Diagnostics.Process();

                //����ͼƬ����ʵ·�����ļ���    
                process.StartInfo.FileName = imagePath;

                //���ý������в�������������󻯴��ڷ�����ʾͼƬ��    
                process.StartInfo.Arguments = "rundl132.exe C://WINDOWS//system32//shimgvw.dll,ImageView_Fullscreen";

                //����Ϊ�Ƿ�ʹ��Shellִ�г�����ϵͳĬ��Ϊtrue������Ҳ�ɲ��裬�������ñ���Ϊtrue    
                process.StartInfo.UseShellExecute = true;

                //�˴����Ը��Ľ������򿪴������ʾ��ʽ�����Բ���    
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.Start();
                process.Close();
            }
            else
            {
                // �����ļ������ڵ����
                Console.WriteLine("�ļ�������: " + imagePath);
            }
        }
        private void MessageImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine($"Message Item double tapped");
            // ��ȡ��˫���� Image �ؼ�
            var image = sender as Image;
            if (image != null)
            {
                var data = image.DataContext as ItemImage;
                // ��ȡͼ��·��
                var imagePath = data.ImagePath; // ������� Source ��һ�� Uri

                // �򿪴�ͼ����
                OpenLargeImageWindow(imagePath);
            }
        }

        private void MenuImageDelete_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_Delete_Click");
            // 1 ȡ�� ��һ��� dataContext �Լ� ��һ����ImagePath
            // 2 ����һ���dataContext ɾ�� ImagePath
            // 3 ���ɾ��֮�� len == 0 �� ɾ������Ϣ
            //               len != 0 �� ������Ϣ

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

                            // ���ɾ�����·�����ļ�
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
                    // if �ı����� �� ɾ�������Ϣ��������db
                    // if �ļ����ݣ�ͼƬ����Ƶ����Ƶ���ĵ����� ɾ�������Ϣ��������ɾ����Щ�ļ���������db
                    if (data != null)
                    {
                        if (data.Type == MessageType.Text)
                        {
                            Messages.Remove(data);
                            new ItemMessage_DB().DeleteMessageFromDB(data.Id);
                        }
                        else
                        {
                            // ɾ�������Ϣ��������db
                            Messages.Remove(data);
                            new ItemMessage_DB().DeleteMessageFromDB(data.Id);
                            // ����ɾ����Щ�ļ���������db
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

        // 2025-1-7 (δ���)
        // ͼƬ������ק������Ӧ���У�������ק������
        //    ������ק���ı����У��ٴη���
        // ͼƬ����ʹ�ÿ�ݼ����ƻ����Ҽ����Ƶ���������
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
            // 1 ����Ƿ����������
            // 2 �����ݿ�select ���� 10����Ϣ�����뵽Messages��
            // 3 ���¹�����λ��
            if (sender is ScrollViewerView)
            {
                var scrollViewer = sender as ScrollViewer;
                Debug.WriteLine($"Scrolling to top,{scrollViewer.VerticalOffset}");
                if (scrollViewer.VerticalOffset == 0)
                {
                    Debug.WriteLine($"Scroll to top,{scrollViewer.VerticalOffset}");

                    // ��ȡMessage�����ID
                    long lastId = Messages[0].Id;
                    var extraMessages = new ItemMessage_DB().GetExtraMessageFromDB(lastId);
                    // �Ѷ������Ϣ���뵽Messages��
                    foreach (ItemMessage msg in extraMessages)
                    {
                        ItemMessageDetail itemMessageDetail = new ItemMessageDetail(msg);
                        Messages.Insert(0, itemMessageDetail);
                    }
                    
                }
            }
            }

        // 2025-1-13
        // ����ˢ������Ϣ��10��/�Σ�

    }
}
