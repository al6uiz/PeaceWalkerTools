﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
    class Briefing
    {
        public static void UnpackBriefing2()
        {
            var location = @"E:\Peace Walker\PSP_GAME\USRDIR";
            var fileName = "BRIEFING.DAT";

            Unpack(location, fileName);
        }

        private static void Unpack(string location, string fileName)
        {
            var magic = new byte[] { 0x06, 0x3B, 0x5D, 0x4B };

            var raw = File.ReadAllBytes(Path.Combine(location, fileName));

            var buffer = new List<byte>();

            var section = new List<byte[]>();
            var offset = 0;

            for (int i = 0; i < raw.Length; i++)
            {
                if (raw.Find(magic, i))
                {
                    var length = i - offset;

                    if (length > 0)
                    {
                        section.Add(buffer.ToArray());

                        buffer.Clear();
                    }

                    offset = i;
                }

                buffer.Add(raw[i]);

            }

            if (buffer.Count > 0)
            {
                section.Add(buffer.ToArray());

                buffer.Clear();
            }



            //var workbook = new Workbook(WorkbookFormat.Excel2007);
            //var sheet = workbook.Worksheets.Add("Sheet");
            //sheet.Columns[1].SetWidth(240, WorksheetColumnWidthUnit.Pixel);
            //sheet.Columns[1].CellFormat.WrapText = ExcelDefaultableBoolean.True;

            var workbook = Workbook.Load(Path.Combine(location, "Briefing.xlsx"));
            var sheet = workbook.Worksheets.First();

            var setIndex = 0;
            var rowIndex = 1;

            foreach (var item in section)
            {
                var set = ReadBriefing(new MemoryStream(item));

                foreach (var entity in set.Entities)
                {

                    var row = sheet.Rows[rowIndex++];

                    row.Cells[0].Value = setIndex;
                    row.Cells[1].Value = entity.Text.Trim();
                }

                setIndex++;
            }

            workbook.Save(Path.Combine(location, "Briefing.xlsx"));
        }


        public static void Repack(string location, string sourcePath)
        {
            var fileNameDAT = "BRIEFING.DAT";
            Dictionary<int, List<string>> koreanSet = ReadKorean(sourcePath);

            var sets = new List<BriefingSet>();

            var data = File.ReadAllBytes(Path.Combine(location, fileNameDAT + ".Original"));

            using (var fs = new MemoryStream(data))
            {
                while (fs.Position < fs.Length)
                {
                    var magic = new byte[4];
                    fs.Read(magic, 0, 4);
                    fs.Position -= 4;
                    if (BitConverter.ToInt32(magic, 0) != 1264401158)
                    {

                    }

                    sets.Add(ReadBriefing(fs));

                    var next = (long)(Math.Ceiling((fs.Position) / 16.0) * 16);

                    fs.Position = next;

                    if (fs.Position >= fs.Length)
                    {
                        break;
                    }

                }

                var index = 0;
                foreach (var set in sets)
                {
                    var sizeKor = 0;

                    var setIndex = index++;

                    List<string> korean;

                    if (!koreanSet.TryGetValue(setIndex, out korean))
                    {
                        continue;
                    }

                    foreach (var e in korean)
                    {
                        sizeKor += Encoding.UTF8.GetByteCount(e) + 1;
                    }

                    if (set.TextSectionLength < sizeKor)
                    {
                        Debug.WriteLine(string.Format("Skip {0} : +{1}", setIndex, sizeKor - set.TextSectionLength));

                        continue;
                    }

                    fs.Position = set.TextSectionStart;

                    for (int i = 0; i < set.Entities.Count; i++)
                    {
                        set.Entities[i].Offset = (int)(fs.Position - set.TextSectionStart);
                        var raw = Encoding.UTF8.GetBytes(korean[i]);
                        fs.Write(raw, 0, raw.Length);
                        fs.WriteByte(0);
                    }



                    var fill = set.TextSectionLength - (fs.Position - set.TextSectionStart);

                    for (int i = 0; i < fill; i++)
                    {
                        fs.WriteByte(0);
                    }

                    fs.Position = set.HeaderStart;
                    for (int i = 0; i < set.Entities.Count; i++)
                    {
                        fs.Write(BitConverter.GetBytes(set.Entities[i].Offset), 0, 4);
                    }

                }
            }


            File.WriteAllBytes(Path.Combine(location, fileNameDAT), data);
        }

        private static Dictionary<int, List<string>> ReadKorean(string path)
        {
            var koreanSet = new Dictionary<int, List<string>>();

            var rowIndex = 1;

            var sheet = Workbook.Load(path).Worksheets.FirstOrDefault();

            while (true)
            {
                var row = sheet.Rows[rowIndex++];
                if (row.Cells[0].Value == null)
                {
                    break;
                }

                List<string> list;
                var setIndex = (int)(double)row.Cells[0].Value;
                if (!koreanSet.TryGetValue(setIndex, out list))
                {
                    list = new List<string>();
                    koreanSet[setIndex] = list;
                }

                var cell = row.Cells[2];
                var value = cell.Value;

                list.Add(cell.GetText()?.ReplaceWideCharacters());
            }

            return koreanSet;
        }

        public static BriefingSet ReadBriefing(Stream fs)
        {
            var set = new BriefingSet();

            set.Offset = (int)fs.Position;

            fs.Position = set.Offset + 8;

            var length = fs.ReadInt32(); // 전체 길이는 start + 8 + 4 + length

            var unknown1 = fs.ReadInt32(); // 0x00000014
            var bodyOffset = fs.ReadInt32(); // 자막 시작이 start + 8 + bofyOffset
            var unknown3 = fs.ReadInt32(); // length - 4
            var unknown4 = fs.ReadInt32(); // 0x00000000

            set.HeaderStart = (int)fs.Position;
            set.TextSectionStart = set.Offset + 8 + bodyOffset;

            while (fs.Position < set.TextSectionStart)
            {
                set.Entities.Add(new BriefingEntry
                {
                    Offset = fs.ReadInt32()
                });
            }

            for (int i = 0; i < set.Entities.Count; i++)
            {
                set.Entities[i].Text = fs.ReadString(set.Entities[i].Offset + set.TextSectionStart);
            }



            if (set.Entities.Count > 0)
            {
                var last = set.Entities.Last();

                set.TextSectionLength = last.Offset + Encoding.UTF8.GetByteCount(last.Text) + 1;
            }
            fs.Position = set.TextSectionStart + set.TextSectionLength;
            fs.Offset(4);
            while (fs.ReadInt32() == 0)
            { }
            var max = (fs.Position - 4) - set.TextSectionStart;
            if (max < set.TextSectionLength)
            {

            }
            set.TextSectionLength =(int) max;

            fs.Position = set.Offset + 8 + 4 + length;

            var infoLength = fs.ReadInt32();

            fs.Position = fs.Position + infoLength;

            return set;
        }

        public static BriefingTitleSet ReadBriefingTitles(Stream fs)
        {
            fs.Position = 16;
            var first = new List<int>();
            while (true)
            {
                var re = fs.ReadInt32();

                if (re == -1)
                { break; }
                else
                {
                    first.Add(re);
                }
            }
            var set = new BriefingTitleSet();

            set.Offset = (int)fs.Position;

            //fs.Position = set.Offset + 8;

            var length = fs.ReadInt32(); // 전체 길이는 start + 8 + 4 + length

            var unknown1 = fs.ReadInt32(); // 0x00000014
            var bodyOffset = fs.ReadInt32(); // 자막 시작이 start + 8 + bofyOffset
            var unknown3 = fs.ReadInt32(); // length - 4
            var unknown4 = fs.ReadInt32(); // 0x00000000

            set.HeaderStart = (int)fs.Position;
            set.TextSectionStart = set.Offset + bodyOffset;

            while (fs.Position < set.TextSectionStart)
            {
                set.Entities.Add(new BriefingTitleEntry
                {
                    Offset = fs.ReadInt32()
                });
            }

            var encoding = Encoding.GetEncoding(20932);

            for (int i = 0; i < set.Entities.Count; i++)
            {
                var offset = set.Entities[i].Offset + set.TextSectionStart;
                fs.Position = offset;

                set.Entities[i].Key = fs.ReadInt32();
                switch (set.Entities[i].Key & 0xFF)
                {
                    case 0x01:
                    offset += 5;
                    break;
                    case 0x02:
                    offset += 4;
                    break;
                    default:
                    offset += 3;
                    break;

                }

                set.Entities[i].Text = fs.ReadString(offset, encoding);
            }

            if (set.Entities.Count > 0)
            {
                var last = set.Entities.Last();

                set.TextSectionLength = last.Offset + encoding.GetByteCount(last.Text) + 1;
            }

            fs.Position = set.Offset + 8 + 4 + length;

            var infoLength = fs.ReadInt32();

            fs.Position = fs.Position + infoLength;

            return set;
        }
    }




    class BriefingTitleSet
    {
        public int Offset { get; set; }

        public int TextSectionStart { get; set; }
        public int TextSectionLength { get; set; }

        public string Name { get; set; }

        public BriefingTitleSet()
        {
            Entities = new List<BriefingTitleEntry>();
        }
        public List<BriefingTitleEntry> Entities { get; set; }


        public int HeaderStart { get; set; }
    }

    class BriefingSet
    {
        public int Offset { get; set; }

        public int TextSectionStart { get; set; }
        public int TextSectionLength { get; set; }

        public string Name { get; set; }

        public BriefingSet()
        {
            Entities = new List<BriefingEntry>();
        }
        public List<BriefingEntry> Entities { get; set; }


        public int HeaderStart { get; set; }
    }

    class BriefingTitleEntry
    {
        public override string ToString()
        {
            return string.Format("@{0} - [{1:X4}] {2}", Offset, Key, Text);
        }

        public int Offset { get; set; }
        public int Key { get; set; }
        public string Text { get; set; }
    }
    class BriefingEntry
    {
        public override string ToString()
        {
            return string.Format("@{0} - {1}", Offset, Text);
        }

        public int Offset { get; set; }
        public string Text { get; set; }
    }
}
