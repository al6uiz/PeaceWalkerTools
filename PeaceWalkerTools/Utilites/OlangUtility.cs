using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Infragistics.Documents.Excel;
using PeaceWalkerTools.Olang;

namespace PeaceWalkerTools
{
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
                    olang.TextList[i].Text = korean[i].ReplaceWideCharacters();
                }

                olang.Write(file);
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
