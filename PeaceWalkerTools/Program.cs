using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Infragistics.Documents.Excel;
using PeaceWalkerTools.Olang;

namespace PeaceWalkerTools
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var raw = File.ReadAllBytes(@"SLOT\105C6000_CB373CB9.bin");
            using (var reader = new BinaryReader(new MemoryStream(raw)))
            {
                reader.BaseStream.Position = 0x24;

                var hash0 = reader.ReadInt32();
                var hash1 = reader.ReadInt32();
                var hash2 = reader.ReadInt32();

                var hash = new Hash(hash0, hash1, hash2);

                DecryptionUtility.Decrypt(raw, 0x30, 2048, ref hash);

                File.WriteAllBytes("slot_test.bin", raw);

            }



            //using (var fs = File.OpenRead(@"D:\Projects\Sandbox\PeaceWalkerTools\PeaceWalkerTools\bin\Debug\_Extracted\291_54242D62.gcx"))
            //{
            //    var set = Briefing.ReadBriefingTitles(fs);
            //}
            //SaveSlotOlangExcel();
                ReplaceSlotOlang();
            //foreach (var olang in Directory.GetFiles("SLOT_", "*.olang"))
            //{
            //    var olf = OlangFile.Read(olang);
            //    SerializationHelper.Save(olf, olang + ".xml");
            //}

            //SimplePDT.ExtractSimplePDTs();
            //Slot.Read(@"E:\Peace Walker\PSP_GAME\USRDIR");
            Slot.Write(@"E:\Peace Walker\PSP_GAME\USRDIR");

            //foreach (var pdt in Directory.GetFiles(@"E:\Games\Emulators\PSP\memstick\PSP\SAVEDATA\NPJH50045DLC", "*.pdt"))
            //{
            //    var raw = File.ReadAllBytes(pdt);
            //    var key = raw[0];
            //    for (int i = 0; i < raw.Length; i++)
            //    {
            //        raw[i] ^= key;
            //    }

            //    File.WriteAllBytes(pdt + ".dec", raw);
            //}

            return;

            ReplaceYPK();

            ReplaceOlang();

            foreach (var dar in Directory.GetFiles("Extracted", "*.dar.inf"))
            {
                DAR.Pack(dar);
            }


            Repack();
            return;

            //OlangUtility.ReplaceText("Olang.xlsx", @".\olang");



            //foreach (var item in Directory.GetFiles(@".\Extracted", "*.dar.inf"))
            //{
            //    DAR.Pack(item);
            //}
            //Slot.Read(@"E:\Peace Walker\PSP_GAME\USRDIR");


            return;
            //Briefing.UnpackBriefing2();




            //ExtractOlang();

            //UnpackOlang();

            if (Debugger.IsAttached)
            {
                //args = Directory.GetFiles(@"D:\Projects\Sandbox\PeaceWalkerTools\PeaceWalkerTools\bin\Debug\Extracted", "*.qar");
                args = Directory.GetFiles(@"D:\Projects\Sandbox\PeaceWalkerTools\PeaceWalkerTools\bin\Debug\Extracted", "*.txp", SearchOption.AllDirectories);

                //args = new string[] { @"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted\002_E993BCED.dar.inf" };
            }


            if (args.Length > 0)
            {
                foreach (var path in args)
                {
                    if (File.Exists(path))
                    {
                        var ext = Path.GetExtension(path);

                        switch (ext)
                        {
                            case ".dar":
                            DAR.Unpack(path);
                            break;

                            case ".qar":
                            QAR.Unpack(path);
                            break;

                            case ".txp":
                            TXP.Extract(path);
                            break;

                            case ".inf":
                            Pack(path);
                            break;
                            default:
                            break;
                        }
                    }
                }
            }
        }
        private static string GetText(object cellValue)
        {
            if (cellValue == null)
            {
                return null;
            }
            if (cellValue is string)
            {
                return cellValue as string;
            }
            else if (cellValue is FormattedString)
            {
                var value = (cellValue as FormattedString).ToString();

                return value;
            }
            else if (cellValue is double)
            {
                return ((double)cellValue).ToString();
            }
            else
            {
                return null;
            }
        }
        private static void ReplaceSlotOlang()
        {
            var workbook = Workbook.Load("SlotOlang.xlsx");

            var map = workbook.Worksheets.ToDictionary(x => x.Name, x => x.Rows.Skip(1).OrderBy(y => (double)(y.Cells[0].Value)).Select(y => GetText(y.Cells[3].Value)).ToList());

            var binMap = Directory.GetFiles("SLOT", "*.bin.xml");

            foreach (var item in binMap)
            {
                var slotOlangs = SerializationHelper<SlotOlang[]>.Read(item);

                foreach (var slotOlang in slotOlangs)
                {
                    var slotFile = Path.Combine("SLOT", Path.GetFileNameWithoutExtension(item));



                    var olangFile = Path.Combine("SLOT", slotOlang.Name);
                    var olang = OlangFile.Read(olangFile);
                    var key = Path.GetFileNameWithoutExtension(slotOlang.Name);

                    List<string> texts;
                    if (map.TryGetValue(key, out texts))
                    {
                        if (texts.TrueForAll(x => x == null))
                        {
                            continue;
                        }
                        Debug.Assert(olang.TextList.Count == texts.Count);

                        for (int i = 0; i < texts.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(texts[i]))
                            {
                                olang.TextList[i].Text = texts[i];
                            }

                        }


                        byte[] rawOlang = null;
                        using (var ms = new MemoryStream())
                        {
                            olang.Write(ms);

                            rawOlang = ms.ToArray();
                        }

                        if (slotOlang.Length >= rawOlang.Length)
                        {
                            Array.Resize(ref rawOlang, slotOlang.Length);

                            using (var fs = File.OpenWrite(slotFile))
                            {
                                fs.Position = slotOlang.Offset;
                                fs.Write(rawOlang, 0, rawOlang.Length);
                            }
                        }
                        else
                        {
                            Debug.WriteLine(slotOlang.Name);
                        }
                    }
                }

            }

        }

        private static void ReplaceOlang()
        {
            OlangUtility.ReplaceText("Olang.xlsx", @"Olang\New");


            var map = Directory.GetFiles(@"Olang\New").ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => File.ReadAllBytes(x));

            foreach (var olang in Directory.GetFiles("Extracted", "*.olang", SearchOption.AllDirectories))
            {
                var key = Path.GetFileNameWithoutExtension(olang);
                if (key == "lang_stagetelop")
                { }
                byte[] data;
                if (map.TryGetValue(key, out data))
                {
                    File.WriteAllBytes(olang, data);
                }
                else
                { }
            }
        }

        private static void ReplaceYPK()
        {
            YpkUtility.ReplaceText("ypk.xlsx", @"YPK\New");


            var mapYpk = Directory.GetFiles(@"YPK\New").ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => File.ReadAllBytes(x));

            foreach (var ypk in Directory.GetFiles("Extracted", "*.ypk", SearchOption.AllDirectories))
            {
                var key = Path.GetFileNameWithoutExtension(ypk);
                byte[] data;
                if (mapYpk.TryGetValue(key, out data))
                {
                    File.WriteAllBytes(ypk, data);
                }
            }
        }

        public class YpkUtility
        {
            public static void ExportToExcel()
            {
                var workbook = new Workbook(WorkbookFormat.Excel2007);
                foreach (var path in Directory.GetFiles("ypk", "*.ypk"))
                {
                    var location = Path.GetDirectoryName(path);
                    var sheet = workbook.Worksheets.Add(Path.GetFileNameWithoutExtension(path));

                    var ypk = YPK.Read(path);

                    var index = 0;
                    var rowIndex = 1;

                    sheet.Rows[0].Cells[0].Value = "Index";
                    sheet.Rows[0].Cells[1].Value = "SyncStart";
                    sheet.Rows[0].Cells[2].Value = "SyncEnd";
                    sheet.Rows[0].Cells[3].Value = "Unknown";
                    sheet.Rows[0].Cells[4].Value = "Japanese";
                    sheet.Rows[0].Cells[5].Value = "Korean";

                    sheet.Columns[4].SetWidth(420, WorksheetColumnWidthUnit.Pixel);
                    sheet.Columns[5].SetWidth(420, WorksheetColumnWidthUnit.Pixel);

                    foreach (var entity in ypk.Entities)
                    {
                        foreach (var line in entity.Lines)
                        {
                            var row = sheet.Rows[rowIndex++];
                            row.Cells[0].Value = index;
                            row.Cells[1].Value = line.SyncStart;
                            row.Cells[2].Value = line.SyncEnd;
                            row.Cells[3].Value = line.Unknown;
                            row.Cells[4].Value = line.Text;
                        }

                        index++;
                    }
                    //var fileName = Path.GetFileName(path);
                    //var export = Path.Combine(location, "xml", string.Format("{0}.xml", fileName));
                    //SerializationHelper.Save(ypk, export);

                    //ypk = SerializationHelper<YPK>.Read(export);
                    //ypk.Write(Path.Combine(location, "New", fileName));


                }
                workbook.Save("ypk.xlsx");
            }

            public static void ReplaceText(string sourcePath, string location)
            {
                var workbook = Workbook.Load(sourcePath);

                var sheetMap = workbook.Worksheets.ToDictionary(x => x.Name, x => GetTextList(x));

                var files = Directory.GetFiles(location, "*.ypk");

                foreach (var file in files)
                {
                    var olang = YPK.Read(file);
                    var korean = sheetMap[Path.GetFileNameWithoutExtension(file)];

                    var list = olang.Entities.SelectMany(x => x.Lines).ToList();

                    if (korean.Count != list.Count)
                    {
                    }
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(korean[i]))
                        {
                            list[i].Text = korean[i];
                        }
                    }


                    olang.Write(file);
                }
            }


            private static string GetText(WorksheetRow row)
            {
                var cellValue = row.Cells[5].Value;

                return GetText(cellValue);
            }

            private static string GetText(object cellValue)
            {
                if (cellValue == null)
                {
                    return null;
                }
                if (cellValue is string)
                {
                    return cellValue as string;
                }
                else if (cellValue is FormattedString)
                {
                    return (cellValue as FormattedString).ToString();
                }
                else
                {
                    return null;
                }
            }

            private static List<string> GetTextList(Worksheet sheet)
            {
                return sheet.Rows.Skip(1).Select(x => GetText(x)).ToList();
            }

        }
        private static void Unpack()
        {
            var file = StageDataFile.Read(@"E:\Peace Walker\PSP_GAME\USRDIR\STAGEDAT.PDT");
            SerializationHelper.Save(file, "STAGEDAT.PDT.xml");
        }

        private static void Repack()
        {
            var file = SerializationHelper<StageDataFile>.Read("STAGEDAT.PDT.xml");
            file.Write();
        }

        private static void ExtractOlang()
        {
            var map = Workbook.Load(@"E:\Peace Walker\PSP_GAME\USRDIR\Olang.xlsx").Worksheets.ToDictionary(x => x.Name);
            var workbook = Workbook.Load(@"D:\Projects\Sandbox\PeaceWalkerTools\PeaceWalkerTools\bin\Debug\Olang.xlsx");
            var asciiPattern = new Regex(@"^[\u0000-\u007F]+$");

            foreach (var sheet in workbook.Worksheets)
            {
                sheet.Rows[0].Cells[0].Value = "Key";
                sheet.Rows[0].Cells[1].Value = "Japanese";
                sheet.Rows[0].Cells[2].Value = "Korean";


                if (map.ContainsKey(sheet.Name))
                {
                    var rowIndex = 1;
                    var map2 = new System.Collections.Generic.Dictionary<string, string>();

                    while (true)
                    {
                        var row = map[sheet.Name].Rows[rowIndex];

                        //var row = sheet.Rows[rowIndex];
                        if (GetString(row.Cells[1].Value) == null)
                        {
                            break;
                        }

                        var value = (GetString(row.Cells[1].Value)).Trim().Replace("\r\n", "\n");

                        if (map2.ContainsKey(value))
                        { }
                        else
                        {
                            var valueTran = GetString(row.Cells[2].Value);
                            if (valueTran != null)
                            {
                                map2[value] = valueTran.Replace("\r\n", "\n");
                            }
                        }


                        rowIndex++;
                    }


                    rowIndex = 1;
                    while (true)
                    {
                        var row = sheet.Rows[rowIndex];

                        if (row.Cells[1].Value == null)
                        {
                            break;
                        }

                        var value = (GetString(row.Cells[1].Value)).Trim().Replace("\r\n", "\n");

                        if (map2.ContainsKey(value))
                        {
                            row.Cells[2].Value = map2[value];
                        }


                        rowIndex++;
                    }
                }

                var replaceEnglishOnly = true;
                if (replaceEnglishOnly)
                {
                    var rowIndex = 1;

                    while (true)
                    {
                        var row = sheet.Rows[rowIndex];

                        if (row.Cells[0].Value == null)
                        {
                            break;
                        }
                        var value = GetString(row.Cells[1].Value);

                        if (asciiPattern.Match(value).Success)
                        {
                            row.Cells[2].Value = value;
                        }

                        rowIndex++;
                    }
                }
            }

            workbook.Save(@"D:\Projects\Sandbox\PeaceWalkerTools\PeaceWalkerTools\bin\Debug\Olang.xlsx");
            //StageDataFile.Read(@"E:\Peace Walker\PSP_GAME\USRDIR\STAGEDAT.PDT");
        }

        private static string GetString(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string)
            {
                return value as string;
            }
            else if (value is FormattedString)
            {
                return (value as FormattedString).UnformattedString;
            }

            else
            {
                throw new NotImplementedException();
            }
        }

        private static void Pack(string path)
        {
            var ext = Path.GetExtension(Path.GetFileNameWithoutExtension(path));

            switch (ext)
            {
                case ".dar":
                DAR.Pack(path);
                break;
                default:
                break;
            }
        }
    }
}
