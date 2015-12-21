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
    class Briefing
    {


        private static void UnpackBriefing2()
        {
            var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";
            var fileName = "BRIEFING.DAT";

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



            var workbook = new Workbook(WorkbookFormat.Excel2007);
            var sheet = workbook.Worksheets.Add("Sheet");
            sheet.Columns[1].SetWidth(240, WorksheetColumnWidthUnit.Pixel);
            sheet.Columns[1].CellFormat.WrapText = ExcelDefaultableBoolean.True;

            var setIndex = 0;
            var rowIndex = 1;
            foreach (var item in section)
            {
                var set = ReadBriefing(new MemoryStream(item));

                foreach (var entity in set.Entities)
                {

                    var row = sheet.Rows[rowIndex++];

                    row.Cells[0].Value = setIndex;
                    row.Cells[1].Value = entity.Text.Replace("\n", string.Empty).Trim();
                }

                setIndex++;
            }


            workbook.Save("briefing.xlsx");

        }



        private static void UnpackBriefing()
        {
            var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";
            var fileNameDAT = "BRIEFING.DAT";
            var fileNameExcel = "BRIEFING.xlsx";

            var koreanSet = new Dictionary<int, List<string>>();

            var rowIndex = 1;

            var sheet = Workbook.Load(Path.Combine(location, fileNameExcel)).Worksheets.FirstOrDefault();

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

                if (value is string)
                {
                    list.Add(value as string);
                }
                else
                {
                    list.Add(cell.GetText());
                }
            }


            var sets = new List<BriefingSet>();

            var data = File.ReadAllBytes(Path.Combine(location, fileNameDAT + ".original"));
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
                        Debug.WriteLine(string.Format("Skip {0}", setIndex));
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

            var output = @"E:\Games\Emulators\PSP\memstick\PSP\SAVEDATA\NPJH50045DAT\BRIEFING.DAT";
            File.WriteAllBytes(output, data);

        }

        private static BriefingSet ReadBriefing(Stream fs)
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

            fs.Position = set.Offset + 8 + 4 + length;

            var infoLength = fs.ReadInt32();

            fs.Position = fs.Position + infoLength;

            return set;
        }


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
    
    class BriefingEntry
    {
        public int Offset { get; set; }
        public string Text { get; set; }
    }
}
