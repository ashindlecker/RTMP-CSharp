using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiscUtil.IO;
using MiscUtil.Conversion;

namespace RTMP
{
    public class AmfData
    {
        public List<AmfObject> Objects;
        public List<string> Strings;
        public List<double> Numbers;
        public List<bool> Booleans;
        public uint Nulls;
        public AmfData()
        {
            Objects = new List<AmfObject>();
            Strings = new List<string>();
            Numbers = new List<double>();
            Booleans = new List<bool>();
            Nulls = 0;
        }
    }
    public class AmfReader
    {
        private Stack<AmfObject> objectStack;
        public AmfData amfData;
 
        public AmfReader()
        {
            objectStack = new Stack<AmfObject>();
            amfData = new AmfData();
        }

        public void Parse(EndianBinaryReader reader, uint size)
        {
            var maxReadPos = reader.BaseStream.Position + size;
            while(reader.BaseStream.Position < maxReadPos)
            {
                if (objectStack.Count != 0)
                {
                    var count = reader.ReadUInt16();
                    var propString = "";
                    for (var i = 0; i < count; i++)
                    {
                        propString += (char) reader.ReadByte();
                    }
                    objectStack.Peek().CurrentProperty = propString;
                }

                var type = (Amf0Types)reader.ReadByte();

                Console.WriteLine(type);
                switch (type)
                {
                    case Amf0Types.Number:
                        {
                            var value = reader.ReadDouble();
                            if(objectStack.Count != 0)
                            {
                                objectStack.Peek().Numbers.Add(objectStack.Peek().CurrentProperty, value);
                            }
                            else
                            {
                                amfData.Numbers.Add(value);
                            }
                        }
                        break;
                    case Amf0Types.Boolean:
                        {
                            var value = reader.ReadBoolean();
                            if (objectStack.Count != 0)
                            {
                                objectStack.Peek().Booleans.Add(objectStack.Peek().CurrentProperty, value);
                            }
                            else
                            {
                                amfData.Booleans.Add(value);
                            }
                        }
                        break;
                    case Amf0Types.String:
                        {
                            var count = reader.ReadUInt16();
                            var pushString = "";
                            for (var i = 0; i < count; i++)
                            {
                                pushString += (char) reader.ReadByte();
                            }

                            if (objectStack.Count != 0)
                            {
                                objectStack.Peek().Strings.Add(objectStack.Peek().CurrentProperty, pushString);
                            }
                            else
                            {
                                amfData.Strings.Add(pushString);
                            }
                        }
                        break;
                    case Amf0Types.Null:

                        if (objectStack.Count != 0)
                        {
                            objectStack.Peek().Nulls++;
                        }
                        else
                        {
                            amfData.Nulls++;
                        }
                        break;
                    case Amf0Types.Object:
                    case Amf0Types.Array:
                        {
                            if(type == Amf0Types.Array)
                            {
                                var arrayLength = reader.ReadInt32();
                            }
                            var objectAdd = new AmfObject();
                            objectStack.Push(objectAdd);
                        }
                        break;
                    case Amf0Types.ObjectEnd:
                        {
                            if(objectStack.Count == 1)
                            {
                                amfData.Objects.Add(objectStack.Pop());
                            }
                            else if(objectStack.Count > 1)
                            {
                                var mostRecentObject = objectStack.Pop();
                                objectStack.Peek().Objects.Add(objectStack.Peek().CurrentProperty, mostRecentObject);
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                /*
                switch (type)
                {
                    case Amf0Types.Array:
                    case Amf0Types.Object:
                        {
                            if(type == Amf0Types.Array)
                            {
                                var arrayLength = reader.ReadInt32();
                                Console.WriteLine("Array:" + arrayLength);
                            }
                            bool hasProperty = false;
                            string property = "";
                            var objectAdd = new AmfObject();
                            while (reader.BaseStream.Position < maxReadPos)
                            {
                                if(!hasProperty)
                                {
                                    property = "";
                                    var propertyStringLength = reader.ReadUInt16();
                                    for (var i = 0; i < propertyStringLength; i++)
                                    {
                                        property += (char)reader.ReadByte();
                                    }
                                    hasProperty = true;
                                }

                                if (hasProperty == true)
                                {
                                    if(property.Length == 0)
                                    {
                                        amfData.Objects.Add(objectAdd);

                                        break;
                                    }
                                    var objtype = (Amf0Types)reader.ReadByte();
                                    parseType(objtype, reader, ref objectAdd.Nulls, objectNumbers: objectAdd.Numbers,
                                              objectBooleans: objectAdd.Booleans, objectStrings: objectAdd.Strings,
                                              property: property);
                                    hasProperty = false;
                                }
                            }
                        }
                        break;
                    default:
                        parseType(type, reader, ref amfData.Nulls, amfData.Numbers, amfData.Booleans, amfData.Strings);
                        break;
                }*/
            }
        }

        //jesus fuck note to self: fix this
        private static void parseType(Amf0Types type, EndianBinaryReader reader,
            ref uint Nulls,
            List<double> Numbers = null,
            List<bool> Booleans = null,
            List<string> Strings = null,
            Dictionary<string, string> objectStrings = null,
            Dictionary<string, double> objectNumbers = null,
            Dictionary<string, bool> objectBooleans = null,
            string property = "")
        {
            Console.WriteLine(BitConverter.ToString(BitConverter.GetBytes((byte)type)) + ":" + type);
            switch (type)
            {
                case Amf0Types.Number:
                    if (Numbers != null)
                        Numbers.Add(reader.ReadDouble());
                    else
                        objectNumbers.Add(property, reader.ReadDouble());
                    break;
                case Amf0Types.Boolean:
                    if (Booleans != null)
                        Booleans.Add(reader.ReadBoolean());
                    else
                        objectBooleans.Add(property, reader.ReadBoolean());
                    break;
                case Amf0Types.String:
                    {
                        var count = reader.ReadUInt16();
                        var pushString = "";
                        for (var i = 0; i < count; i++)
                        {
                            pushString += (char) reader.ReadByte();
                        }
                        if (Strings != null)
                            Strings.Add(pushString);
                        else
                            objectStrings.Add(property, pushString);
                    }
                    break;
                case Amf0Types.Null:
                    Nulls++;
                    break;
                case Amf0Types.Array:
                    break;
                case Amf0Types.ObjectEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
