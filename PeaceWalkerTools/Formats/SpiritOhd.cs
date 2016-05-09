using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class SpiritOhd
    {
        public static List<string> Read(string path)
        {
            var offset = 0x4c;

            var list = new List<string>();

            using (var fs = File.OpenRead(path))
            {
                while (offset < fs.Length)
                {
                    fs.Position = offset;
                    list.Add(fs.ReadString());

                    offset += 0x80;
                }
            }

            return list;
        }

        internal static void Write(string path, List<string> lisst)
        {
            var offset = 0x4c;

            var index = 0;
            using (var fs = File.OpenWrite(path))
            {
                while (offset < fs.Length)
                {
                    fs.Position = offset;
                    var text = lisst[index++];
                    var rawText = Encoding.UTF8.GetBytes(text);
                    Array.Resize(ref rawText, 0x40);
                    fs.Write(rawText, 0, 0x40);

                    offset += 0x80;
                }
            }
        }

        public List<string> Texts { get; private set; } = new List<string>();
    }
}
