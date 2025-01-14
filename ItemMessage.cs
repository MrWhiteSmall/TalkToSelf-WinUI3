using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Windows.Storage;

namespace demo1
{
    // 枚举表示输入内容的类型
    public enum MessageType
    {
        Text,
        Image,
        Audio,
        Video,
        Document,
    }

    public class ItemMessage
    {
        // id for update select delete
        public long Id { get; set; }

        // 输入的内容
        public string Content { get; set; }

        // 输入的类型（文本、图片、语音、视频）
        public MessageType Type { get; set; }

        // 时间戳，自动计算并格式化为 "年-月-日-时：分：秒"
        public string Time { get; set; }

        public string From_Who { get; set; }

        public ItemMessage()
        {
            Id = 0;
            Content = string.Empty;
            Type = MessageType.Text;
            Time = string.Empty; // 自动计算时间
            From_Who = string.Empty;
        }
        // 构造函数，初始化内容和类型，并自动设置时间
        public ItemMessage(string content, MessageType type)
        {
            Id = 0;
            Content = content;
            Type = type;
            Time = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"); // 自动计算时间
            From_Who = "user";
        }

        // 将ItemMessage转换为JSON格式的字符串
        public static string ToJson(ItemMessage message)
        {
            return JsonConvert.SerializeObject(new
            {
                content = message.Content,
                type = message.Type.ToString(),
            });
        }

        // 从JSON字符串创建ItemMessage对象
        public static ItemMessage FromJson(string json)
        {
            try
            {
                var temp = JsonConvert.DeserializeObject<dynamic>(json);
                if (temp == null)
                {
                    return new ItemMessage();
                }
                // Deserialize the message type and map to the correct enum
                MessageType type = Enum.TryParse<MessageType>((string)temp.type, out var parsedType) ? parsedType : MessageType.Text;

                return new ItemMessage((string)temp.content, type)
                {
                    Time = string.Empty,
                    From_Who = string.Empty,
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return new ItemMessage();
            }
            
        }
    }
}
