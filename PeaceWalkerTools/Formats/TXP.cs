using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Ionic.Zlib;

namespace PeaceWalkerTools
{

    public class TXP
    {
        public static void Unpack(string path)
        {
            var list1 = new List<TxpEntity>();
            var list2 = new List<TxpEntitySub>();

            using (var fs = File.OpenRead(path))
            {
                var typeFlag = fs.ReadInt32();
                var key = fs.ReadInt32();

                var count = fs.ReadInt32();
                var metaCount = fs.ReadInt32();

                Debug.WriteLine(string.Format("{0} {1} {2}", Path.GetFileName(path), count, metaCount));

                var colorCount = fs.ReadInt32();

                var header1Start = fs.ReadInt32();
                var header2Start = fs.ReadInt32();

                var paletteStart = fs.ReadInt32();

                fs.Position = header1Start;

                for (int i = 0; i < count; i++)
                {
                    var entity = new TxpEntity();

                    entity.Unknown0 = fs.ReadInt32();
                    entity.Unknown1 = fs.ReadInt32();
                    entity.Unknown2 = fs.ReadInt32();

                    if ((entity.Unknown0 & 0xFF) > 0x10)
                    {
                        entity.Unknown3 = fs.ReadInt32();
                        entity.PixelStart = fs.ReadInt32();
                    }
                    else
                    {
                        entity.PixelStart = fs.ReadInt32();
                        entity.Unknown3 = fs.ReadInt32();
                    }

                    list1.Add(entity);
                }

                fs.Position = header2Start;

                for (int i = 0; i < metaCount; i++)
                {
                    var entity = new TxpEntitySub();

                    entity.Unknown4 = fs.ReadInt32();
                    entity.Unknown5 = fs.ReadInt32();

                    entity.Unknown6 = fs.ReadInt32();
                    entity.PaletteStart = fs.ReadInt32();

                    entity.Unknown8 = fs.ReadInt32();
                    entity.Unknown9 = fs.ReadInt32();

                    entity.Unknown10 = fs.ReadInt32();
                    entity.Unknown11 = fs.ReadInt32();

                    entity.Width = fs.ReadInt16();
                    entity.Height = fs.ReadInt16();
                    entity.Unknown12 = fs.ReadInt32();

                    list2.Add(entity);
                }


                fs.Position = list2[0].PaletteStart;
                var current = 0;
                while (fs.Position < list1[0].PixelStart)
                {
                    if (current + 1 < list2.Count && list2[current + 1].PaletteStart == fs.Position)
                    {
                        current++;
                    }
                    var colors = list2[current].Colors;

                    var r = fs.ReadByte();
                    var g = fs.ReadByte();
                    var b = fs.ReadByte();
                    var a = fs.ReadByte();

                    colors.Add(Color.FromArgb(a, r, g, b));

                    colorCount -= 4;
                }

                for (int i = 0; i < count; i++)
                {
                    fs.Position = list1[i].PixelStart;
                    var next = i + 1 < count ? list1[i + 1].PixelStart : (int)fs.Length;
                    var length = next - list1[i].PixelStart;
                    if (length <= 0)
                    {
                        list1[i].RawData = new byte[0];
                    }
                    else
                    {
                        var raw = fs.ReadBytes(length);
                        list1[i].RawData = raw;

                        if (list1[i].IsCompressed)
                        {
                            var compressedSize = BitConverter.ToInt32(raw, 0);

                            var compressed = new byte[compressedSize];
                            Buffer.BlockCopy(raw, 4, compressed, 0, compressedSize);

                            list1[i].RawData = ZlibStream.UncompressBuffer(compressed);
                        }
                    }
                }

                Dump(list1, list2);


                for (int i = 0; i < metaCount; i++)
                {
                    var paletteEntity = list2[i];
                    var pixelEntity = list1[paletteEntity.PixelDataIndex];


                    var pixelData = pixelEntity.RawData;

                    var location = Path.GetDirectoryName(path);
                    var name = Path.GetFileNameWithoutExtension(path);
                    var fileName = Path.Combine(location, string.Format("{0}_{1}.png", name, i));

                    using (var bitmap = new TiledPixelReader(pixelData, paletteEntity.Width, paletteEntity.Height, paletteEntity.Colors).GetImage())
                    {
                        if (bitmap != null)
                        {
                            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }

            }
        }

        public static void Pack(string path)
        {
            var list1 = new List<TxpEntity>();
            var list2 = new List<TxpEntitySub>();

            using (var fs = File.OpenRead(path))
            {
                var typeFlag = fs.ReadInt32();
                var key = fs.ReadInt32();

                var count = fs.ReadInt32();
                var metaCount = fs.ReadInt32();

                Debug.WriteLine(string.Format("{0} {1} {2}", Path.GetFileName(path), count, metaCount));

                var colorCount = fs.ReadInt32();

                var header1Start = fs.ReadInt32();
                var header2Start = fs.ReadInt32();

                var paletteStart = fs.ReadInt32();

                fs.Position = header1Start;

                for (int i = 0; i < count; i++)
                {
                    var entity = new TxpEntity();

                    entity.Unknown0 = fs.ReadInt32();
                    entity.Unknown1 = fs.ReadInt32();
                    entity.Unknown2 = fs.ReadInt32();

                    if ((entity.Unknown0 & 0xFF) > 0x10)
                    {
                        entity.Unknown3 = fs.ReadInt32();
                        entity.PixelStart = fs.ReadInt32();
                    }
                    else
                    {
                        entity.PixelStart = fs.ReadInt32();
                        entity.Unknown3 = fs.ReadInt32();
                    }

                    list1.Add(entity);
                }

                fs.Position = header2Start;

                for (int i = 0; i < metaCount; i++)
                {
                    var entity = new TxpEntitySub();

                    entity.Unknown4 = fs.ReadInt32();
                    entity.Unknown5 = fs.ReadInt32();

                    entity.Unknown6 = fs.ReadInt32();
                    entity.PaletteStart = fs.ReadInt32();

                    entity.Unknown8 = fs.ReadInt32();
                    entity.Unknown9 = fs.ReadInt32();

                    entity.Unknown10 = fs.ReadInt32();
                    entity.Unknown11 = fs.ReadInt32();

                    entity.Width = fs.ReadInt16();
                    entity.Height = fs.ReadInt16();
                    entity.Unknown12 = fs.ReadInt32();

                    list2.Add(entity);
                }


                fs.Position = list2[0].PaletteStart;
                var current = 0;
                while (fs.Position < list1[0].PixelStart)
                {
                    if (current + 1 < list2.Count && list2[current + 1].PaletteStart == fs.Position)
                    {
                        current++;
                    }
                    var colors = list2[current].Colors;

                    var r = fs.ReadByte();
                    var g = fs.ReadByte();
                    var b = fs.ReadByte();
                    var a = fs.ReadByte();

                    colors.Add(Color.FromArgb(a, r, g, b));

                    colorCount -= 4;
                }

                for (int i = 0; i < count; i++)
                {
                    fs.Position = list1[i].PixelStart;
                    var next = i + 1 < count ? list1[i + 1].PixelStart : (int)fs.Length;
                    var length = next - list1[i].PixelStart;
                    if (length <= 0)
                    {
                        list1[i].RawData = new byte[0];
                    }
                    else
                    {
                        var raw = fs.ReadBytes(length);
                        list1[i].RawData = raw;

                        if (list1[i].IsCompressed)
                        {
                            var compressedSize = BitConverter.ToInt32(raw, 0);

                            var compressed = new byte[compressedSize];
                            Buffer.BlockCopy(raw, 4, compressed, 0, compressedSize);

                            list1[i].RawData = ZlibStream.UncompressBuffer(compressed);
                        }
                    }
                }

                Dump(list1, list2);


                for (int i = 0; i < metaCount; i++)
                {
                    var paletteEntity = list2[i];
                    var pixelEntity = list1[paletteEntity.PixelDataIndex];


                    var pixelData = pixelEntity.RawData;

                    var location = Path.GetDirectoryName(path);
                    var name = Path.GetFileNameWithoutExtension(path);
                    var fileName = Path.Combine(location, string.Format("{0}_{1}.png", name, i));

                    using (var bitmap = new TiledPixelReader(pixelData, paletteEntity.Width, paletteEntity.Height, paletteEntity.Colors).GetImage())
                    {
                        if (bitmap != null)
                        {
                            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }

            }
        }

        private static void Dump(List<TxpEntity> list1, List<TxpEntitySub> list2)
        {
            foreach (var item in list1)
            {
                Debug.WriteLine("{0:X8} {1:X8} {2:X8} {3:X8} @{4,-5} {5:X8}", item.Unknown0, item.Unknown1, item.Unknown2, item.Unknown3, item.PixelStart, item.RawData.Length);
            }

            foreach (var item in list2)
            {
                Debug.WriteLine("{0,-3} * {1,-3} * {3,-3} @{2,-5} {4:X8} {5:X8} {6:X8} {7:X8} {8:X8} {9:X8} {10:X8} {11:X8}", item.Width, item.Height, item.PaletteStart, item.Colors.Count, item.Unknown4, item.Unknown5, item.Unknown6, item.Unknown8, item.Unknown9, item.Unknown10, item.Unknown11, item.Unknown12);
            }
            Debug.WriteLine("");
        }
    }

    class TxpEntity
    {
        public TxpEntity()
        {
        }

        public bool IsCompressed { get { return ((Unknown0 >> 4) & 0xF) != 0; } }

        public int Unknown0 { get; set; }
        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }
        public int PixelStart { get; set; }
        public byte[] RawData { get; internal set; }
    }

    class TxpEntitySub
    {
        public TxpEntitySub()
        {
            Colors = new List<Color>();
        }

        public int Unknown4 { get; set; }
        public int Unknown5 { get; set; }
        public int Unknown6 { get; set; }
        public int PaletteStart { get; set; }
        public int Unknown8 { get; set; }
        public int Unknown9 { get; set; }
        public int Unknown10 { get; set; }
        public int Unknown11 { get; set; }
        public int Unknown12 { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int PixelDataIndex
        {
            get
            {
                return (Unknown6 - 0x20) / 0x14;
            }
        }

        public List<Color> Colors { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} * {1} * {2}/ {3} ", Width, Height, Colors.Count, PaletteStart);
        }
    }
}