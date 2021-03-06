﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Ionic.Zlib;

namespace PeaceWalkerTools
{
    public class StageDataFile
    {
        [XmlAttribute]
        public int HashKey1 { get; set; }
        [XmlAttribute]
        public int HashKey2 { get; set; }
        [XmlAttribute]
        public int HashKey3 { get; set; }

        [XmlIgnore]
        public Hash Hash { get; private set; }


        private const int ENTITY_ITEM_LENGTH = 12;
        private const int LOOKUP_ITEM_LENGTH = 16;

        public static StageDataFile Read(string path, string outputLocation)
        {
            var sd = new StageDataFile();

            FileUtility.PrepareFolder(outputLocation);


            string combineKey = null;

            using (var fs = File.OpenRead(path))
            {
                var raw = File.ReadAllBytes(path);

                sd.HashKey1 = fs.ReadInt32();
                sd.HashKey2 = fs.ReadInt32();
                sd.HashKey3 = fs.ReadInt32();

                // +0xc 이놈은 뭔지모르겠다. 그냥 덮어써버린다.

                sd.Hash = new Hash(sd.HashKey1, sd.HashKey2, sd.HashKey3);

                var hash = sd.Hash;

                var header = fs.ReadBytes(20);
                DecryptionUtility.Decrypt(header, ref hash);

                sd.Unknown1 = BitConverter.ToUInt32(header, 0);
                sd.Unknown2 = BitConverter.ToUInt32(header, 4);
                sd.Unknown3 = BitConverter.ToUInt32(header, 8);

                sd.EntityCount = BitConverter.ToUInt16(header, 12);
                sd.LookupStart = BitConverter.ToInt32(header, 16);

                var listData = fs.ReadBytes(sd.EntityCount * ENTITY_ITEM_LENGTH);
                DecryptionUtility.Decrypt(listData, ref hash);

                Debug.Assert(fs.Position == sd.LookupStart);

                var lookupData = fs.ReadBytes(sd.EntityCount * LOOKUP_ITEM_LENGTH);
                DecryptionUtility.Decrypt(lookupData, ref hash);


                sd.Entities = new List<Entity>();
                sd.LookupList = new List<LookupBlock>();

                for (int i = 0; i < sd.EntityCount; i++)
                {
                    var offset = i * ENTITY_ITEM_LENGTH;
                    var entity = new Entity
                    {
                        Size = BitConverter.ToUInt32(listData, offset),
                        Hash = BitConverter.ToUInt32(listData, offset + 4),
                        Position = BitConverter.ToUInt32(listData, offset + 8),
                    };
                    sd.Entities.Add(entity);

                    offset = i * LOOKUP_ITEM_LENGTH;

                    var lookup = new LookupBlock
                    {
                        Key = BitConverter.ToUInt32(lookupData, offset),
                        Index = BitConverter.ToUInt32(lookupData, offset + 4),
                        Unknown1 = BitConverter.ToUInt32(lookupData, offset + 8),
                        Unknown2 = BitConverter.ToUInt32(lookupData, offset + 12)
                    };

                    sd.LookupList.Add(lookup);

                    var decompressed = Decompress(sd.Hash, raw, (int)entity.Position, (int)entity.Size);

                    if (BitConverter.ToInt32(decompressed, 0) == 0x636F6E2E) // '.noc'ache
                    {
                        using (var sr = new StringReader(Encoding.ASCII.GetString(decompressed)))
                        {
                            string line = null;

                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.EndsWith(".rlc"))
                                {
                                    combineKey = line.Remove(line.IndexOf('.'));
                                }
                            }
                        }
                    }

                    entity.Extension = ResolveExtension(HashString(combineKey), (int)lookup.Key);
                    entity.CombineKey = combineKey;

                    var extension = entity.Extension == EntityExtensions.Unknown ? "" : entity.Extension.ToString();
                    var fileName = Path.Combine(outputLocation, string.Format("{0:X8}.{1}", entity.Hash, extension));

                    File.WriteAllBytes(fileName, decompressed);
                }
            }


            return sd;
        }

        public void Write(string outputPath)
        {
            var path = Path.GetTempFileName();
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                Hash = new Hash(HashKey1, HashKey2, HashKey3);
                EntityCount = (ushort)Entities.Count;
                LookupStart = 32 + EntityCount * ENTITY_ITEM_LENGTH;

                writer.Write(HashKey1);
                writer.Write(HashKey2);
                writer.Write(HashKey3);


                writer.BaseStream.Position = LookupStart + EntityCount * LOOKUP_ITEM_LENGTH;

                var index = 0;

                foreach (var entity in Entities)
                {
                    Debug.Write(string.Format("[{0} / {1}] ", ++index, Entities.Count));

                    entity.Position = (uint)writer.BaseStream.Position;
                    var data = Path.Combine(Settings.Working, "STAGEDAT", string.Format("{0:X8}.{1}", entity.Hash, entity.Extension));
                    var raw = File.ReadAllBytes(data);
                    var compressed = Compress(Hash, raw);
                    entity.Size = (uint)compressed.Length;
                    writer.Write(compressed);
                }


                byte[] subBuffer;
                var hash = Hash;

                writer.BaseStream.Position = 12;

                using (var msSub = new MemoryStream(20))
                using (var subWriter = new BinaryWriter(msSub))
                {
                    subWriter.Write(Unknown1);
                    subWriter.Write(Unknown2);
                    subWriter.Write(Unknown3);
                    subWriter.Write(EntityCount);
                    subWriter.Write((ushort)5);
                    subWriter.Write(LookupStart);

                    subBuffer = msSub.ToArray();
                }

                DecryptionUtility.Decrypt(subBuffer, ref hash);

                writer.Write(subBuffer);


                using (var msSub = new MemoryStream(Entities.Count * ENTITY_ITEM_LENGTH))
                using (var subWriter = new BinaryWriter(msSub))
                {
                    foreach (var entity in Entities)
                    {
                        subWriter.Write(entity.Size);
                        subWriter.Write(entity.Hash);
                        subWriter.Write(entity.Position);
                    }

                    subBuffer = msSub.ToArray();
                }

                DecryptionUtility.Decrypt(subBuffer, ref hash);
                writer.Write(subBuffer);


                using (var msSub = new MemoryStream(Entities.Count * LOOKUP_ITEM_LENGTH))
                using (var subWriter = new BinaryWriter(msSub))
                {
                    foreach (var lookup in LookupList)
                    {
                        subWriter.Write(lookup.Key);
                        subWriter.Write(lookup.Index);
                        subWriter.Write(lookup.Unknown1);
                        subWriter.Write(lookup.Unknown2);
                    }

                    subBuffer = msSub.ToArray();
                }

                DecryptionUtility.Decrypt(subBuffer, ref hash);

                writer.Write(subBuffer);

            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(path, outputPath);

        }

        private void Print(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Write(string.Format("{0:X2} ", data[i]));
            }

            Debug.WriteLine("");
        }

        [XmlIgnore]
        public ushort EntityCount { get; set; }
        [XmlIgnore]
        public int LookupStart { get; set; }

        public List<Entity> Entities { get; set; }
        public List<LookupBlock> LookupList { get; set; }

        [XmlAttribute]
        public uint Unknown1 { get; set; }
        [XmlAttribute]
        public uint Unknown2 { get; set; }
        [XmlAttribute]
        public uint Unknown3 { get; set; }


        public static int HashString(string input)
        {
            var hash = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var temp1 = hash >> 0x13;
                var temp2 = hash << 5;
                temp2 |= temp1;
                hash = temp2 + input[i];

                hash &= 0x00FFFFFF;
            }

            return hash;
        }

        private static byte[] Decompress(Hash hash, byte[] raw, int offset, int size)
        {
            var compressed = new byte[size - 4];
            Buffer.BlockCopy(raw, offset + 4, compressed, 0, size - 4);

            DecryptionUtility.Decrypt(raw, offset, 4, ref hash);

            var decomprssedSize = BitConverter.ToInt32(raw, offset);

            DecryptionUtility.Decrypt(compressed, 0, compressed.Length, ref hash);

            return ZlibStream.UncompressBuffer(compressed);
        }

        private static byte[] Compress(Hash hash, byte[] data)
        {
            byte[] result = null;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                var sw = Stopwatch.StartNew();
                using (var msCompress = new MemoryStream())
                {

                    using (var zs = new ZlibStream(msCompress, CompressionMode.Compress, CompressionLevel.None))
                    {
                        zs.Write(data, 0, data.Length);
                    }

                    var compressed = msCompress.ToArray();

                    writer.Write(data.Length);
                    writer.Write(compressed);

                    result = ms.ToArray();
                    sw.Stop();
                    Debug.WriteLine(string.Format("Compression : {0:N0} -> {1:N0} - {2} ms ", data.Length, compressed.Length, sw.ElapsedMilliseconds));
                }
            }

            DecryptionUtility.Decrypt(result, 0, result.Length, ref hash);

            return result;
        }


        private static EntityExtensions ResolveExtension(int hash1, int hash2)
        {
            var hash = hash2;

            if (hash1 != 0)
            {
                var temp = (hash1 >> 4) ^ (hash1 << 4);
                temp ^= 0x10EA;
                temp = ~temp;

                hash = temp ^ hash2;
            }

            return ExtensionUtility.GetExtension((byte)(hash >> 24));
        }


        private static Hash GenerateHash(int hash0, int hash1, int hash2)
        {
            var hash = new Hash();

            hash0 = hash0 ^ hash1;
            hash.Low = hash0 * hash2;
            hash.High = hash0 | (hash0 ^ 0x6576) << 16;

            return hash;
        }
    }

    public class Entity
    {
        [XmlIgnore]
        public uint Hash { get; set; }
        [XmlAttribute("Hash")]
        public string HashHex
        {
            get { return Hash.ToString("X8"); }
            set { Hash = uint.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlIgnore]
        public uint Position { get; set; }

        [XmlIgnore]
        public uint Size { get; set; }

        [XmlAttribute]
        public string CombineKey { get; set; }

        [XmlAttribute]
        public EntityExtensions Extension { get; set; }
        public override string ToString()
        {
            return string.Format("@{0} ( {1} ) {2:X8}", Position, Size, Hash);
        }
    }
    public class LookupBlock
    {
        [XmlIgnore]
        public uint Key { get; set; }
        [XmlAttribute("Key")]
        public string KeyHex
        {
            get { return Key.ToString("X8"); }
            set { Key = uint.Parse(value, NumberStyles.HexNumber); }
        }

        [XmlAttribute]
        public uint Index { get; set; }

        [XmlIgnore]
        public uint Unknown1 { get; set; }
        [XmlAttribute("Unknown1")]
        public string Unknown1Hex
        {
            get { return Unknown1.ToString("X8"); }
            set { Unknown1 = uint.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlIgnore]
        public uint Unknown2 { get; set; }
        [XmlAttribute("Unknown2")]
        public string Unknown2Hex
        {
            get { return Unknown2.ToString("X8"); }
            set { Unknown2 = uint.Parse(value, NumberStyles.HexNumber); }
        }

        public override string ToString()
        {
            return string.Format("{0:X8} {1} {2} {3}", Key, Index, Unknown1, Unknown2);
        }
    }

    public struct Hash
    {
        public Hash(int hash0, int hash1, int hash2)
        {
            hash0 = hash0 ^ hash1;

            Low = hash0 * hash2;
            High = hash0 | (hash0 ^ 0x6576) << 16;
        }

        public int High { get; set; }
        public int Low { get; set; }
    }


}
