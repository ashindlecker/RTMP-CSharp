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
        
        public enum ClientStates
        {
            None,
            Handshaking,
            HandshakingClientDone,
            Normal,
        }

        public ClientStates CurrentState { get; private set; }

        private Random random;
        private TcpClient tcpClient;
        private byte[] serverS1RandomBytes;

        private MemoryStream sMemory;
        private BinaryWriter sWriter;

        public Client()
        {
            CurrentState = ClientStates.None;
            tcpClient = new TcpClient();
            random = new Random();
            serverS1RandomBytes = new byte[HANDSHAKE_RAND_LENGTH];

            sMemory = new MemoryStream();
            sWriter = new BinaryWriter(sMemory);
        }

        public void Connect(string ip, int port = 1935)
        {
            tcpClient.Connect(ip, port);
            tcpClient.NoDelay = true;
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
            random.NextBytes(byteBuffer);

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

        public void SendC2Handshake()
        {
            tcpClient.GetStream().Write(serverS1RandomBytes, 0, serverS1RandomBytes.Length);
            CurrentState = ClientStates.HandshakingClientDone;
        }

        public void SendAmf(AmfWriter amf)
        {
            sendMessage(amf.GetByteArray(), RtmpMessageTypeId.AMF0);
        }

        public void Ping()
        {
            sendMessage(new byte[0]{}, RtmpMessageTypeId.Ping );
        }

        private void sendMessage(byte[] data, RtmpMessageTypeId messageType)
        {
            const byte chunkHeaderType = 0x03;
            var timeStampDelta = new byte[3] { 0x00, 0x0b, 0x68 };

            //The packet length is only 3 bytes long when sent, so the last byte of the integer needs to be cut off
            var packetLengthBytes = new byte[3];
            {
                var packetLengthValue = data.Length;
                var converter = new BigEndianBitConverter();

                var packetLengthBytesFull = converter.GetBytes(packetLengthValue);
                for (var i = 0; i < packetLengthBytes.Length; i++)
                {
                    packetLengthBytes[i] = packetLengthBytesFull[i + 1];
                }
            }

            const int streamId = 0x00;

            var memory = new MemoryStream();
            var writer = new EndianBinaryWriter(EndianBitConverter.Big, memory);
            writer.Write(chunkHeaderType);
            writer.Write(timeStampDelta);
            writer.Write(packetLengthBytes);
            writer.Write((byte)messageType);
            writer.Write(streamId);
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

                if (CurrentState == ClientStates.HandshakingClientDone)
                {
                    CurrentState = ClientStates.Normal;
                }
            }
        }
    }
}
