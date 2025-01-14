using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace demo1
{
    class DataAccess
    {
        public string dbName = "Demo2.db";
        public async void InitializeDatabase()
        {
            if (!string.IsNullOrEmpty(dbName))
            {
                _ = await ApplicationData.Current.LocalFolder
                        .CreateFileAsync(dbName, CreationCollisionOption.OpenIfExists);
            }
            
        }

        

    }
}
