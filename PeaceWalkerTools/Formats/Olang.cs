using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Infragistics.Documents.Excel;

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
        public int HeaderOffset { get; set; }
        [XmlAttribute]
        public int Unknown4 { get; set; }
        [XmlAttribute]
        public int ReferenceOffset { get; set; }
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
        public static OlangFile Read(string path)
        {
            var file = new OlangFile();
            file.ReadInternal(File.OpenRead(path));

            return file;
        }

        public static OlangFile Read(Stream stream)
        {
            var file = new OlangFile();
            file.ReadInternal(stream);

            return file;
        }
        private void ReadInternal(Stream stream)
        {

            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                Magic = reader.ReadInt32(); // RBX
                Unknown1 = reader.ReadInt32();
                Unknown2 = reader.ReadInt32();
                Unknown3 = reader.ReadInt32();

                HeaderOffset = reader.ReadInt32();
                Unknown4 = reader.ReadInt32();
                ReferenceOffset = reader.ReadInt32();
                BodyOffset = reader.ReadInt32();

                var entityCount = (ReferenceOffset - HeaderOffset - 8) / 8;

                var referenceCount = (BodyOffset - ReferenceOffset) / 12;


                reader.BaseStream.Position = HeaderOffset;
                if (reader.BaseStream.Length <= HeaderOffset)
                {
                    TextList = new List<OlangText>();
                    return;
                }
                Unknown5 = reader.ReadInt32();
                EntityCount = reader.ReadInt16();
                Unknown6 = reader.ReadInt16();


                for (int i = 0; i < entityCount; i++)
                {
                    var entity = new Entity();
                    Entities.Add(entity);

                    entity.Key = reader.ReadInt32();
                    entity.ReferenceIndex = reader.ReadInt16();
                    entity.Unknown1 = reader.ReadInt16();
                }

                reader.BaseStream.Position = ReferenceOffset;

                for (int i = 0; i < referenceCount; i++)
                {
                    var reference = new Reference();
                    References.Add(reference);

                    reference.Unknown0 = reader.ReadInt32();
                    reference.Offset = reader.ReadInt32();
                    reference.Flag = reader.ReadInt32();
                }


                var map = References.Select(x => x.Offset).Distinct().ToDictionary(x => x, x => (string)null);

                foreach (var offset in map.Keys.ToList())
                {
                    var position = BodyOffset + offset;

                    if (position < reader.BaseStream.Length)
                    {
                        reader.BaseStream.Position = position;

                        var text = reader.BaseStream.ReadString();

                        map[offset] = text;
                    }
                }

                var textSet = map.OrderBy(x => x.Key).ToList();

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
                    var text = map[References[i].Offset];

                    References[i].Text = text;
                }
            }
        }

        public void Write(string path)
        {
            var stream = File.Create(path);

            Write(stream);
        }

        public void Write(Stream stream)
        {
            var indexToOffset = new Dictionary<int, int>();

            byte[] rawText = null;

            var index = 0;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                foreach (var item in TextList)
                {
                    indexToOffset[index++] = (int)writer.BaseStream.Position;
                    writer.Write(Encoding.UTF8.GetBytes(item.Text));
                    writer.Write((byte)0);
                    if (writer.BaseStream.Position % 2 == 1)
                    {
                        writer.Write((byte)0);
                    }
                }

                rawText = ms.ToArray();
            }

            //var fileName = Path.GetFileName(path);
            //var location = Path.GetDirectoryName(path);
            //path = Path.Combine(location, "New", fileName);

            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); // RBX
                writer.Write(Unknown1);
                writer.Write(Unknown2);
                writer.Write(Unknown3);

                writer.Write(HeaderOffset);
                writer.Write(Unknown4);
                writer.Write(ReferenceOffset);
                writer.Write(BodyOffset);

                var entityCount = (ReferenceOffset - HeaderOffset - 8) / 8;

                var referenceCount = (BodyOffset - ReferenceOffset) / 12;


                writer.BaseStream.Position = HeaderOffset;

                writer.Write(Unknown5);
                writer.Write(EntityCount);
                writer.Write(Unknown6);


                for (int i = 0; i < entityCount; i++)
                {
                    var entity = Entities[i];

                    writer.Write(entity.Key);
                    writer.Write(entity.ReferenceIndex);
                    writer.Write(entity.Unknown1);
                }

                writer.BaseStream.Position = ReferenceOffset;

                for (int i = 0; i < referenceCount; i++)
                {
                    var reference = References[i];
                    reference.Offset = indexToOffset[reference.OffsetIndex];

                    writer.Write(reference.Unknown0);
                    writer.Write(reference.Offset);
                    writer.Write(reference.Flag);
                }

                writer.BaseStream.Position = BodyOffset;

                writer.Write(rawText);
            }
        }

        public class Entity
        {
            [XmlAttribute("Key")]
            public string KeyHex
            {
                get { return Key.ToString("X8"); }
                set { Key = int.Parse(value, NumberStyles.HexNumber); }
            }
            [XmlIgnore]
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
            [XmlAttribute("Flag")]
            public string FlagHex
            {
                get { return Flag.ToString("X8"); }
                set { Flag = int.Parse(value, NumberStyles.HexNumber); }
            }
            [XmlIgnore]
            public int Flag { get; set; }

            public override string ToString()
            {
                return string.Format("?:{0:X8} ?:{1:X8} @{2,-8} - {3}", Unknown0, Flag, Offset, Text);
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

    public static class OlangUtility
    {
        public static void ReplaceText(string sourcePath, string location)
        {
            var workbook = Workbook.Load(sourcePath);
            var sheetMap = workbook.Worksheets.ToDictionary(
                x => x.Name,
                x => x.Rows.Skip(1).OrderBy(y => (double)(y.Cells[0].Value)).Select(y => y.Cells[3].GetText()?.Replace("\r\n", "\n")).ToList());


            var files = Directory.GetFiles(location, "*.olang");

            foreach (var file in files)
            {
                var olang = OlangFile.Read(file);
                var key = Path.GetFileNameWithoutExtension(file);


                var korean = sheetMap[key];

                for (var i = 0; i < olang.TextList.Count; i++)
                {
                    olang.TextList[i].Text = korean[i];
                }

                olang.Write(file);
            }
        }

        private static string GetText(WorksheetRow row)
        {
            var cellValue = row.Cells[2].Value;
            if (cellValue == null)
            {
                return GetText(row.Cells[1].Value);
            }
            return GetText(cellValue);
        }

        private static string GetText(object cellValue)
        {
            if (cellValue is string)
            {
                return cellValue as string;
            }
            else if (cellValue is FormattedString)
            {
                var text = (cellValue as FormattedString).UnformattedString;

                return text;
            }
            else
            {
                return cellValue.ToString();
            }
        }

        public static void DumpLang(string path)
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


}