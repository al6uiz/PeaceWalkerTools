using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Ionic.Zlib;
using PeaceWalkerTools.Olang;

namespace PeaceWalkerTools
{
    class Slot
    {
        public static void Read(string location)
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

                    File.WriteAllBytes(string.Format(@"SLOT\{0:X8}_{1:X8}.dec", start, itemHash), raw);


                    var data = new byte[compressedSize];
                    Buffer.BlockCopy(raw, 16, data, 0, compressedSize);


                    var uncompressed = ZlibStream.UncompressBuffer(data);

                    var t5 = BitConverter.ToInt32(uncompressed, 0);
                    var output = string.Format(@"SLOT\{0:X8}_{1:X8}.bin", start, itemHash);
                    File.WriteAllBytes(output, uncompressed);

                    DumpRBX(uncompressed, output);

                    Console.WriteLine("{0:X8} {1:X8} {2:X8} {3:X8} {4:X8} {5:X8}", itemHash, unknown1, unknown2, compressedSize, uncompressedSize, t5);
                }

            }

        }


        public static void Write(string location)
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

            var olangMap = new HashSet<string>(Directory.GetFiles("SLOT", "*.bin.xml").Select(x => Path.GetFileNameWithoutExtension(x)));
            var items = new List<SlotItem>();

            using (var keyReader = new BinaryReader(new MemoryStream(rawKeys)))
            {
                while (keyReader.BaseStream.Position < keyReader.BaseStream.Length)
                {
                    var rawStart = keyReader.ReadUInt32();
                    var rawEnd = keyReader.ReadUInt32();

                    var start = (0x000FFFFF & rawStart) << 0xB;
                    var end = (0x000FFFFF & rawEnd) << 0xB;

                    items.Add(new SlotItem
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
                    var sourcePath = string.Format(@"SLOT\{0:X8}_{1:X8}.bin", item.Start, item.Hash);
                    if (!olangMap.Contains(Path.GetFileName(sourcePath)))
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

        private static void DumpRBX(byte[] data, string path)
        {
            var list = new List<SlotOlang>();
            var count = 0;
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 'R' && data[i + 1] == 'B' && data[i + 2] == 'X' && data[i + 3] == 0)
                {
                    var size = data.Length - i;
                    var raw = new byte[size];
                    Buffer.BlockCopy(data, i, raw, 0, raw.Length);

                    var olang = OlangFile.Read(new MemoryStream(raw));

                    string exportPath = null;
                    if (count == 0)
                    {
                        exportPath = path + ".olang";
                    }
                    else
                    {
                        exportPath = string.Format("{0}_{1}.olang", path, count);
                    }

                    olang.Write(exportPath);

                    list.Add(new SlotOlang
                    {
                        Length = (int)new FileInfo(exportPath).Length,
                        Offset = i,
                        Name = Path.GetFileName(exportPath)
                    });
                    count++;

                }
            }

            if (list.Count > 0)
            {
                SerializationHelper.Save(list, path + ".xml");
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

    class SlotItem
    {
        public uint Start { get; set; }
        public uint Length { get { return End - Start; } }
        public uint End { get; set; }
        public int Hash { get; set; }
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

    public class SlotOlang
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public int Offset { get; set; }
        [XmlAttribute]
        public int Length { get; set; }

    }
}
