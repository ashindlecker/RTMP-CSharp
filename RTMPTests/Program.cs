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
using TwitchSharp;

namespace RTMPTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = File.OpenRead("Test.flv");
            var Flv = new RTMP.Flv();
            Flv.Load(file);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            var client = new Client();

            client.PublisherId = "live_WhateverYourKeyIs";
            client.Connect("199.9.255.53");
            client.Start();
            var count = 0;

            const int tagSend = 70;

            var streamer = new FlvStreamer(Flv);
            streamer.Start();
            while(true)
            {
                client.Update();
                streamer.Update(client);
            }
        }
    }
}
