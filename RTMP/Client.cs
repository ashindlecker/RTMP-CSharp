using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using MiscUtil.Conversion;
using MiscUtil.IO;

namespace RTMP
{
    public class Client
    {
        public const ushort HANDSHAKE_RAND_LENGTH = 1536;
        public const byte PROTOCOL_VERSION = 0x03;

        public int StreamId;
        public string PublisherId;

        public DataFrame MyDataFrame;
        BigEndianBitConverter converter = new BigEndianBitConverter();
        public uint CurrentChunkSize { get; private set; }
        public enum ClientStates
        {
            None,
            Handshaking,
            WaitingForAcknowledge,
            WaitForPeerBandwidth,
            WaitForStreamBeginControl,
            WaitForConnectResult,
            WaitForCreateStreamResponse,
            WaitForPublishStreamBeginResult,
            Streaming,
        }

        public ClientStates CurrentState { get; private set; }

        private Random random;
        private TcpClient tcpClient;
        private byte[] serverS1RandomBytes;

        private MemoryStream sMemory;
        private BinaryWriter sWriter;

        public Client()
        {
            CurrentChunkSize = 0;
            MyDataFrame = new DataFrame();

            CurrentState = ClientStates.None;
            tcpClient = new TcpClient();
            random = new Random();
            serverS1RandomBytes = new byte[HANDSHAKE_RAND_LENGTH];

            sMemory = new MemoryStream();
            sWriter = new BinaryWriter(sMemory);

            StreamId = 1;
            PublisherId = "";
        }

        public void Connect(string ip, int port = 1935)
        {
            tcpClient.Connect(ip, port);
            tcpClient.NoDelay = false;
            CurrentState = ClientStates.Handshaking;
        }

        public void Start()
        {
            SendC0Handshake();
            SendC1Handshake();
        }

        private void SendC0Handshake()
        {
            tcpClient.GetStream().Write(new byte[1]{PROTOCOL_VERSION}, 0, 1);
        }

        private void SendC1Handshake()
        {
            var byteBuffer = new byte[HANDSHAKE_RAND_LENGTH];
            for(var i = 0; i < byteBuffer.Length; i++)
            {
                byteBuffer[i] = 0x00;
            }
            //random.NextBytes(byteBuffer);

            tcpClient.GetStream().Write(byteBuffer, 0, byteBuffer.Length);
        }

        private void ParseS1Handshake()
        {
            sMemory.Position = 0;
            var reader = new BinaryReader(sMemory);

            if(reader.ReadByte() != PROTOCOL_VERSION)
            {
                throw new Exception("PROTOCOL DOES NOT MATCH");
            }
            for(var i = 0; i < serverS1RandomBytes.Length; i++)
            {
                serverS1RandomBytes[i] = reader.ReadByte();
            }

            SendC2Handshake();
        }

        private void SendC2Handshake()
        {
            tcpClient.GetStream().Write(serverS1RandomBytes, 0, serverS1RandomBytes.Length);
            Connect("app");
            CurrentState = ClientStates.WaitingForAcknowledge;

            HandshakeOver();
        }

        public void SendAmf(AmfWriter amf)
        {
            sendMessage(amf.GetByteArray(), RtmpMessageTypeId.AMF0);
        }

        public void SendChunkSize(uint chunkSize)
        {
            sendMessage(converter.GetBytes(chunkSize), RtmpMessageTypeId.SetChunkSize);
            CurrentChunkSize = chunkSize;
        }

        public void Stop()
        {
            var amfWriter = new AmfWriter();
            amfWriter.WriteString("deleteStream");
            amfWriter.WriteNumber(7);
            amfWriter.WriteNull();
            amfWriter.WriteNumber(1);
            SendAmf(amfWriter);
            
            tcpClient.Close();
        }

        private void Connect(string type = "app")
        {
            var writer = new AmfWriter();
            writer.WriteString("connect");
            writer.WriteNumber(1);
            var connectObject = new AmfObject();
            connectObject.Strings.Add("app", "app");
            writer.WriteObject(connectObject);
            SendAmf(writer);
        }

        private void SendWindowAcknowledgementSize()
        {
            SendChunkSize(5000);
        }

        private void createStream()
        {
            var writer = new AmfWriter();
            writer.WriteString("createStream");
            writer.WriteNumber(4);
            writer.WriteNull();
            SendAmf(writer);
        }

        private void publish(string id)
        {
            var writer = new AmfWriter();
            writer.WriteString("publish");
            writer.WriteNumber(0);
            writer.WriteNull();
            writer.WriteString(id);
            writer.WriteString("live");
            SendAmf(writer);
        }

        public void SendDebugVideoData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(0x17);
            var bytes =
                //Helper.StringToByteArray(
                //  "FF0000c8000003b6419af0344c414ffe07e2511173ef4fa8e5fed86bf8c15b7a579ffd50464c7f3115705c306bccce001a5e7c94fda3e03784dac50bb3d23f492d3764fd91fbdfa039d37bc2077d60dd68c77b64e4ae08e424a5c397f07525c30508dcf55430007048b6cccd9673704fc3f9781f0daea4e4d0b871730f89be10e5f2d32f673b21aefc871484efe6eaac122861d7263fe99e043ed891400644b267c356dbd73fb3400b16c79c9f1ae206e209325b5982729a4d4d414b6bc1b8d5e93c5dcfffa5550f445da3f3d6012e74dc5af7a6ce77edc04bdcf7cdbfe4a826b7ddaee5c85d5b3618a265b74ed3d0614df141e88f349ca229115b59ca7ad88aa302bc0a8fd25ad9c842b6b3eaa8e338f19df2911ab491d16daec91ca07dcb06b3e72fc045b24c83ec7a7c13a0497b6ac0f29bfb71bf777f3a7064291d5bf0b79c420922b84e4bf57fc843370b6a346de6b16f684450f2118cab739d03fa3c35971c657af3b713ed24de85497a6a3f3e1671fe1b7556cc03adc9a5375742eb77fed13607da8ca193a838aa2b61034717766f724bdb83d2970996e6cad33583dd161545c78b5e0a296bc309eb11a248af8ead8629213349ddd85c026d795f03af0c9a1e56452afcc3295de62d7046722ab223f0761d887ef818471c2c80dfc54286d5723f969139f8a0b5f9013bf432d03a2853db77b63f403adfaed29299a85bb99b5f9f1d781e0a48e8445622fc65d1e33708fc7cbd91a4ea128f8abbd0118fc8111dc69deff1e3ac48a0482a9c7b1124bd7e0db87cfa04403c6708c8ee1c41d88c1cf6a20f78d5503eb8c9c77cfa0c34faa99469065885bd0688004217c4a52ab887407b1729dcb851d42e214997d1d62b545de1c2796368f91324766378e57035ce54c2591fadb2a618cb3cab1893a9306b373f43b6b88531314107fc7c0b96822c22b2e688a0c0a685f3e1d88c0e7cc0d1ebe98f01e540576f4d91700e8d4290bf524b533fe0432273aed4a6d124ba2fb337662aae1320337dbd9fe5678f660b66027851ad4b4aa5c604aaf5e6d261acd6e6ca79d8ed1145b067cef9d3b676523f40222a38538b6409498d9b7a3dc43ce3e8025962f6a82088d57f7183e1094908e75bc3e983ab7d67fe5c68fce8ee446e273635fca2e19c6a6fd2aabbca96ef080fcb227529a1fc7fcbd96a2bcace9c5b7e10142802d51cad4d2a5fa0962a182c2ff5f1cde8fe8d83ada401901a410390b872316a2a35b6ab900fdc7b1ffe0635d8f54997b274e36eea1eadcc77988cffdaa726c611ad343780105d615497219f55647b69c9b6c839c8f574266478956c4ccc3f8757770954f07981");
                Helper.StringToByteArray("FFFF");
            writer.Write(bytes);
            sendMessage(memory.ToArray(), RtmpMessageTypeId.Video);

        }
        public void SendDebugAudioData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(0x17);
            var bytes =
                //Helper.StringToByteArray(
                //  "FF0000c8000003b6419af0344c414ffe07e2511173ef4fa8e5fed86bf8c15b7a579ffd50464c7f3115705c306bccce001a5e7c94fda3e03784dac50bb3d23f492d3764fd91fbdfa039d37bc2077d60dd68c77b64e4ae08e424a5c397f07525c30508dcf55430007048b6cccd9673704fc3f9781f0daea4e4d0b871730f89be10e5f2d32f673b21aefc871484efe6eaac122861d7263fe99e043ed891400644b267c356dbd73fb3400b16c79c9f1ae206e209325b5982729a4d4d414b6bc1b8d5e93c5dcfffa5550f445da3f3d6012e74dc5af7a6ce77edc04bdcf7cdbfe4a826b7ddaee5c85d5b3618a265b74ed3d0614df141e88f349ca229115b59ca7ad88aa302bc0a8fd25ad9c842b6b3eaa8e338f19df2911ab491d16daec91ca07dcb06b3e72fc045b24c83ec7a7c13a0497b6ac0f29bfb71bf777f3a7064291d5bf0b79c420922b84e4bf57fc843370b6a346de6b16f684450f2118cab739d03fa3c35971c657af3b713ed24de85497a6a3f3e1671fe1b7556cc03adc9a5375742eb77fed13607da8ca193a838aa2b61034717766f724bdb83d2970996e6cad33583dd161545c78b5e0a296bc309eb11a248af8ead8629213349ddd85c026d795f03af0c9a1e56452afcc3295de62d7046722ab223f0761d887ef818471c2c80dfc54286d5723f969139f8a0b5f9013bf432d03a2853db77b63f403adfaed29299a85bb99b5f9f1d781e0a48e8445622fc65d1e33708fc7cbd91a4ea128f8abbd0118fc8111dc69deff1e3ac48a0482a9c7b1124bd7e0db87cfa04403c6708c8ee1c41d88c1cf6a20f78d5503eb8c9c77cfa0c34faa99469065885bd0688004217c4a52ab887407b1729dcb851d42e214997d1d62b545de1c2796368f91324766378e57035ce54c2591fadb2a618cb3cab1893a9306b373f43b6b88531314107fc7c0b96822c22b2e688a0c0a685f3e1d88c0e7cc0d1ebe98f01e540576f4d91700e8d4290bf524b533fe0432273aed4a6d124ba2fb337662aae1320337dbd9fe5678f660b66027851ad4b4aa5c604aaf5e6d261acd6e6ca79d8ed1145b067cef9d3b676523f40222a38538b6409498d9b7a3dc43ce3e8025962f6a82088d57f7183e1094908e75bc3e983ab7d67fe5c68fce8ee446e273635fca2e19c6a6fd2aabbca96ef080fcb227529a1fc7fcbd96a2bcace9c5b7e10142802d51cad4d2a5fa0962a182c2ff5f1cde8fe8d83ada401901a410390b872316a2a35b6ab900fdc7b1ffe0635d8f54997b274e36eea1eadcc77988cffdaa726c611ad343780105d615497219f55647b69c9b6c839c8f574266478956c4ccc3f8757770954f07981");
                Helper.StringToByteArray("FFFF");
            writer.Write(bytes);
            sendMessage(memory.ToArray(), RtmpMessageTypeId.Audio);

        }

        public void SendFlv(FlvTag[] flvs)
        {
            var memory = new MemoryStream();
            var writer = new EndianBinaryWriter(EndianBitConverter.Big, memory);

            const byte chunkHeaderType = 0x03;

            var chunkCount = 0;
            for (var i = 0; i < flvs.Length; i++)
            {
                var flv = flvs[i];
                chunkCount += flv.Data.Length;
                writer.Write(chunkHeaderType);
                writer.Write(flv.TimeStamp, 0, 3);
                writer.Write(flv.Length, 0, 3);
                writer.Write((byte) flv.TagType);


                //writer.Write(new byte[] {0x00, flv.StreamId[0], flv.StreamId[1], flv.StreamId[2]});

                var streamIdBytes = converter.GetBytes(StreamId);
                for (int id = streamIdBytes.Length - 1; id >= 0; id--)
                {
                    writer.Write(streamIdBytes[id]);
                }

                writer.Write(flv.Data);
            }
            if(chunkCount > CurrentChunkSize)
                SendChunkSize((uint)chunkCount);
            tcpClient.GetStream().Write(memory.ToArray(), 0, memory.ToArray().Length);
        }

        private void sendMessage(byte[] data, RtmpMessageTypeId messageType)
        {

            const byte chunkHeaderType = 0x03;

            var timeStampDelta = new byte[3] { 0x00, 0x00, 0x10 };

            //The packet length is only 3 bytes long when sent, so the last byte of the integer needs to be cut off
            var packetLengthBytes = new byte[3];
            {
                var packetLengthValue = data.Length;

                var packetLengthBytesFull = converter.GetBytes(packetLengthValue);
                for (var i = 0; i < packetLengthBytes.Length; i++)
                {
                    packetLengthBytes[i] = packetLengthBytesFull[i + 1];
                }
            }

            var memory = new MemoryStream();
            var writer = new EndianBinaryWriter(EndianBitConverter.Big, memory);
            writer.Write(chunkHeaderType);
            writer.Write(timeStampDelta);
            writer.Write(packetLengthBytes);
            writer.Write((byte)messageType);

            var streamIdBytes = converter.GetBytes(StreamId);
            for (int i = streamIdBytes.Length - 1; i >= 0; i--)
            {
                writer.Write(streamIdBytes[i]);
            }
            writer.Write(data);
            tcpClient.GetStream().Write(memory.ToArray(), 0, memory.ToArray().Length);
        }

        public void Update()
        {
            if(tcpClient.Available != 0)
            {
                var buffer = new byte[tcpClient.Available];
                tcpClient.GetStream().Read(buffer, 0, buffer.Length);
                
                if(CurrentState == ClientStates.Handshaking)
                {
                    if(sMemory.Length < HANDSHAKE_RAND_LENGTH * 2)
                    {
                        sWriter.Write(buffer);
                        if(sMemory.Length >= HANDSHAKE_RAND_LENGTH * 2)
                        {
                            ParseS1Handshake();
                        }
                    }
                    else
                    {
                        ParseS1Handshake();
                    }
                }
                else
                {
                    var memory = new MemoryStream(buffer);
                    var reader = new EndianBinaryReader(EndianBitConverter.Big, memory);

                    while (memory.Position < memory.Length)
                    {
                        reader.ReadBytes(4); //as of now not used data
                        var bodySizeBytes = new byte[] {0, reader.ReadByte(), reader.ReadByte(), reader.ReadByte()};
                        var bodySize = converter.ToUInt32(bodySizeBytes, 0);

                        var messageId = (RtmpMessageTypeId) reader.ReadByte();
                        reader.ReadInt32(); //stream id is not needed as of now

                        switch (messageId)
                        {
                            case RtmpMessageTypeId.SetChunkSize:
                                {
                                    ParseSetChunkSize(reader.ReadInt32());
                                }
                                break;
                            case RtmpMessageTypeId.UserControlMessage:
                                {
                                    //No fawking clue why it's six bytes atm
                                    ParseUserControlMessage(reader.ReadBytes(6));
                                    if(CurrentState == ClientStates.WaitForStreamBeginControl)
                                    {
                                        //Console.WriteLine("SWITCh3");
                                        CurrentState = ClientStates.WaitForConnectResult;
                                    }
                                    if(CurrentState == ClientStates.WaitForPublishStreamBeginResult)
                                    {
                                        //Console.WriteLine("Switch6");
                                        CurrentState = ClientStates.Streaming;
                                        SendChunkSize(100);
                                    }
                                }
                                break;
                            case RtmpMessageTypeId.ServerBandwidth:
                                {
                                    if(CurrentState == ClientStates.WaitingForAcknowledge)
                                    {
                                        //Console.WriteLine("SWITCH1");
                                        SendWindowAcknowledgementSize();
                                        CurrentState = ClientStates.WaitForPeerBandwidth;
                                    }
                                    ParseServerBandwidth(reader.ReadInt32());
                                }
                                break;
                            case RtmpMessageTypeId.ClientBandwitdh:
                                {
                                    if(CurrentState == ClientStates.WaitForPeerBandwidth)
                                    {
                                        //Console.WriteLine("SWITCh2");
                                        CurrentState = ClientStates.WaitForStreamBeginControl;
                                    }
                                    ParseClientBandwidth(reader.ReadInt32(), reader.ReadByte());
                                }
                                break;
                            case RtmpMessageTypeId.Audio:
                                break;
                            case RtmpMessageTypeId.Video:
                                break;
                            case RtmpMessageTypeId.AMF3:
                                break;
                            case RtmpMessageTypeId.Invoke:
                                break;
                            case RtmpMessageTypeId.AMF0:
                                {
                                    var amfReader = new AmfReader();
                                    amfReader.Parse(reader, bodySize);

                                    if(CurrentState == ClientStates.WaitForConnectResult)
                                    {
                                        if (amfReader.amfData.Strings.Contains("_result"))
                                        {
                                            //Console.WriteLine("SWITCH4");
                                            createStream();
                                            CurrentState = ClientStates.WaitForCreateStreamResponse;
                                        }
                                    }
                                    if(CurrentState == ClientStates.WaitForCreateStreamResponse)
                                    {
                                        if (amfReader.amfData.Strings.Contains("_result"))
                                        {
                                            //Console.WriteLine("SWITCH5");
                                            publish(PublisherId);
                                            CurrentState = ClientStates.WaitForPublishStreamBeginResult;
                                        }
                                    }

                                    ParseAmf(amfReader.amfData);
                                }
                                break;
                                case RtmpMessageTypeId.Acknowledgement:
                                ParseAcknowledgement(reader.ReadInt32());
                                break;
                            default:
                                Console.WriteLine(messageId);
                                break;
                        }

                        ParseMessage(messageId, reader);
                    }
                }
            }
        }

        protected virtual void ParseAcknowledgement(int value)
        {

        }


        protected virtual void ParseSetChunkSize(int chunkSize)
        {
            //this should be done in the derived classes
        }

        protected virtual void ParseUserControlMessage(byte[] eventType)
        {
            //this should be done in the derived classes
        }

        protected virtual void ParseServerBandwidth(int amount)
        {
            //this should be done in the derived classes
        }

        protected virtual void ParseClientBandwidth(int amount, byte limitType)
        {
            //this should be done in the derived classes
        }
        
        protected virtual void ParseAmf(AmfData amf)
        {
            //this should be done in the derived classes
        }

        protected virtual void ParseMessage(RtmpMessageTypeId messageType, EndianBinaryReader reader)
        {
            //this should be done in the derived classes
        }

        protected virtual void HandshakeOver()
        {
            //this should be done in the derived classes
        }
    }
}
