using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class InitializationFile
    {

        private string _path;

        public class Section
        {
            public string SectionName { get; set; }

            public InitializationFile File { get; set; }

            public string this[string key]
            {
                get
                {
                    return File.GetIniValue(SectionName, key);
                }
                set
                {
                    File.SetIniValue(SectionName, key, value);
                }
            }
        }

        public Section this[string section]
        {
            get
            {
                return new Section
                {
                    SectionName = section,
                    File = this
                };
            }
        }

        public InitializationFile(string path)
        {
            _path = Path.GetFullPath( path);  //INI 파일 위치를 생성할때 인자로 넘겨 받음
        }

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(    // GetIniValue 를 위해
            string section,
            string key,
            string def,
            StringBuilder retVal,
            int size,
            string filePath);



        [DllImport("kernel32.dll")]
        private static extern long WritePrivateProfileString(  // SetIniValue를 위해
            string section,
            string key,
            string val,
            string filePath);


        // INI 값을 읽어 온다. 
        private string GetIniValue(string Section, string Key)
        {
            var result = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", result, result.Capacity, _path);
            return result.ToString();
        }

        // INI 값을 셋팅
        private void SetIniValue(string Section, string Key, string Value)
        {
            var result=WritePrivateProfileString(Section, Key, Value, _path);
        }
    }
}

