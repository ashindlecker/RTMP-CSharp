using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTMP
{
    public enum Amf0Types : byte
    {
        Number = 0x00,
        Boolean = 0x01,
        String = 0x02,
        Object = 0x03,
        Null = 0x05,
        Array = 0x08,
        ObjectEnd = 0x09,
    }

    public enum RtmpMessageTypeId : byte
    {
        SetPacketSize = 0x01,
        Ping = 0x04,
        ServerBandwidth = 0x05,
        ClientBandwitdh = 0x06,
        Audio = 0x08,
        Video = 0x09,
        AMF3 = 0x11,
        Invoke = 0x12,
        AMF0 = 0x14,
    }
}
