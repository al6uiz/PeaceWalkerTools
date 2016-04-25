using System;
using System.IO;

namespace PeaceWalkerTools
{
    class SimplePDT
    {


        public static void ExtractSimplePDTs()
        {
            var location = @"E:\Peace Walker\PSP_GAME\USRDIR";

            var files = new string[] { "VOICEBF.PDT", "VOICEPS.PDT", "VOICERT.PDT" };

            foreach (var file in files)
            {
                ExtractSimplePDT(location, file);
            }
        }

        public static void ExtractSimplePDT(string location, string file)
        {
            var buffer = new byte[0xFFFF];

            using (var fs = File.OpenRead(Path.Combine(location, file)))
            {
                var key = fs.ReadByte();
                var skip = 0;
                using (var ws = File.Create(Path.Combine(location, file + ".dec")))
                {
                    while (fs.Length > fs.Position)
                    {
                        var count = (int)Math.Min(fs.Length - fs.Position, buffer.Length);
                        fs.Read(buffer, 0, count);
                        for (int i = 0; i < count; i++)
                        {
                            if (skip++ >= 16)
                            {
                                buffer[i] = (byte)(buffer[i] ^ key);
                            }
                        }
                        ws.Write(buffer, 0, count);
                    }
                }
            }
        }
    }
}
