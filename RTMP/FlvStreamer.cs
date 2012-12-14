using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RTMP
{
    public class FlvStreamer
    {
        public Flv Video;

        private Stopwatch timer;
        private uint tagId;
        private List<FlvTag> sendTags; 

        public uint SendRate;
        public byte TagsPerMessage;

        
        public FlvStreamer(Flv vid = null)
        {
            tagId = 0;

            Video = vid;

            TagsPerMessage = 70;
            SendRate = 550;

            sendTags = new List<FlvTag>();

            timer = new Stopwatch();
            
        }

        public void Reset()
        {
            tagId = 0;
            timer.Reset();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public void Restart()
        {
            tagId = 0;
            timer.Restart();
        }

        public void Start()
        {
            timer.Start();
        }

        public void Update(Client[] clients)
        {
            if(Video.Tags.Count > tagId && timer.ElapsedMilliseconds >= SendRate)
            {
                timer.Restart();

                
                for(uint count = 0; tagId < Video.Tags.Count && count < TagsPerMessage; tagId++, count++)
                {
                    //Console.WriteLine(tagId);
                    sendTags.Add(Video.Tags[(int)tagId]);
                }

                for (var i = 0; i < clients.Length; i++)
                    if(clients[i].CurrentState == Client.ClientStates.Streaming)
                        clients[i].SendFlv(sendTags.ToArray());
                sendTags.Clear();
            }
        }

        public void Update(Client client)
        {
            Update(new Client[1] {client});
        }
    }
}
