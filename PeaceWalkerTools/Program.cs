using System.Diagnostics;
using System.IO;

namespace PeaceWalkerTools
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var iniFile = new InitializationFile("PeaceWalkerTools.ini");

            Settings.Working = iniFile["Global"]["Working"] ?? ".";
            Settings.SourceFolder = iniFile["Global"]["SourceLocation"];
            Settings.SourceUserFolder = Path.Combine(iniFile["Global"]["SourceLocation"], "USRDIR");
            Settings.SourceSystemFolder = Path.Combine(iniFile["Global"]["SourceLocation"], "SYSDIR");
            Settings.InstallFolder = iniFile["Global"]["InstallLocation"];
            Settings.TranslationFolder = iniFile["Global"]["TranslationFolder"];

            if (Debugger.IsAttached)
            {
                Test();
            }
            else
            {
                Process(args);
            }
        }

        private static void Process(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            foreach (var path in args)
            {
                if (File.Exists(path))
                {
                    var ext = Path.GetExtension(path);

                    switch (ext)
                    {
                        case ".dar":
                        {
                            DAR.Unpack(path);
                            break;
                        }
                        case ".qar":
                        {
                            QAR.Unpack(path);
                            break;
                        }
                        case ".txp":
                        {
                            TXP.Unpack(path);
                            break;
                        }
                        case ".inf":
                        {
                            Pack(path);
                            break;
                        }
                        default:
                        {
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
                case ".dar":
                {
                    DAR.Pack(path);
                    break;
                }
                case ".qar":
                {
                    QAR.Pack(path);
                    break;
                }
                default:
                break;
            }
        }
    }
}
