using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ionic.Zlib;

namespace PeaceWalkerTools
{
    class SlotData
    {
        public static void Unpack(string location)
        {
            var keyPath = Path.Combine(location, "SLOT.KEY");
            var dataPath = Path.Combine(location, "SLOT.DAT.original");


            Hash hash;
            byte[] keys;

            using (var reader = new BinaryReader(File.OpenRead(keyPath)))
            {
                var hash0 = reader.ReadInt32();
                var hash1 = reader.ReadInt32();
                var hash2 = reader.ReadInt32();

                keys = reader.ReadBytes((int)reader.BaseStream.Length - 12);
                hash = new Hash(hash0, hash1, hash2);
            }


            var lastEnd = 0u;

            using (var keyReader = new BinaryReader(new MemoryStream(keys)))
            using (var dataReader = new BinaryReader(File.OpenRead(dataPath)))
            {
                while (keyReader.BaseStream.Position < keyReader.BaseStream.Length)
                {
                    var rawStart = keyReader.ReadUInt32();
                    var rawEnd = keyReader.ReadUInt32();

                    var start = (0x000FFFFF & rawStart) << 0xB;
                    var end = (0x000FFFFF & rawEnd) << 0xB;

                    lastEnd = end;

                    var remStart = rawStart >> 20;
                    var remEnd = rawEnd >> 20;

                    Debug.WriteLine(string.Format("{0:X3} {1:X3} ", rawStart >> 20, rawEnd >> 20));

                    var itemHash = keyReader.ReadInt32();

                    dataReader.BaseStream.Position = start;

                    if (end < start)
                    { continue; }

                    var raw = dataReader.ReadBytes((int)(end - start));

                    var hashCopy = hash;

                    DecryptionUtility.Decrypt(raw, ref hashCopy);
                    var unknown1 = BitConverter.ToInt32(raw, 0);
                    var unknown2 = BitConverter.ToInt32(raw, 4);

                    if (unknown1 != 0x00100004)
                    {

                    }
                    else if (unknown2 != 0)
                    {
                    }

                    var compressedSize = BitConverter.ToInt32(raw, 8);
                    var uncompressedSize = BitConverter.ToInt32(raw, 12);

                    var data = new byte[compressedSize];
                    Buffer.BlockCopy(raw, 16, data, 0, compressedSize);


                    var uncompressed = ZlibStream.UncompressBuffer(data);

                    var t5 = BitConverter.ToInt32(uncompressed, 0);
                    var output = string.Format(@"SLOT\{0:X8}_{1:X8}.slot", start, itemHash);

                    if (Directory.Exists("SLOT") == false)
                    {
                        Directory.CreateDirectory("SLOT");
                    }
                    File.WriteAllBytes(output, uncompressed);

                    Console.WriteLine("{0:X8} {1:X8} {2:X8} {3:X8} {4:X8} {5:X8}", itemHash, unknown1, unknown2, compressedSize, uncompressedSize, t5);
                }
            }
        }

        public static void Pack(string location, string[] filter)
        {
            var keyPath = Path.Combine(location, "SLOT.KEY");
            var dataPath = Path.Combine(location, "SLOT.DAT");

            File.Copy(Path.Combine(location, "SLOT.DAT.original"), Path.Combine(location, "SLOT.DAT"), true);

            Hash hash;
            byte[] rawKeys;

            using (var reader = new BinaryReader(File.OpenRead(keyPath)))
            {
                var hash0 = reader.ReadInt32();
                var hash1 = reader.ReadInt32();
                var hash2 = reader.ReadInt32();

                rawKeys = reader.ReadBytes((int)reader.BaseStream.Length - 12);
                hash = new Hash(hash0, hash1, hash2);
            }

            if (!Directory.Exists("SLOT"))
            {
                Directory.CreateDirectory("SLOT");
            }

            HashSet<string> filterSet;

            if (filter == null)
            {
                filterSet = new HashSet<string>();
            }
            else
            {
                filterSet = new HashSet<string>(filter);
            }
            var items = new List<SlotItemInfo>();

            using (var keyReader = new BinaryReader(new MemoryStream(rawKeys)))
            {
                while (keyReader.BaseStream.Position < keyReader.BaseStream.Length)
                {
                    var rawStart = keyReader.ReadUInt32();
                    var rawEnd = keyReader.ReadUInt32();

                    var start = (0x000FFFFF & rawStart) << 0xB;
                    var end = (0x000FFFFF & rawEnd) << 0xB;

                    items.Add(new SlotItemInfo
                    {
                        Start = start,
                        End = end,
                        Hash = keyReader.ReadInt32()
                    });
                }
            }

            var current = 0;

            using (var writer = new BinaryWriter(File.OpenWrite(dataPath)))
            {
                foreach (var item in items)
                {
                    current++;
                    var sourcePath = string.Format(@"SLOT\{0:X8}_{1:X8}.slot", item.Start, item.Hash);
                    if (!filterSet.Contains(sourcePath))
                    {
                        continue;
                    }
                    Debug.WriteLine(string.Format("Process {0:X8} ({1}/{2})", item.Start, current, items.Count));

                    var data = File.ReadAllBytes(sourcePath);

                    byte[] compressed;
                    if (Compress(data, (int)item.Length - 16, out compressed) == false)
                    {
                        Debug.WriteLine("Failed to compress!");
                        continue;
                    }

                    var raw = new byte[item.Length];

                    using (var rawWriter = new BinaryWriter(new MemoryStream(raw)))
                    {
                        rawWriter.Write(0x00100004);
                        rawWriter.Write(0x00000000);
                        rawWriter.Write(compressed.Length);
                        rawWriter.Write(data.Length);

                        rawWriter.Write(compressed);
                        rawWriter.Flush();
                    }

                    var hashCopy = hash;
                    DecryptionUtility.Decrypt(raw, ref hashCopy);
                    writer.BaseStream.Position = item.Start;
                    writer.Write(raw);
                }
            }
        }

        private static bool Compress(byte[] data, int max, out byte[] compressed)
        {
            compressed = null;

            var level = CompressionLevel.Level4;

        ReTry:
            using (var tempMemoryStream = new MemoryStream())
            {
                using (var zipStream = new ZlibStream(tempMemoryStream, CompressionMode.Compress, level))
                {
                    zipStream.Write(data, 0, data.Length);
                }
                compressed = tempMemoryStream.ToArray();
            }

            if (compressed.Length > max)
            {
                if (level == CompressionLevel.BestCompression)
                {
                    return false;
                }

                level = (CompressionLevel)(level + 1);
                Debug.WriteLine(string.Format("- Retry {0}", level));

                goto ReTry;
            }

            return true;
        }

        class SlotItemInfo
        {
            public uint Start { get; set; }
            public uint Length { get { return End - Start; } }
            public uint End { get; set; }
            public int Hash { get; set; }
        }
    }

    class _ReverseSlot
    { 
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

        public static void ProcessItem(byte[] data)
        {
            var a0 = 0;
            var a1 = 1;
            var a2 = 1;

            var t0 = 0;
            var t1 = 0;
            var t2 = 0;
            var t7 = int.MinValue;

            var s7 = t0;
            var s6 = t1;
            var s5 = t2;
            var s4 = t2;
            var s3 = a0;
            var s2 = a1;

            var v1 = 0;

            var s0 = a0 + 4;

            var sp_4 = 0;
            var sp_0 = 0;

            var t6 = BitConverter.ToInt32(data, a0);
            var v0 = t6 << 3;
            v0 = v0 + 0x803;

            MipsOP.Ins(ref v0, 0, 0, 0xb);
            v0 = v0 + a0;

            if (t6 > 0)
            {
                sp_0 = v0;
                v0 = a2 & 0x1000;
                var s1 = 0;
                var fp = 0x7f000000;
                sp_4 = v0;
                var t4 = BitConverter.ToInt32(data, s0);

            _0880559c:
                v0 = t4;
                MipsOP.Ins(ref v0, 0, 0, 0x18);

                if (fp == v0)
                {

                _08805668:
                    t7 = int.MinValue;
                    s0 = s0 + 8;
                    s1 = s1 + 1;

                    if (s1 < t6)
                    {
                        t4 = BitConverter.ToInt32(data, s0);
                        goto _0880559c;
                    }
                }
                else
                {
                    a1 = t4 | t7;

                    if (t4 == 0)
                    {
                        //goto _08805668;
                    }
                    else
                    {
                        v0 = s2 ^ 3;
                        a0 = BitConverter.ToInt32(data, s0 + 4);
                        t7 = sp_0;
                        s0 = s0 + 8;
                        v1 = BitConverter.ToInt32(data, s0 + 4);
                    }

                }
            }
        }
    }
}
