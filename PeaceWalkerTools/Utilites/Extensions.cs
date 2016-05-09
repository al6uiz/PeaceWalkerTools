using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PeaceWalkerTools
{
    public static class Extensions
    {
        public static string RemoveExtension(this string path, int count)
        {
            var location = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);

            for (int i = 0; i < count; i++)
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            return Path.Combine(location, fileName);
        }

        public static bool Find(this byte[] data, string text, int start)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (data[start + i] != (byte)text[i])
                { return false; }
            }

            return true;

            //return Find(Encoding.ASCII.GetBytes(p), raw, start);
        }


        public static bool Find(this byte[] data, byte[] find, int offset)
        {
            if (find.Length + offset < data.Length)
            {
                for (int i = 0; i < find.Length; i++)
                {
                    if (data[offset + i] != find[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }



        private static List<byte> _stringBuffer = new List<byte>();

        public static void Offset(this Stream fs, int v)
        {
            var offset = v - fs.Position % v;

            if (offset < v)
            {
                fs.Position += offset;
            }

        }
        public static string ReadString(this Stream fs)
        {
            return ReadString(fs, fs.Position);
        }

        public static string ReadString(this Stream fs, long position)
        {
            return ReadString(fs, position, Encoding.UTF8);
        }

        public static string ReadString(this Stream fs, long position, Encoding encoding)
        {
            _stringBuffer.Clear();

            fs.Position = position;
            try
            {
                while (true)
                {

                    fs.Read(_readBuffer, 0, _readBuffer.Length);
                    for (int i = 0; i < _readBuffer.Length; i++)
                    {
                        position++;
                        if (_readBuffer[i] != 0)
                        {
                            _stringBuffer.Add(_readBuffer[i]);
                        }
                        else
                        {
                            var raw = _stringBuffer.ToArray();

                            var text = encoding.GetString(raw);

                            return text;
                        }
                    }

                }
            }
            finally
            {
                fs.Position = position;
            }
        }


        static byte[] _readBuffer = new byte[0xff];
        public static int ReadUInt16(this Stream fs)
        {
            fs.Read(_readBuffer, 0, 2);

            return BitConverter.ToUInt16(_readBuffer, 0);
        }

        public static int ReadInt32(this Stream fs)
        {
            fs.Read(_readBuffer, 0, 4);

            return BitConverter.ToInt32(_readBuffer, 0);
        }
        public static short ReadInt16(this Stream fs)
        {
            fs.Read(_readBuffer, 0, 2);

            return BitConverter.ToInt16(_readBuffer, 0);
        }

        public static byte[] ReadBytes(this Stream fs, int length)
        {
            var read = new byte[length];
            fs.Read(read, 0, length);
            return read;
        }

        public static string GetString(this byte[] raw, int offset)
        {
            _stringBuffer.Clear();
            while (true)
            {
                if (offset < raw.Length && raw[offset] != 0)
                {
                    _stringBuffer.Add(raw[offset]);

                    offset++;
                }
                else
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(_stringBuffer.ToArray());
        }
    }
}
