using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTMP
{
    //mfw I finally understand what something in this protocol means
    public class DataFrame
    {
        //DOUBLES DOUBLES DOUBLES
        public double Duration;
        public double Width;
        public double Height;
        public double VideoDataRate;
        public double FrameRate;
        public double VideoCodeCid;
        public double AudioDataRate;
        public double AudioSampleRate;
        public double AudioSampleSize;
        public bool Stereo;
        public double AudioCodecId;
        public string Encoder;
        public double FileSize;

        public DataFrame()
        {
            Duration = 0;
            Width = 100;
            Height = 100;
            VideoDataRate = 732.421875;
            FrameRate = 1000;
            VideoCodeCid = 7;
            AudioDataRate = 250;
            AudioSampleRate = 44100;
            AudioSampleSize = 16;
            Stereo = true;
            AudioCodecId = 10;
            Encoder = "Lavf54.25.100";
            FileSize = 0;
        }

        public RTMP.AmfWriter GetAmf()
        {
            var amfWriter = new RTMP.AmfWriter();

            amfWriter.WriteString("@setDataFrame");
            amfWriter.WriteString("onMetaData");

            var dataOjbect = new RTMP.AmfObject();
            dataOjbect.Numbers.Add("duration", Duration);
            dataOjbect.Numbers.Add("width", Width);
            dataOjbect.Numbers.Add("height", Height);
            dataOjbect.Numbers.Add("videodatarate", VideoDataRate);
            dataOjbect.Numbers.Add("framerate", FrameRate);
            dataOjbect.Numbers.Add("videocodecid", VideoCodeCid);
            dataOjbect.Numbers.Add("audiodatarate", AudioDataRate);
            dataOjbect.Numbers.Add("audiosamplerate", AudioSampleRate);
            dataOjbect.Numbers.Add("audiosamplesize", AudioSampleSize);
            dataOjbect.Booleans.Add("stereo", Stereo);
            dataOjbect.Numbers.Add("audiocodecid", AudioCodecId);
            dataOjbect.Strings.Add("encoder", Encoder);
            dataOjbect.Numbers.Add("filesize", FileSize);
            amfWriter.WriteObject(dataOjbect, true);

            return amfWriter;
        }

    }
}
