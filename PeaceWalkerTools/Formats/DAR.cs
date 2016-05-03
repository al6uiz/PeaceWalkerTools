using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PeaceWalkerTools
{
    class DAR
    {
        public static void Pack(string path)
        {
            var files = File.ReadAllLines(path).Where(x => x.Trim().Length > 0).ToList();
            var location = Path.GetDirectoryName(path);


            var outputPath = Path.Combine(location, Path.GetFileNameWithoutExtension(path));

            using (var ms = new MemoryStream())
            using (var fs = new BinaryWriter(ms, Encoding.ASCII))
            {
                fs.Write(files.Count);

                for (int i = 0; i < files.Count; i++)
                {
                    var entityPath = Path.Combine(location, files[i]);

                    var fileName = Path.GetFileName(entityPath);
                    var data = File.ReadAllBytes(entityPath);

                    fs.Write(Encoding.ASCII.GetBytes(fileName));
                    fs.Write((byte)0x00);


                    fs.BaseStream.Offset(4);


                    fs.Write(data.Length);

                    fs.BaseStream.Offset(16);

                    fs.Write(data, 0, data.Length);
                    fs.Write((byte)0x00);
                }

                File.WriteAllBytes(outputPath, ms.ToArray());
            }

        }

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

            using (var fs = File.OpenRead(path))
            {
                var count = fs.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    var fileName = fs.ReadString();

                    fs.Offset(4);

                    var length = fs.ReadInt32();

                    fs.Offset(16);

                    var data = new byte[length];

                    fs.Read(data, 0, data.Length);
                    fs.ReadByte();

                    File.WriteAllBytes(Path.Combine(extractLocation, fileName), data);


                    var outputInfo = string.Format(@"{0}\{1}", folderName, fileName);
                    fileList.AppendLine(outputInfo);

                    Console.WriteLine(outputInfo);
                }
            }

            var infoOutput = Path.Combine(location, name + ".dar.inf");

            File.WriteAllText(infoOutput, fileList.ToString());
        }
    }
}
