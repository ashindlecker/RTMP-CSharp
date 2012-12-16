using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiscUtil.Conversion;
using RTMP;
using System.IO;
using MiscUtil.IO;
using System.Diagnostics;
using RTMP.YouTube;
using TwitchSharp;
using System.Web;
using System.Net;

namespace RTMPTests
{
    class Program
    {
        static void Main(string[] args)
        {
            FlvStreamer streamer = new FlvStreamer();

            var client = new Client();

            client.PublisherId = "live_YourStreamID";
            client.Connect("199.9.255.53");
            client.Start();


            var vclient = new VideoClient();
            vclient.GrabVideo("http://www.youtube.com/watch?v=at68PMbgyhw");
            vclient.AddedTag += delegate(FlvTag tag)
                                    {
                                        client.SendFlv(new FlvTag[1] {tag});
                                    };


            while(true)
            {
                client.Update();
                if(client.CurrentState == Client.ClientStates.Streaming)
                    vclient.Update();
            }
        }
    }
}
