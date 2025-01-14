using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demo1
{
    class ViewModel_DialogEditMessage:BindableBase
    {
        private string _content = "";
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                Debug.WriteLine($"update content {value}");
                Debug.WriteLine($"update content {_content}");
                OnPropertyChanged();
            }
        }
    }
}
