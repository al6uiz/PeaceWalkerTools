using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
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
}
