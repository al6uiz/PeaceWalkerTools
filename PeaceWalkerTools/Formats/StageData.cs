using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zlib;

namespace PeaceWalkerTools
{
    class StageDataFile
    {
        public Hash Hash { get; private set; }

        internal static Dictionary<byte, EntityExtensions> ReverseExtensionMap
        {
            get
            {
                return _reverseExtensionMap;
            }
        }

        private const int ENTITY_ITEM_LENGTH = 12;
        private const int LOOKUP_ITEM_LENGTH = 16;

        public static StageDataFile Read(string path)
        {
            var sd = new StageDataFile();

            if (!Directory.Exists("Extracted"))
            {
                Directory.CreateDirectory("Extracted");
            }

            string combineKey = null;

            using (var fs = File.OpenRead(path))
            {
                var raw = File.ReadAllBytes(path);

                var key1 = fs.ReadInt32();
                var key2 = fs.ReadInt32();
                var key3 = fs.ReadInt32();

                // +0xc 이놈은 뭔지모르겠다. 그냥 덮어써버린다.

                sd.Hash = new Hash(key1, key2, key3);

                var hash = sd.Hash;

                var header = fs.ReadBytes(20);
                DecryptionUtility.Decrypt(header, ref hash);

                var entityCount = BitConverter.ToUInt16(header, 12);
                var lookupStart = BitConverter.ToInt32(header, 16);

                var listData = fs.ReadBytes(entityCount * ENTITY_ITEM_LENGTH);
                DecryptionUtility.Decrypt(listData, ref hash);

                Debug.Assert(fs.Position == lookupStart);

                var lookupData = fs.ReadBytes(entityCount * LOOKUP_ITEM_LENGTH);
                DecryptionUtility.Decrypt(lookupData, ref hash);


                var entities = new List<Entity>();
                var lookupList = new List<LookupBlock>();

                for (int i = 0; i < entityCount; i++)
                {
                    var offset = i * ENTITY_ITEM_LENGTH;
                    var entity = new Entity
                    {
                        Size = BitConverter.ToUInt32(listData, offset),
                        Unknown = BitConverter.ToUInt32(listData, offset + 4),
                        Position = BitConverter.ToUInt32(listData, offset + 8),
                    };
                    entities.Add(entity);

                    offset = i * LOOKUP_ITEM_LENGTH;
                    var lookup = new LookupBlock
                    {
                        Key = BitConverter.ToUInt32(lookupData, offset),
                        Index = BitConverter.ToUInt32(lookupData, offset + 4),
                        Unknown2 = BitConverter.ToUInt32(lookupData, offset + 8),
                        Unknown3 = BitConverter.ToUInt32(lookupData, offset + 12)
                    };

                    lookupList.Add(lookup);

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


                    var extension = entity.Extension == EntityExtensions.Unknown ? "" : entity.Extension.ToString();

                    File.WriteAllBytes(string.Format(@"Extracted\{2:000}_{0:X8}.{1}", entity.Unknown, extension, i), decompressed);
                }
            }


            return sd;
        }

        private static readonly byte[] EXTENSION_HASH =
        {
             0x1A,  0xF1,  0x21,  0x20,
             0x30,  0x05,  0x6E,  0x1D,
             0x35,  0x6B,  0x0A,  0x64,
             0xFF,  0x1B,  0x18,  0x02,

             0x10,  0x38,  0x1E,  0x03,
             0x16,  0x13,  0x65,  0x24,
             0x15,  0x5F,  0x33,  0x23,
             0x17,  0x6D,  0x6C,  0x06,

             0x5D,  0x09,  0x36,  0x61,
             0x04,  0x1F,  0x01,  0x19,
             0x69,  0x13,  0x6A,  0x12,
             0x34,  0x14,  0x22,  0x0F,

             0x63,  0x68,  0x5E,  0x66,
             0xF0,  0x1C,  0x32,  0x6F,
             0x0C,  0x07,  0x31,  0x08,
             0x11,  0x60,  0x13,  0x0B,

             0x37,  0x13,  0xF2,
        };



        private static Dictionary<string, int> _extensionMap = Enum.GetValues(typeof(EntityExtensions)).Cast<EntityExtensions>().ToDictionary(x => x.ToString(), x => (int)x);
        private static Dictionary<byte, EntityExtensions> _reverseExtensionMap = GetReverseExtension();

        private static Dictionary<byte, EntityExtensions> GetReverseExtension()
        {
            var map = new Dictionary<byte, EntityExtensions>();
            for (int i = 0; i < EXTENSION_HASH.Length; i++)
            {
                map[EXTENSION_HASH[i]] = (EntityExtensions)i;
            }
            return map;
        }

        private static int GetFileNameHash(string input)
        {
            var indexOfDot = input.IndexOf('.');

            var extension = input.Substring(indexOfDot + 1);

            var hash = 0;

            for (int i = 0; input[i] != 0 && input[i] != '.'; i++)
            {
                var temp1 = hash >> 0x13;
                var temp2 = hash << 5;
                temp2 |= temp1;
                hash = temp2 + input[i];

                hash &= 0x00FFFFFF;
            }


            int typeIndex;
            _extensionMap.TryGetValue(extension, out typeIndex);
            hash |= EXTENSION_HASH[typeIndex] << 24;

            return hash;
        }

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

            EntityExtensions extension;
            if (!ReverseExtensionMap.TryGetValue((byte)(hash >> 24), out extension))
            {
                extension = EntityExtensions.Unknown;
            }
            return extension;
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

    class Entity
    {
        public uint Size { get; set; }
        public uint Unknown { get; set; }
        public uint Position { get; set; }
        public EntityExtensions Extension { get; set; }

        public override string ToString()
        {
            return string.Format("@{0} ( {1} ) {2:X8}", Position, Size, Unknown);
        }
    }
    class LookupBlock
    {
        public uint Key { get; set; }
        public uint Index { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }

        public override string ToString()
        {
            return string.Format("{0:X8} {1} {2} {3}", Key, Index, Unknown2, Unknown3);
        }
    }

    struct Hash
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

    enum EntityExtensions
    {
        mdpe, qar, vrdv, vrd,
        mgm, mds, row, spk,
        cap, rat, mtfa, eqp,
        psq, dcd, mtst, gcx,

        cvd, bgp, ohd, tri,
        rpd, mdp, vlm, vcpg,
        kms, la2, ptcp, vcp,
        fcx, ola, rcm, lt2,

        olang, mtsq, pcmp, vram,
        mdh, mmd, bin, mdpb,
        img, mdc, vib, zon,
        cddl, txp, vrdt, nav,

        cmf, png, la3, lst,
        dar, ypk, rlc, mtra,
        geom, cv2, prx, mtar,
        eft, slot, mdl, mtcm,

        sep, mdb, cnf,

        Unknown = -1,
    }


    static class DecryptionUtility
    {
        private static void Write(byte[] data, int offset, int value)
        {
            data[offset + 0] = (byte)((value >> 0) & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
            data[offset + 2] = (byte)((value >> 16) & 0xFF);
            data[offset + 3] = (byte)((value >> 24) & 0xFF);
        }


        public static void Decrypt(byte[] listData, ref Hash hash)
        {
            Decrypt(listData, 0, listData.Length, ref hash);
        }

        public static void Decrypt(byte[] raw, int offset, int length, ref Hash hash)
        {
            var position = (int)((offset + 3) & 0xFFFFFFFC);
            length = (int)(length & 0xFFFFFFFC);

            var high = hash.High;

            while (length > 0)
            {
                var temp1 = hash.Low;
                temp1 += high * 0x02E90EDD;

                var temp2 = BitConverter.ToInt32(raw, position);
                temp2 = temp2 ^ high;
                Write(raw, position, temp2);
                high = temp1;

                length -= 4;
                position += 4;
            }

            hash.High = high;
        }

    }
}
