using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class Slot
    {
        public static void Read(string location)
        {
            var keyPath = Path.Combine(location, "SLOT.KEY");
            var dataPath = Path.Combine(location, "SLOT.DAT");

            using (var reader = new BinaryReader(File.OpenRead(keyPath)))
            using (var dataReader = new BinaryReader(File.OpenRead(dataPath)))
            {
                var hash0 = reader.ReadInt32();
                var hash1 = reader.ReadInt32();
                var hash2 = reader.ReadInt32();

                var keys = reader.ReadBytes((int)reader.BaseStream.Length - 12);

                var hash = new Hash(hash0, hash1, hash2);

                var keyReader = new BinaryReader(new MemoryStream(keys));

                var lastEnd = 0u;
                while (keyReader.BaseStream.Position < keyReader.BaseStream.Length)
                {
                    var rawStart = keyReader.ReadUInt32();
                    var rawEnd = keyReader.ReadUInt32();

                    var start = (0x000FFFFF & rawStart) << 0xB;
                    var end = (0x000FFFFF & rawEnd) << 0xB;

                    if (lastEnd > 0 & lastEnd != start)
                    {

                    }
                    lastEnd = end;

                    Debug.WriteLine(string.Format("{0:X3} {1:X3} ", rawStart >> 20, rawEnd >> 20));

                    var itemHash = keyReader.ReadInt32();

                    dataReader.BaseStream.Position = start;

                    if (end < start)
                    { continue; }

                    var raw = dataReader.ReadBytes((int)(end - start));

                    var hashCopy = hash;

                    DecryptionUtility.Decrypt(raw, ref hashCopy);


                    var t1 = BitConverter.ToInt32(raw, 0);
                    var t2 = BitConverter.ToInt32(raw, 4);
                    var t3 = BitConverter.ToInt32(raw, 8);
                    var t4 = BitConverter.ToInt32(raw, 12);

                    //var data = new byte[raw.Length - 16];
                    //Buffer.BlockCopy(raw, 16, data, 0, data.Length);
                    //File.WriteAllBytes(string.Format("{0:X8}.bin", start), raw);

                    //var uc = ZlibStream.UncompressBuffer(data);

                    //var t5 = BitConverter.ToInt32(uc, 0);
                    //File.WriteAllBytes(string.Format(@"SLOT\{0:X8}_{1:X8}.bin", start, itemHash), uc);

                    //Console.WriteLine("{0:X8} {1:X8} {2:X8} {3:X8} {4:X8} {5:X8}", itemHash, t1, t2, t3, t4, t5);
                }
                //var a0 = 0x0220450b;
                //var a1 = 0x08c31208;
                //var address = DoSometing(keys, a0, a1);

                //a0 = a1;

            }

        }

        private static int DoSometing(byte[] keys, int a0, int a1)
        {
            int v0 = 0;
            int a3 = 0;


            var v1 = 0x684; //entityCount 

            var t2 = a0;
            var t0 = a0;

            var a2 = v1 - 1;
            var t1 = keys;

            if (a2 >= 0)
            {
                t2 &= 0x00ffffff;
                v0 = a2 >> 0x1f; // v0 = av
                v0 += a2;

                a1 = v0 >> 1; // a1=v0/2
                v1 = a1 << 2; // v1 = a1*4;
                v0 = a1 << 4; // v0 = a1*8;

                v0 -= v1; // v0 = a1*8 - a1*4;

                v0 = BitConverter.ToInt32(t1, v0);

                if (a0 != v0)
                {
                    a3 = 0;
                    t2 &= 0x00ffffff;

                    if (v0 < t2)
                    {

                    }
                    a2 = a1 - 1;

                    v1 = a2 + a3;
                    v0 = v1 >> 0x1f;

                    v0 += v1;
                    a1 = v0 >> 1;
                    v1 = a1 << 2;
                    v0 = a1 << 4;
                    v0 -= v1;

                    if (a2 < a3)
                    {

                    }
                    else
                    {
                        v0 = BitConverter.ToInt32(keys, v0);
                        if (t0 == v0)
                        {
                            return v0;
                        }
                        else
                        {
                            v0 &= 0x00ffffff;
                            if (v0 < t2)
                            {

                            }
                            else
                            {

                            }
                        }
                    }
                }
            }

            return -1;
        }
    }

    class SlotKey
    {

    }

    public static class MipsOp
    {
        internal static int Ins(ref int rd, int rs, int p, int s)
        {

            var bitMask = mask[s];
            var originalMask = (int)~(bitMask << p);

            rd = (rd & originalMask) | (int)((rs & bitMask) << p);

            return rd;

        }

        static uint[] mask =
        {
            0x0,
            0x1,
            0x3,
            0x7,
            0xf,
            0x1f,
            0x3f,
            0x7f,
            0xff,
            0x1ff,
            0x3ff,
            0x7ff,
            0xfff,
            0x1fff,
            0x3fff,
            0x7fff,
            0xffff,
            0x1ffff,
            0x3ffff,
            0x7ffff,
            0xfffff,
            0x1fffff,
            0x3fffff,
            0x7fffff,
            0xffffff,
            0x1ffffff,
            0x3ffffff,
            0x7ffffff,
            0xfffffff,
            0x1fffffff,
            0x3fffffff,
            0x7fffffff,
            0xffffffff,

        };
    }
}
