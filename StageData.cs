using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zlib;

namespace PeaceWalkerTools
{
    class StageDataFile
    {
        public Hash Hash { get; private set; }

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
                var ENTITY_LIST_START = 32;

                var hash = sd.Hash;

                var header = fs.ReadBytes(20);
                Decrypt(header, ref hash);

                var entityCount = BitConverter.ToUInt16(header, 12);
                var lookupStart = BitConverter.ToInt32(header, 16);

                var listData = fs.ReadBytes(entityCount * ENTITY_ITEM_LENGTH);
                Decrypt(listData, ref hash);

                Debug.Assert(fs.Position == lookupStart);

                var lookupData = fs.ReadBytes(entityCount * LOOKUP_ITEM_LENGTH);
                Decrypt(lookupData, ref hash);


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
                    //var extension = "";
                    File.WriteAllBytes(string.Format(@"Extracted\{2:000}_{0:X8}.{1}", entity.Unknown, extension, i), decompressed);
                }
            }


            return sd;
        }


        private static void UpdateOffset(byte[] raw, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var position = offset + i * 16;

                var offseted = BitConverter.ToInt32(raw, position + 8);

                var a0 = offseted + offset;
                if (offseted != 0)
                {
                    Write(raw, position + 8, a0);
                }

                offseted = BitConverter.ToInt32(raw, position + 12);

                a0 = offseted + offset;
                if (offseted != 0)
                {
                    Write(raw, position + 0xc, a0);
                }
            }
        }


        private static void Decrypt(byte[] listData, ref Hash hash)
        {
            Decrypt(listData, 0, listData.Length, ref hash);
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

        public static void Decrypt(string path)
        {
            var raw = File.ReadAllBytes(path);

            var key1 = BitConverter.ToInt32(raw, 0);
            var key2 = BitConverter.ToInt32(raw, 4);
            var key3 = BitConverter.ToInt32(raw, 8);
            // +0xc 이놈은 뭔지모르겠다. 그냥 덮어써버린다.

            var firshtHash = GenerateHash(key1, key2, key3);
            var headerLength = 32;

            var hash = firshtHash;
            Decrypt(raw, 12, 0x14, ref hash);

            var entityCount = BitConverter.ToUInt16(raw, 0x18);

            var entityListStart = headerLength;
            var entitiyListLength = entityCount * 12;

            var lookupStart = entityListStart + entitiyListLength;
            var lookupLength = entityCount * 16;

            Decrypt(raw, entityListStart, entitiyListLength, ref hash);

            Decrypt(raw, lookupStart, lookupLength, ref hash);

            UpdateOffset(raw, lookupStart, entityCount);

            var entities = new List<Entity>();
            var lookupList = new List<LookupBlock>();

            for (int i = 0; i < entityCount; i++)
            {
                var offset = entityListStart + i * 12;
                var entity = new Entity
                {
                    Size = BitConverter.ToUInt32(raw, offset),
                    Unknown = BitConverter.ToUInt32(raw, offset + 4),
                    Position = BitConverter.ToUInt32(raw, offset + 8)
                };
                entities.Add(entity);

                var offset2 = lookupStart + i * 16;

                lookupList.Add(new LookupBlock
                {
                    Key = BitConverter.ToUInt32(raw, offset2),
                    Index = BitConverter.ToUInt32(raw, offset2 + 4),
                    Unknown2 = BitConverter.ToUInt32(raw, offset2 + 8),
                    Unknown3 = BitConverter.ToUInt32(raw, offset2 + 12)
                });
            }


            var a1 = HashString("init");
            var a2 = GetFileNameHash("data.cnf");

            var entityIndex = FindEntityFromLookup(raw, lookupStart, a1, a2);

            var entityOffset = entityListStart + entityIndex * 12;

            if (a1 >= 0)
            {
                var size = BitConverter.ToInt32(raw, entityOffset + 0);
                var key = BitConverter.ToInt32(raw, entityOffset + 4);
                var offset = BitConverter.ToInt32(raw, entityOffset + 8);


                File.WriteAllBytes("data.cnf", Decompress(firshtHash, raw, offset, size));
            }







            var output = path + ".dec2";
            File.WriteAllBytes(output, raw);




        }

        private static byte[] Decompress(Hash hash, byte[] raw, int offset, int size)
        {
            var compressed = new byte[size - 4];
            Buffer.BlockCopy(raw, offset + 4, compressed, 0, size - 4);

            Decrypt(raw, offset, 4, ref hash);

            var decomprssedSize = BitConverter.ToInt32(raw, offset);

            Decrypt(compressed, 0, compressed.Length, ref hash);

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
            if (!_reverseExtensionMap.TryGetValue((byte)(hash >> 24), out extension))
            {
                extension = EntityExtensions.Unknown;
            }
            return extension;
        }

        private static int FindEntityFromLookup(byte[] raw, int lookupStart, int hash1, int hash2)
        {
            var hash = hash2;

            if (hash1 != 0)
            {
                hash = hash1 >> 4;

                var temp = hash1 << 4;
                temp ^= hash;
                temp ^= 0x10EA;
                temp = ~temp;
                hash = temp ^ hash2;
            }

            var position = lookupStart;

            var key = BitConverter.ToInt32(raw, position);

            while (key != hash)
            {
                if (key < hash)
                {
                    position = BitConverter.ToInt32(raw, position + 8);
                }
                else
                {
                    position = BitConverter.ToInt32(raw, position + 12);
                }

                if (position == 0)
                {
                    return -1;
                }

                key = BitConverter.ToInt32(raw, position);

            }
            return BitConverter.ToInt32(raw, position + 4);
        }






        private static void Decrypt(byte[] raw, int offset, int length, ref Hash hash)
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

        //private static void Decrypt(byte[] raw, int offset, int length, ref Hash hash)
        //{
        //    var a3 = hash.Low;
        //    var t0 = hash.High;
        //    var v1 = (offset + 3);
        //    var v0 = 0x02e90000;
        //    v1 = (int)(v1 & 0xFFFFFFFC);
        //    length = (int)(length & 0xFFFFFFFC);

        //    var t1 = v0 | 0xedd;

        //    var lo = 0;
        //    while (length > 0)
        //    {
        //        lo = a3;
        //        lo += t0 * t1;
        //        v0 = BitConverter.ToInt32(raw, v1);
        //        length -= 4;
        //        v0 = v0 ^ t0;
        //        Write(raw, v1, v0);
        //        t0 = lo;
        //        v1 += 4;
        //    }
        //    lo = a3;
        //    v0 = v1 - offset;
        //    hash.High = t0;
        //}

        private static void Write(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
            data[offset + 2] = (byte)((value >> 16) & 0xFF);
            data[offset + 3] = (byte)((value >> 24) & 0xFF);
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
}
