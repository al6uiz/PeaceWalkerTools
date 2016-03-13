using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Slot.Read(@"E:\Peace Walker\PSP_GAME\USRDIR");


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
