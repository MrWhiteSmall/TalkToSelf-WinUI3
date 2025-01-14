using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace demo1
{
    public class ItemMessageDetail
    {
        // id
        public long Id { get; set; }
        // 输入的内容
        public string Content { get; set; }

        // 输入的类型（文本、图片、语音、视频）
        public MessageType Type { get; set; }

        public List<ItemImage> itemImages { get; set; }
        public List<ItemAudio> itemAudios { get; set; }
        public List<ItemVideo> itemVideos { get; set; }
        public List<ItemDocument> itemDocuments { get; set; }

        // 时间戳，自动计算并格式化为 "年-月-日-时：分：秒"
        public string Time { get; set; }

        public string From_Who { get; set; }

        public ItemMessageDetail()
        {
            Id = 0;
            Content = string.Empty;
            Type = MessageType.Text;
            Time = string.Empty; // 自动计算时间
            From_Who = string.Empty;
            itemImages = new List<ItemImage>();
            itemAudios = new List<ItemAudio>();
            itemVideos = new List<ItemVideo>();
            itemDocuments = new List<ItemDocument>();
        }

        public ItemMessageDetail(ItemMessage itemMessage)
        {
            itemImages = new List<ItemImage>();
            itemAudios = new List<ItemAudio>();
            itemVideos = new List<ItemVideo>();
            itemDocuments = new List<ItemDocument>();
            if (itemMessage.Type == MessageType.Text)
            {
                Content = itemMessage.Content;
            }
            else
            {
                Content = string.Empty;
                string paths = itemMessage.Content;
                string[] pathArray = paths.Split(';');
                if (pathArray.Length > 0)
                {
                    foreach (string path in pathArray)
                    {
                        if (itemMessage.Type == MessageType.Image)
                        {
                            Type = MessageType.Image;
                            itemImages.Add(new ItemImage(itemMessage.Id,path));
                        }
                        else if (itemMessage.Type == MessageType.Audio)
                        {
                            Type = MessageType.Audio;
                            itemAudios.Add(new ItemAudio(path));
                        }
                        else if (itemMessage.Type == MessageType.Video)
                        {
                            Type = MessageType.Video;
                            itemVideos.Add(new ItemVideo(path));
                        }
                        else if (itemMessage.Type == MessageType.Document)
                        {
                            Type = MessageType.Document;
                            itemDocuments.Add(new ItemDocument(path));
                        }
                    }
                }
            }

            Id = itemMessage.Id;
            Time = itemMessage.Time;
            From_Who = itemMessage.From_Who;
        }

    }

    public class ItemImage
    {
        public long Id { get; set; }
        public string ImagePath { get; set; }

        public ItemImage(long id, string imagePath)
        {
            Id = id;
            ImagePath = imagePath;
        }
    }

    public class ItemAudio
    {
        public string AudioPath { get; set; }
        public ItemAudio(string audioPath)
        {
            AudioPath = audioPath;
        }
    }

    public class ItemVideo
    {
        public string VideoPath { get; set; }
        public ItemVideo(string videoPath)
        {
            VideoPath = videoPath;
        }
    }

    public class ItemDocument
    {
        public string DocumentPath { get; set; }
        public ItemDocument(string documentPath)
        {
            DocumentPath = documentPath;
        }
    }
}


