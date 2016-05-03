using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    public class Settings
    {
        public static string Working { get; set; }
        public static string SourceFolder{ get; set; }
        public static string SourceUserFolder { get; set; }
        public static string InstallFolder { get; set; }
        public static string SourceSystemFolder { get; internal set; }
        public static string TranslationFolder { get; internal set; }
    }
}
