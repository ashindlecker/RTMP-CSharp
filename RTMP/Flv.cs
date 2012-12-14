using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MiscUtil.Conversion;
using MiscUtil.IO;

namespace RTMP
{
    public class FlvTag
    {
        public enum TagTypes : byte
        {
            MetaData = 0x12,
            Audio = 0x08,
            Video = 0x09,
        }

        public TagTypes TagType;
        public byte[] Length;

        public int LengthValue
        {
            get
            {
                var convert = new BigEndianBitConverter();
                var bytes = new byte[4] { 0x00, Length[0], Length[1], Length[2] };
                return convert.ToInt32(bytes, 0);
            }
        }

        public byte[] TimeStamp;    //NOTE: THAT THE TIMESTAMP IS 4 BYTES BUT IN RTMP IT'S 3 BYTES WHEN SENT
        public uint TimeStampValue { get; set; }
        public byte[] TimeStampNormal;

        public byte[] StreamId; //NOTE: THAT THE STREAM ID IS 3 BYTES BUT IN RTMP IT'S 4 BYTES WHEN SENT
        public uint StreamIdIdValue { get; set; }

        public byte[] Data;

        public FlvTag()
        {
            TagType = TagTypes.Video;
            Length = null;
            TimeStamp = null;
            Data = null;
            TimeStampNormal = null;
        }

        public void Load(EndianBinaryReader reader)
        {
            
            TagType = (TagTypes)reader.ReadByte();
            Length = reader.ReadBytes(3);
            TimeStamp = reader.ReadBytes(4);
            StreamId = reader.ReadBytes(3);
            Data = reader.ReadBytes((int)LengthValue);

            //BECAUSE SOMEONE AT ADOBE THOUGHT IT'D BE A GRAND IDEA TO DO MIXED ENDIAN
            TimeStampNormal = new byte[TimeStamp.Length];
            TimeStampNormal[0] = TimeStamp[3];
            TimeStampNormal[1] = TimeStamp[0];
            TimeStampNormal[2] = TimeStamp[1];
            TimeStampNormal[3] = TimeStamp[2];
        }
    }

    public class Flv
    {
        public enum BitmaskTypes : byte
        {
            Audio = 0x04,
            Video = 0x01,
            AudioAndVideo = 0x05,
        }

        public BitmaskTypes Bitmask { get; private set; }
        public uint HeaderSize { get; private set; }
        public byte Version { get; private set; }

        public List<FlvTag> Tags;

        public Flv()
        {
            Version = 0;
            Bitmask = BitmaskTypes.Audio;
            Version = 0;
            Tags = new List<FlvTag>();
        }


        public void Load(Stream stream)
        {
            var reader = new EndianBinaryReader(EndianBitConverter.Big, stream);

            //Header
            reader.ReadBytes(3); // "FLV"
            Version = reader.ReadByte();
            Bitmask = (BitmaskTypes) reader.ReadByte();
            HeaderSize = reader.ReadUInt32();

            //Start reading tags
            while(stream.Position < stream.Length)
            {
                var footer = reader.ReadUInt32();
                if(stream.Position >= stream.Length)
                {
                    break;
                }
                var tag = new FlvTag();
                
                tag.Load(reader);
                Tags.Add(tag);
            }
        }
    }
}
