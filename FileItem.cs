﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demo1
{
    public class FileItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";

        public override string ToString()
        {
            return FileName;
        }
    }
}
