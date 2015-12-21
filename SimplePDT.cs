using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class SimplePDT
    {


        public static void ExtractSimplePDTs()
        {
            var location = @"E:\Games\Metal Gear Solid\Metal Gear Solid - Peace Walker\Metal Gear Solid Peace Walker GEN-D3\PSP_GAME\USRDIR";

            var files = new string[] { "VOICEBF.PDT", "VOICEPS.PDT", "VOICERT.PDT" };

            foreach (var file in files)
            {
                //ExtractPDT(location, file);
            }
        }

        public static void ExtractSimplePDT(string location, string file)
        {
            var buffer = new byte[0xFFFF];

            using (var fs = File.OpenRead(Path.Combine(location, file)))
            {
                var key = fs.ReadByte();

                using (var ws = File.Create(Path.Combine(location, file + ".dec")))
                {
                    while (fs.Length > fs.Position)
                    {
                        var count = (int)Math.Min(fs.Length - fs.Position, buffer.Length);
                        fs.Read(buffer, 0, count);
                        for (int i = 0; i < count; i++)
                        {
                            buffer[i] = (byte)(buffer[i] ^ key);
                        }
                        ws.WriteAsync(buffer, 0, count);
                    }
                }
            }
        }
    }
}
