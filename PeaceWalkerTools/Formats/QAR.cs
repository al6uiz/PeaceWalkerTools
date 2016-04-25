using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PeaceWalkerTools
{
    class QAR
    {
        public static void Unpack(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var location = Path.GetDirectoryName(path);

            var extractLocation = Path.Combine(location, string.Format("{0}_{1}", name, ext.Substring(1)));

            if (Directory.Exists(extractLocation) == false)
            {
                Directory.CreateDirectory(extractLocation);
            }

            Debug.WriteLine(path);

            using (var fs = File.OpenRead(path))
            {

                var list = new List<int>();
                var names = new List<string>();

                fs.Position = fs.Length - 4;
                var infoOffset = fs.ReadInt32();
                fs.Position = infoOffset;
                var count = fs.ReadInt32();
                var total = 0;
                for (int i = 0; i < count; i++)
                {
                    var value1 = fs.ReadInt32();
                    var value2 = fs.ReadInt32();
                    list.Add(value1);
                    list.Add(value2);
                    total += value2;
                }

                for (int i = 0; i < count; i++)
                {
                    names.Add(fs.ReadString());
                }

                fs.Position = 0;
                for (int i = 0; i < count; i++)
                {
                    File.WriteAllBytes(Path.Combine(extractLocation, names[i]), fs.ReadBytes(list[i * 2 + 1]));
                    fs.Offset(128);
                }
            }
        }
    }
}
