using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
    partial class Program
    {
        private void Misc()
        {


            //UnpackOlang();


            //StageDataFile.Read(@"E:\Games\Metal Gear Solid\PW\Metal_Gear_Solid_Peace_Walker_USA\PSP_GAME\USRDIR\STAGEDAT.PDT");

            ////StageDataFile.Read(@"E:\Peace Walker\PSP_GAME\USRDIR\STAGEDAT.PDT");

            foreach (var file in Directory.GetFiles(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted", "*.dar"))
            {
                DAR.Unapck(file);
            }

            ////QAR.Extract(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted\264_51AC44B4.qar");
            //foreach (var file in Directory.GetFiles(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted", "*.qar"))
            //{
            //    QAR.Extract(file);
            //}

            //foreach (var file in Directory.GetFiles(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted", "*.txp", SearchOption.AllDirectories))
            //{
            //    TXP.Extract(file);
            //}
            //UnpackOlang();


            //MergeOlang();

            //TXP.Extract(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted\001_3B69F53C_qar\owl_item_def.txp");
            //TXP.Extract(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted\216_F1C6B4F2_qar\w05s06r053_bwin_a0.txp");

            //Replace();
            //return;



            //var offset0 = 0; //3F800000
            //var offset1 = 28;//5
            //var offset2 = 1534176;//1


            //var path = @"D:\Downloads\BaiduYunDownload\PWHD\disc0_rel\STAGEDAT.PDT";
            //var path = @"E:\Peace Walker\PSP_GAME\USRDIR\stagedat.pdt";
            //StageDataFile.Read(path);
            //StageDataFile.Decrypt(path);

            //var key4 = raw[3];

            //for (int i = 0; i < raw.Length; i+=4)
            //{
            //    raw[i] = (byte)(raw[i] ^ key1);
            //    raw[i+1] = (byte)(raw[i+1] ^ key2);
            //    raw[i+2] = (byte)(raw[i+2] ^ key3);
            //    raw[i+3] = (byte)(raw[i+3] ^ key4);
            //}

            //File.WriteAllBytes(@"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR\stagedat.pdt.dec", raw);


            //foreach (var file in Directory.GetFiles(@"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR\dlc"))
            //{
            //    var raw = File.ReadAllBytes(file);
            //    var key = raw[0];
            //    for (int i = 0; i < raw.Length; i++)
            //    {
            //        raw[i] = (byte)(raw[i] ^ key);    

            //    }
            //    File.WriteAllBytes(file + ".DEC", raw);
            //}








            //UnpackItem.Unpack();

            //ReplaceSpecial();


            //var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";

            //var fileName = "STAGEDAT.PDT";

            //var raw = File.ReadAllBytes(Path.Combine(location,fileName));
            //var raw2  = new byte[raw.Length-4];

            //Buffer.BlockCopy(raw,4,raw2,0,raw2.Length);


            //ZlibStream.UncompressBuffer(raw2);

            //var font = new PGF();
            //var data = File.ReadAllBytes(@"E:\Games\Emulators\PSP\flash0\font\jpn0.pgf");
            //font.ReadPtr(data, data.Length);



            //UnpackBriefing();
            //return;
            //UnpackBriefing2();

            //Olang.DumpLang();


        }
        private static void MergeOlang()
        {

            var jpn = @"extracted\olang_jp.xlsx";
            var eng = @"extracted\olang_.xlsx";

            var bookJpn = Workbook.Load(jpn);
            var bookEng = Workbook.Load(eng);

            var mapJpn = bookJpn.Worksheets.ToDictionary(x => x.Name);
            var mapEng = bookEng.Worksheets.ToDictionary(x => x.Name);

            foreach (var item in mapEng)
            {
                if (!item.Key.EndsWith("_en"))
                {
                    bookEng.Worksheets.Remove(item.Value);
                };
            }

            bookEng.Save("olang_en.xlsx");

            foreach (var item in mapJpn)
            {
                if (mapEng.ContainsKey(item.Key + "_en"))
                {
                    var count = item.Value.Rows.Count();
                    var countEng = mapEng[item.Key + "_en"].Rows.Count();

                    if (countEng != count)
                    {
                        Debug.WriteLine(item.Key);

                    }

                }
                else
                {
                    //Debug.WriteLine(item.Key);
                }
            }



        }

        private static void UnpackOlang()
        {
            var location = @"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted";

            var workbook = new Workbook(WorkbookFormat.Excel2007);

            var offsets = new List<int>();
            var md5 = MD5.Create();

            var all = Directory.GetFiles(location, "*.olang", SearchOption.AllDirectories);

            var group = all.GroupBy(x => Path.GetFileName(x)).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());


            foreach (var item in group)
            {
                var hashes = item.Value.Select(x =>
                {
                    using (var fs = File.OpenRead(x))
                    {
                        var hash = md5.ComputeHash(fs);

                        return new { FileName = x, Hash = BitConverter.ToUInt64(hash, 0) ^ BitConverter.ToUInt64(hash, 8) };
                    }
                }).GroupBy(x => x.Hash).ToDictionary(x => x.Key, x => x.Select(y => y.FileName).ToList());

                if (hashes.Count != 1)
                {
                    Debugger.Break();
                }

                var olang = Olang.Unpack(hashes.First().Value.First());

                var key = Path.GetFileNameWithoutExtension(item.Key);

                var sheet = workbook.Worksheets.Add(key);

                sheet.Columns[1].SetWidth(600, WorksheetColumnWidthUnit.Pixel);
                sheet.Columns[1].CellFormat.WrapText = ExcelDefaultableBoolean.True;

                sheet.Columns[2].SetWidth(600, WorksheetColumnWidthUnit.Pixel);
                sheet.Columns[2].CellFormat.WrapText = ExcelDefaultableBoolean.True;

                var rowIndex = 1;
                foreach (var entitiy in olang.TextMap.OrderBy(x => x.Key))
                {
                    var row = sheet.Rows[rowIndex++];

                    row.Cells[0].Value = entitiy.Key;
                    row.Cells[1].Value = entitiy.Value;
                }
            }

            workbook.Save(Path.Combine(location, "olang_.xlsx"));

        }


        private static void ReplaceSpecial()
        {

            var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";
            var fileName = "BRIEFING2.xlsx";


            var extMap = new Dictionary<string, string>();
            var map = new Dictionary<string, string>();

            foreach (var line in File.ReadLines("replace.txt"))
            {
                var tokens = line.Split('\t');
                if (tokens.Length != 2)
                {

                }
                extMap[tokens[0]] = tokens[1];
            }

            var path = Path.Combine(location, fileName);
            var workbook = Workbook.Load(path);
            var sheet = workbook.Worksheets.First();


            var regex = new Regex("<R=[^>]+>");
            var rowIndex = 1;
            while (true)
            {
                var row = sheet.Rows[rowIndex++];

                if (row.Cells[0].Value != null)
                {
                    var text = row.Cells[2].GetText();
                    var ms = regex.Matches(text);
                    foreach (Match m in ms)
                    {
                        if (!m.Value.Contains(',') && !extMap.ContainsKey(m.Value))
                        {
                            map[m.Value] = null;

                        }
                    }
                }
                else
                { break; }
            }

            var buffer = new StringBuilder();
            foreach (var item in map.Keys)
            {
                buffer.AppendFormat("{0}\t{0}\n", item);
            }
            File.AppendAllText("replace.txt", buffer.ToString());




            foreach (var line in File.ReadLines("replace.txt"))
            {
                var tokens = line.Split('\t');
                if (tokens.Length != 2)
                {

                }
                extMap[tokens[0]] = tokens[1];
            }

            rowIndex = 1;

            while (true)
            {
                var row = sheet.Rows[rowIndex++];

                if (row.Cells[0].Value != null)
                {
                    var text = row.Cells[2].GetText();
                    var ms = regex.Matches(text);

                    foreach (Match m in ms)
                    {
                        if (!m.Value.Contains(','))
                        {
                            text = text.Replace(m.Value, extMap[m.Value]);



                        }
                    }

                    if (ms.Count > 0)
                    {
                        row.Cells[2].Value = text;
                    }
                }
                else
                { break; }
            }
            workbook.Save(path);
        }

        private static void Replace()
        {
            var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";
            var fileName = "BRIEFING.xlsx";

            var path = Path.Combine(location, fileName);

            ExcelUtility.ReplaceBackSpecialLetter(path, 1, 2);

        }
    }
}
