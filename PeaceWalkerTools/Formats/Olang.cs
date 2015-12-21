using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
    class Olang
    {


        //public static void UnpackOlang()
        //{
        //    var location = @"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted";
        //    location = ".";
        //    var workbook = new Workbook(WorkbookFormat.Excel2007);

        //    var offsets = new List<int>();


        //    foreach (var file in Directory.GetFiles(location, "*.olang", SearchOption.AllDirectories))
        //    {
        //        var key = Path.GetFileNameWithoutExtension(file);
        //        var items = Unpack(file);

        //        continue;

        //        var sheet = workbook.Worksheets.Add(key);

        //        sheet.Columns[1].SetWidth(600, WorksheetColumnWidthUnit.Pixel);
        //        sheet.Columns[1].CellFormat.WrapText = ExcelDefaultableBoolean.True;

        //        var rowIndex = 1;
        //        foreach (var item in items)
        //        {
        //            var row = sheet.Rows[rowIndex++];

        //            row.Cells[1].Value = item;
        //        }

        //    }


        //    //workbook.Save(Path.Combine(location, "olang_.xlsx"));


        //}

        static Dictionary<string, List<string>> _olangs = new Dictionary<string, List<string>>();


        static List<byte> _stringBuffer = new List<byte>();

        public static void DumpLang()
        {
            //var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";

            foreach (var path in Directory.GetFiles(".", "*."))
            {
                DumpLang(path);
            }

        }



        public int Magic { get; set; }
        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }

        public int ReferenceOffset { get; set; }
        public int Unknown4 { get; set; }
        public int HeaderOffset { get; set; }
        public int BodyOffset { get; set; }


        public List<OlangEntity> Entities { get; private set; } = new List<OlangEntity>();
        public List<OlangSubEntity> Headers { get; private set; } = new List<OlangSubEntity>();
        public Dictionary<int, string> TextMap { get; private set; }

        public static Olang Unpack(string path)
        {
            return new Olang(path);
        }

        public Olang(string path)
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

                var unknown5 = reader.ReadInt32();
                var entityCount = reader.ReadInt16();
                var unknown6 = reader.ReadInt16();


                for (int i = 0; i < referenceCount; i++)
                {
                    var entity = new OlangEntity();
                    Entities.Add(entity);
                    entity.Key = reader.ReadInt32();
                    entity.ReferenceIndex = reader.ReadInt16();
                    entity.Unknown1 = reader.ReadInt16();
                }

                reader.BaseStream.Position = HeaderOffset;
                for (int i = 0; i < headerCount; i++)
                {
                    var entity = new OlangSubEntity();
                    Headers.Add(entity);

                    entity.Unknown0 = reader.ReadInt32();
                    entity.Offset = reader.ReadInt32();
                    entity.Unknown1 = reader.ReadInt32();


                    if (entity.Unknown0 != 0xdb0)
                    {
                        //Debugger.Break();
                    }
                    if (entity.Unknown1 != 1)
                    {
                        //Debugger.Break();
                    }
                }


                TextMap = Headers.Select(x => x.Offset).Distinct().ToDictionary(x => x, x => (string)null);

                foreach (var offset in TextMap.Keys.ToList())
                {
                    var position = BodyOffset + offset;

                    if (position < reader.BaseStream.Length)
                    {
                        reader.BaseStream.Position = position;
                        var text = reader.BaseStream.ReadString();
                        TextMap[offset] = text;
                        //Debug.WriteLine(text);
                    }
                }

                for (int i = 0; i < Headers.Count; i++)
                {
                    var text = TextMap[Headers[i].Offset];
                    Headers[i].Text = text;
                }
            }
        }


        //private static IEnumerable<string> UnpackOlang(string path)
        //{

        //    var raw = File.ReadAllBytes(path);

        //    var header1Offset = BitConverter.ToInt32(raw, 16);
        //    var header2Offset = BitConverter.ToInt32(raw, 20);
        //    var header3Offset = BitConverter.ToInt32(raw, 24);
        //    var bodyOffset = BitConverter.ToInt32(raw, 28);

        //    var header1Count = (header3Offset - header1Offset) / 8;


        //    var entitiyCount = (bodyOffset - header3Offset) / 12;

        //    var buffer = new List<byte>();


        //    var entities = new List<OlangEntity>();

        //    for (int i = 0; i < entitiyCount; i++)
        //    {
        //        var entity = new OlangEntity();
        //        entities.Add(entity);
        //        entity.Unknown0 = BitConverter.ToInt32(raw, header1Offset + i * 8);
        //        entity.Unknown1 = BitConverter.ToInt32(raw, header1Offset + i * 8 + 4);

        //        entity.Unknown2 = BitConverter.ToInt32(raw, header2Offset + i * 8);
        //        entity.Unknown3 = BitConverter.ToInt32(raw, header2Offset + i * 8 + 4);

        //        entity.Unknown4 = BitConverter.ToInt32(raw, header3Offset + i * 12);
        //        entity.Offset = BitConverter.ToInt32(raw, header3Offset + i * 12 + 4);
        //        entity.Unknown6 = BitConverter.ToInt32(raw, header3Offset + i * 12 + 8);

        //        entity.Text = raw.GetString(bodyOffset + entity.Offset);

        //    }



        //    var section = new List<string>();
        //    var offset = bodyOffset;

        //    var isLastNull = true;

        //    for (int i = bodyOffset; i < raw.Length; i++)
        //    {

        //        if (raw[i] == 0)
        //        {
        //            var length = i - offset;

        //            if (length > 0 && !isLastNull)
        //            {
        //                section.Add(Encoding.UTF8.GetString(buffer.ToArray()));

        //                buffer.Clear();
        //            }

        //            offset = i;
        //            isLastNull = true;
        //        }
        //        else
        //        {
        //            buffer.Add(raw[i]);
        //            isLastNull = false;
        //        }

        //    }

        //    if (buffer.Count > 0)
        //    {
        //        section.Add(Encoding.UTF8.GetString(buffer.ToArray()));

        //        buffer.Clear();
        //    }

        //    return section;
        //}




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

    class OlangEntity
    {
        public int Key { get; set; }
        public short ReferenceIndex { get; set; }
        public short Unknown1 { get; set; }


        public override string ToString()
        {
            return string.Format("{0:X8} {1:X4} #{2,-4}", Key, Unknown1, ReferenceIndex);
        }
    }

    class OlangSubEntity
    {
        public int Unknown0 { get; set; }
        public int Offset { get; set; }
        public int Unknown1 { get; set; }

        public override string ToString()
        {
            return string.Format("{0:X8} {1:X8} @{2,-8} {3}", Unknown0, Unknown1, Offset, Text);
        }

        public string Text { get; set; }
    }


}
