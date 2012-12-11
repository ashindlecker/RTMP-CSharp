using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiscUtil.Conversion;
using RTMP;
using System.IO;
using MiscUtil.IO;

namespace RTMPTests
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var memory = new MemoryStream();
            var writer = new EndianBinaryWriter(EndianBitConverter.Big, memory);

            RTMP.Client client = new Client();
            client.Connect("199.9.255.53");
            client.Start();

            bool testSend = true;
            while(true)
            {
                client.Update();

                if(client.CurrentState == Client.ClientStates.Normal)
                {
                    if(testSend)
                    {
                        testSend = false;
                        var testPacket = new AmfWriter();
                        testPacket.WriteString("createStream");
                        testPacket.WriteNumber(2.0);
                        testPacket.WriteNull();
                        client.SendAmf(testPacket);

                        client.Ping();
                    }
                }
            }
        }
    }
}
