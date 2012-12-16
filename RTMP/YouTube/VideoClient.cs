/*
 * SPECIAL THANKS TO THE CREATOR OF YOUTUBE-EXTRACTOR, CHECK IT OUT HERE: https://github.com/flagbug/YoutubeExtractor
 * Although I modified it a bit to work with streaming, sorry doods.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.IO;
using MiscUtil.IO;
using MiscUtil.Conversion;
using YoutubeExtractor; //Thanks again <3



namespace RTMP.YouTube
{
    public class VideoClient
    {
        public delegate void AddTagDeleage(FlvTag tag);

        public event AddTagDeleage AddedTag;

        //FLV Related Checks
        public List<byte[]> Tags; 
        private VideoDownloader downloader;

        public static BigEndianBitConverter convert = new BigEndianBitConverter();

        public void GrabVideo(string link)
        {
            AddedTag = null;

            Tags = new List<byte[]>();

            var videoInfos = DownloadUrlResolver.GetDownloadUrls(link);
            var video = videoInfos.First(info => info.VideoType == VideoType.Flash);

            downloader = new VideoDownloader(video, "");
            downloader.Execute(true);
        }

        public bool Update()
        {
            var bitStream = downloader.DownloadStream;
            var reader = new EndianBinaryReader(EndianBitConverter.Big, bitStream);

            if(bitStream != null)
            {
                var stream = new StreamReader(bitStream);
                {
                    reader.ReadBytes(3); //"FLV"
                    reader.ReadBytes(6); //Other starter shit

                    while (true)
                    {
                        try
                        {
                            var footer = reader.ReadUInt32();
                            var tag = new FlvTag();
                            tag.Load(reader);

                            AddedTag(tag);

                        }
                        catch (Exception)
                        {
                            reader.Close();
                            //End of stream
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
