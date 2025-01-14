using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using System.Diagnostics;

namespace demo1
{
    class ItemMessage_DB
    {
        string dbName = "Demo1.db";
        string tableName = "ChatMessage";

        public void CreateTable()
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                string tableCommand = $"CREATE TABLE IF NOT " +
                    $"EXISTS {tableName} (Msg_ID INTEGER PRIMARY KEY, " +
                                            "Content NVARCHAR(4096) NULL," +
                                            "Time NVARCHAR(512) NULL," +
                                            "From_Who NVARCHAR(128) NULL)";

                var createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public long AddMessageToDB(ItemMessage msg)
        {

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            long insertedId = -1;
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                var insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = $"INSERT INTO {tableName} VALUES (NULL, @Content,@Time,@From_Who); SELECT last_insert_rowid();";
                insertCommand.Parameters.AddWithValue("@Content", ItemMessage.ToJson(msg));
                insertCommand.Parameters.AddWithValue("@Time", msg.Time);
                insertCommand.Parameters.AddWithValue("@From_Who", msg.From_Who);

                insertedId = (long)insertCommand.ExecuteScalar();
            }
            return insertedId;

        }

        // 初次从数据库中读取数据
        public List<ItemMessage> GetMessageFromDB(int limitedNums = 20)
        {
            var entries = new List<ItemMessage>();
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand
                    ($"SELECT Content,Time,From_Who,Msg_ID from {tableName} ORDER BY Msg_ID DESC LIMIT {limitedNums}", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {

                    string contentJson = query.GetString(0);
                    ItemMessage tmpMessage = ItemMessage.FromJson(contentJson);
                    var itemMessage = new ItemMessage()
                    {
                        Id = query.GetInt32(3),
                        Content = tmpMessage.Content,
                        Type = tmpMessage.Type,
                        Time = query.GetString(1),
                        From_Who = query.GetString(2),
                    };
                    entries.Add(itemMessage); // 0 是 返回结果 中 列的下标 
                }

            }
            Debug.WriteLine("GetMessageFromDB: " + entries.Count);

            // return reverse entries
            entries.Reverse();
            return entries;
        }
        // 刷新时读取数据，每次额外读取最多10条
        public List<ItemMessage> GetExtraMessageFromDB(long lastId, int limitedNums = 10)
        {
            var entries = new List<ItemMessage>();
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand
                    ($"SELECT Content,Time,From_Who,Msg_ID from {tableName} WHERE Msg_ID < {lastId} ORDER BY Msg_ID DESC LIMIT {limitedNums}", db);
                SqliteDataReader query = selectCommand.ExecuteReader();
                while (query.Read())
                {
                    string contentJson = query.GetString(0);
                    ItemMessage tmpMessage = ItemMessage.FromJson(contentJson);
                    var itemMessage = new ItemMessage()
                    {
                        Id = query.GetInt32(3),
                        Content = tmpMessage.Content,
                        Type = tmpMessage.Type,
                        Time = query.GetString(1),
                        From_Who = query.GetString(2),
                    };
                    entries.Add(itemMessage); // 0 是 返回结果 中 列的下标 
                }
            }
            Debug.WriteLine("Update GetMessageFromDB: " + entries.Count);
            // return reverse entries
            entries.Reverse();
            return entries;
        }


        public void DeleteMessageFromDB(long id)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var deleteCommand = new SqliteCommand
                    ($"DELETE FROM {tableName} WHERE Msg_ID = {id}", db);
                deleteCommand.ExecuteReader();
            }
        }

        public void UpdateMessageToDB(ItemMessage itemMessage, long Id)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path,
                                         dbName);
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var updateCommand = new SqliteCommand
                    ($"UPDATE {tableName} SET Content = @Content WHERE Msg_ID = {Id}", db);
                updateCommand.Parameters.AddWithValue("@Content", ItemMessage.ToJson(itemMessage));

                updateCommand.ExecuteReader();
            }
        }
    }
}
