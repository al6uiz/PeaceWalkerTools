using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PeaceWalkerTools
{
    class QAR
    {
        public static void Unpack(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            var ext = Path.GetExtension(path);
            var location = Path.GetDirectoryName(path);
            var folderName = string.Format("{0}_{1}", name, ext.Substring(1));

            var extractLocation = Path.Combine(location, folderName);

            if (Directory.Exists(extractLocation) == false)
            {
                Directory.CreateDirectory(extractLocation);
            }

            Console.WriteLine(path);

            var fileList = new StringBuilder();

            var list = new List<int>();
            var names = new List<string>();


            using (var fs = new BinaryReader(File.OpenRead(path)))
            {

                fs.BaseStream.Position = fs.BaseStream.Length - 4;

                var infoOffset = fs.ReadInt32();

                fs.BaseStream.Position = infoOffset;

                var count = fs.ReadInt32();
                var total = 0;

                for (int i = 0; i < count; i++)
                {
                    var value1 = fs.ReadInt32();

                    var length = fs.ReadInt32();

                    list.Add(value1);

                    list.Add(length);

                    total += length;
                }

                for (int i = 0; i < count; i++)
                {
                    var fileName = fs.BaseStream.ReadString();
                    names.Add(fileName);


                    var outputInfo = string.Format("{0:X8}\t{1}\\{2}", list[i * 2], folderName, fileName);
                    fileList.AppendLine(outputInfo);

                    Console.WriteLine(outputInfo);
                }

                fs.BaseStream.Position = 0;
                for (int i = 0; i < count; i++)
                {
                    var data = fs.ReadBytes(list[i * 2 + 1]);
                    File.WriteAllBytes(Path.Combine(extractLocation, names[i]), data);
                    fs.BaseStream.Offset(128);
                }
            }

            var infoOutput = Path.Combine(location, name + ".qar.inf");

            File.WriteAllText(infoOutput, fileList.ToString());
        }

        public static void Pack(string path)
        {
            var output = path.RemoveExtension(1);
            var source = Path.GetFileNameWithoutExtension(path);

            var name = Path.GetFileNameWithoutExtension(source);
            var ext = Path.GetExtension(source);
            var location = Path.GetDirectoryName(path);

            var extractLocation = Path.Combine(location, string.Format("{0}_{1}", name, ext.Substring(1)));

            var listHash = new List<int>();
            var listOffset = new List<int>();
            var listName = new List<string>();
            foreach (var item in File.ReadAllLines(path).Select(x => x.Split('\t')))
            {
                listHash.Add(int.Parse(item[0], NumberStyles.HexNumber));
                listName.Add(item[1]);
            }

            Debug.WriteLine(path);

            using (var fs = new BinaryWriter(File.Create(output), Encoding.UTF8))
            {
                for (int i = 0; i < listName.Count; i++)
                {
                    var inputPath = Path.Combine(location, listName[i]);
                    if (File.Exists(inputPath + "new"))
                    {
                        inputPath = inputPath + "new";
                    }


                    var data = File.ReadAllBytes(inputPath);
                    fs.Write(data, 0, data.Length);
                    fs.BaseStream.Offset(128);
                    listOffset.Add((int)data.Length);
                }
                var infoOffset = (int)fs.BaseStream.Position;

                fs.Write(listName.Count);


                for (int i = 0; i < listName.Count; i++)
                {
                    fs.Write(listHash[i]);
                    fs.Write(listOffset[i]);
                }

                for (int i = 0; i < listName.Count; i++)
                {
                    var entityName = Path.GetFileName(listName[i]);
                    var rawName = Encoding.ASCII.GetBytes(entityName);
                    fs.Write(rawName, 0, rawName.Length);
                    fs.Write((byte)0);
                }

                fs.BaseStream.Offset(4);
                fs.Write(infoOffset);
            }
        }
    }
}
