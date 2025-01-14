using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace demo1
{
    public enum EditResult
    {
        EditOK,
        EditCancel,
        Nothing
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DialogEditMessage : ContentDialog
    {
        public EditResult Result { get; private set; }
        public ItemMessageDetail MessageDetail { get; set; }

        ViewModel_DialogEditMessage viewModel;
        public DialogEditMessage(ItemMessageDetail messageDetail)
        {
            this.InitializeComponent();
            viewModel = new ViewModel_DialogEditMessage() { 
                Content = messageDetail.Content
            };

            MessageDetail = messageDetail;
            this.DataContext = viewModel;
            
            this.Opened += CustomOpenOperation;
            this.Closed += CustomeCloseOperation;
        }

        

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Result = EditResult.EditOK;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Result = EditResult.EditCancel;
        }

        private void CustomOpenOperation(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            this.Result = EditResult.Nothing;
        }
        private void CustomeCloseOperation(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            this.MessageDetail.Content = viewModel.Content;
        }

    }
}
