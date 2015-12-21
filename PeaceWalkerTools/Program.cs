using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Infragistics.Documents.Excel;
using Ionic.Zlib;

namespace PeaceWalkerTools
{
    partial class Program
    {
        static void Main(string[] args)
        {

            if (Debugger.IsAttached)
            {
                args = Directory.GetFiles(@"D:\Projects\SandBox\TranslatePW\Font\bin\Debug\Extracted", " *.dar.inf");

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
                            case ".dar": break;
                            case ".qar": break;

                            case ".inf": Pack(path); break;
                            default:
                            break;
                        }
                    }
                }
            }
        }


        private static void Pack(string path)
        {
            var ext = Path.GetExtension(Path.GetFileNameWithoutExtension(path));

            switch (ext)
            {
                case ".dar": DAR.Pack(path); break;
                default:
                break;
            }
        }
    }
}
