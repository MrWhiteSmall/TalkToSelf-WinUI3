using System;
using demo1;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Windows.Graphics;//注意要以上操作正确，否则会报错


namespace TalkToSelfDemo1
{
    public sealed partial class MainWindow : Window
    {
        Microsoft.UI.Windowing.AppWindow m_appWindow;
        public MainWindow()
        {
            
            this.InitializeComponent();


            ContentPage.Navigate(typeof(Page_TalkToSelf));

            m_appWindow = GetAppWindowForCurrentWindow();
            //m_appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);  // This line
            // 调整窗口位置和大小，以屏幕像素为单位
            m_appWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 480, _Height: 850));
        }

        private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
        }


    }
}
