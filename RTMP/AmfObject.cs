using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTMP
{
    public class AmfObject
    {
        public Dictionary<string, double> Numbers; //NUMBERS NUMBERS NUMBERS
        public Dictionary<string, string> Strings;
        public Dictionary<string, bool> Booleans;

        public AmfObject()
        {
            Numbers = new Dictionary<string, double>();
            Strings = new Dictionary<string, string>();
            Booleans = new Dictionary<string, bool>();
        }
    }
}
