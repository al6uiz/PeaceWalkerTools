using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PeaceWalkerTools.Olang
{
    public class OlangFile
    {
        [XmlAttribute]
        public int Magic { get; set; }
        [XmlAttribute]
        public int Unknown1 { get; set; }
        [XmlAttribute]
        public int Unknown2 { get; set; }
        [XmlAttribute]
        public int Unknown3 { get; set; }

        [XmlAttribute]
        public int ReferenceOffset { get; set; }
        [XmlAttribute]
        public int Unknown4 { get; set; }
        [XmlAttribute]
        public int HeaderOffset { get; set; }
        [XmlAttribute]
        public int BodyOffset { get; set; }

        [XmlAttribute]
        public int Unknown5 { get; set; }
        [XmlAttribute]
        public short EntityCount { get; set; }
        [XmlAttribute]
        public short Unknown6 { get; set; }


        public List<Entity> Entities { get; set; } = new List<Entity>();
        public List<Reference> References { get; set; } = new List<Reference>();

        [XmlIgnore]
        public Dictionary<int, string> TextMap { get; set; }
        public List<OlangText> TextList { get; set; }
        public static OlangFile Unpack(string path)
        {
            var file = new OlangFile();
            file.Read(path);

            return file;
        }

        private void Read(string path)
        {

            using (var reader = new BinaryReader(File.OpenRead(path), Encoding.UTF8))
            {
                Magic = reader.ReadInt32(); // RBX
                Unknown1 = reader.ReadInt32();
                Unknown2 = reader.ReadInt32();
                Unknown3 = reader.ReadInt32();

                ReferenceOffset = reader.ReadInt32();
                Unknown4 = reader.ReadInt32();
                HeaderOffset = reader.ReadInt32();
                BodyOffset = reader.ReadInt32();

                var referenceCount = (HeaderOffset - ReferenceOffset - 8) / 8;

                var headerCount = (BodyOffset - HeaderOffset) / 12;


                reader.BaseStream.Position = ReferenceOffset;

                Unknown5 = reader.ReadInt32();
                EntityCount = reader.ReadInt16();
                Unknown6 = reader.ReadInt16();


                for (int i = 0; i < referenceCount; i++)
                {
                    var entity = new Entity();
                    Entities.Add(entity);
                    entity.Key = reader.ReadInt32();


                    entity.ReferenceIndex = reader.ReadInt16();
                    entity.Unknown1 = reader.ReadInt16();
                }

                reader.BaseStream.Position = HeaderOffset;

                for (int i = 0; i < headerCount; i++)
                {
                    var entity = new Reference();
                    References.Add(entity);

                    entity.Unknown0 = reader.ReadInt32();
                    entity.Offset = reader.ReadInt32();
                    entity.Unknown1 = reader.ReadInt32();
                }


                var TextMap = References.Select(x => x.Offset).Distinct().ToDictionary(x => x, x => (string)null);

                foreach (var offset in TextMap.Keys.ToList())
                {
                    var position = BodyOffset + offset;

                    if (position < reader.BaseStream.Length)
                    {
                        reader.BaseStream.Position = position;

                        var text = reader.BaseStream.ReadString();

                        TextMap[offset] = text;
                    }
                }
                var textSet = TextMap.OrderBy(x => x.Key).ToList();

                TextList = textSet.Select(x => new OlangText { Text = x.Value }).ToList();


                var offsetMap = new Dictionary<int, int>();
                for (int i = 0; i < textSet.Count; i++)
                {
                    offsetMap[textSet[i].Key] = i;
                }

                for (int i = 0; i < References.Count; i++)
                {
                    References[i].OffsetIndex = offsetMap[References[i].Offset];
                }


                for (int i = 0; i < References.Count; i++)
                {
                    var text = TextMap[References[i].Offset];

                    References[i].Text = text;
                }
            }
        }

        internal static OlangFile Pack(string path)
        {


            return null;
        }
    }


    public class Temp
    {
        private static void DumpLang(string path)
        {
            var raw = File.ReadAllBytes(path);

            var dic = new Dictionary<string, byte[]>();

            string lastFileName = null;

            var extBuffer = new StringBuilder();

            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == (byte)'.')
                {
                    var j = i + 1;
                    extBuffer.Clear();
                    while (raw[j] >= 'a' && raw[j] <= 'z' || raw[j] >= '0' && raw[j] <= '9')
                    {
                        extBuffer.Append((char)raw[j++]);
                    }
                    var ext = extBuffer.ToString();
                    if (ext.Length > 2)
                    {
                        var start = i;
                        var end = i;

                        while (true)
                        {
                            if (start <= 1)
                            {
                                break;
                            }

                            if (raw[--start - 1] == 0)
                            { break; }

                            if (start <= 1)
                            {
                                break;
                            }
                        }
                        while (true)
                        {
                            if (raw[++end] == 0)
                            { break; }
                            if (end == raw.Length - 1)
                            { break; }
                        }
                        if (start >= 0)
                        {
                            var fileName = Encoding.ASCII.GetString(raw, start, end - start);
                            Debug.WriteLine(fileName);
                        }
                    }

                    if (raw.Find(".olang", i) || raw.Find(".ypk", i) /*|| Find(".la3", raw, i) || Find(".mdp", raw, i) || Find(".mtar", raw, i) || Find(".eqp", raw, i) || Find(".eft", raw, i) || Find(".sep", raw, i) || Find(".vlm", raw, i) || Find(".mtsq", raw, i) || Find(".ohd", raw, i) || Find(".mmd", raw, i) || Find(".lt2", raw, i)*//*|| Find(".txp", raw, i)*/)
                    {

                        var start = i;
                        var end = i;
                        while (true)
                        {
                            if (raw[--start - 1] == 0)
                            { break; }
                        }
                        while (true)
                        {
                            if (raw[++end] == 0)
                            { break; }
                        }

                        lastFileName = Encoding.ASCII.GetString(raw, start, end - start);
                        i = end;
                        while (raw[i] == 0)
                        {
                            i++;
                            if (i >= raw.Length)
                            {
                                break;
                            }
                        }

                        var length = BitConverter.ToInt32(raw, i);
                        i += 4;

                        while (raw[i] == 0)
                        {
                            i++;
                            if (i >= raw.Length)
                            {
                                break;
                            }
                        }


                        var magic = Encoding.ASCII.GetString(raw, i, 3);
                        var isRBX = magic == "RBX";
                        if (isRBX == false)
                        {

                        }
                        var section = new byte[length];

                        Buffer.BlockCopy(raw, i, section, 0, length);

                        dic[lastFileName] = section;

                        i += length;

                    }


                }
            }

            var location = Path.GetDirectoryName(path);

            foreach (var item in dic)
            {
                var output = Path.Combine(location, item.Key);

                File.WriteAllBytes(output, item.Value);
            }
        }
    }

    public class Entity
    {
        [XmlAttribute]
        public int Key { get; set; }
        [XmlAttribute]
        public short ReferenceIndex { get; set; }
        [XmlAttribute]
        public short Unknown1 { get; set; }


        public override string ToString()
        {
            return string.Format("Key: {0:X6} {1:X4} #{2,-4}", Key, Unknown1, ReferenceIndex);
        }
    }

    public class Reference
    {
        [XmlAttribute]
        public int Unknown0 { get; set; }
        [XmlIgnore]
        public int Offset { get; set; }

        [XmlAttribute]
        public int OffsetIndex { get; set; }
        [XmlAttribute]
        public int Unknown1 { get; set; }

        public override string ToString()
        {
            return string.Format("?:{0:X8} ?:{1:X8} @{2,-8} - {3}", Unknown0, Unknown1, Offset, Text);
        }

        [XmlIgnore]
        public string Text { get; set; }
    }

    public class OlangText
    {
        [XmlAttribute]
        public string Text { get; set; }
    }


}
