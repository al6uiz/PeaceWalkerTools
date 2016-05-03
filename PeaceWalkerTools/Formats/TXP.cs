using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ionic.Zlib;

namespace PeaceWalkerTools
{

    public class TXP
    {
        public static void Unpack(string path)
        {
            var list1 = new List<TxpEntity>();
            var list2 = new List<TxpEntitySub>();

            using (var fs = new BinaryReader(File.OpenRead(path)))
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

                fs.BaseStream.Position = header1Start;

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

                fs.BaseStream.Position = header2Start;

                for (int i = 0; i < metaCount; i++)
                {
                    var entity = new TxpEntitySub();

                    entity.Unknown4 = fs.ReadInt32();
                    entity.Unknown5 = fs.ReadInt32();

                    entity.Unknown6 = fs.ReadInt32();
                    entity.PaletteStart = fs.ReadInt32();

                    entity.X = fs.ReadSingle();
                    entity.Y = fs.ReadSingle();

                    entity.RatioX = fs.ReadSingle();
                    entity.RatioY = fs.ReadSingle();

                    entity.Width = fs.ReadInt16();
                    entity.Height = fs.ReadInt16();
                    entity.Skip1 = fs.ReadInt16();
                    entity.Skip2 = fs.ReadInt16();

                    list2.Add(entity);
                }


                fs.BaseStream.Position = list2[0].PaletteStart;
                var current = 0;
                while (fs.BaseStream.Position < list1[0].PixelStart)
                {
                    if (current + 1 < list2.Count && list2[current + 1].PaletteStart == fs.BaseStream.Position)
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
                    fs.BaseStream.Position = list1[i].PixelStart;
                    var next = i + 1 < count ? list1[i + 1].PixelStart : (int)fs.BaseStream.Length;
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




                foreach (var group in list2.GroupBy(x => x.PixelDataIndex).ToDictionary(x => x.Key, x => x.ToList()))
                {
                    var whole = group.Value.First(x => x.X == 0f && x.Y == 0);
                    var pixel = list1[whole.PixelDataIndex];

                    pixel.Width = (int)(whole.Width / whole.RatioX);
                    pixel.Height = (int)(whole.Height / whole.RatioY);
                }

                Dump(list1, list2);

                int index = 0;

                foreach (var group in list2.GroupBy(x => x.PixelDataIndex).ToDictionary(x => x.Key, x => x.ToList()))
                {
                    var first = group.Value.First();

                    TextureFormat format = null;
                    if (first.Colors.Count == 16)
                    {
                        format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];
                    }
                    else if (first.Colors.Count == 256)
                    {
                        format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];
                    }
                    else
                    {
                        continue;
                    }

                    var pixel = list1[group.Key];
                    var data = ImageUtility.Unswizzle(format, pixel.RawData, pixel.Width);
                    if (format.Size == 0)
                    {
                        var data2 = new byte[data.Length * 2];
                        for (int j = 0; j < data.Length; j++)
                        {
                            data2[j * 2] = (byte)(data[j] & 0xF);
                            data2[j * 2 + 1] = (byte)(data[j] >> 4);
                        }
                        data = data2;
                    }

                    //var item = group.Value.First();

                    //var location = Path.GetDirectoryName(path);
                    //var name = Path.GetFileNameWithoutExtension(path);
                    //var fileName = Path.Combine(location, "png", string.Format("{0}__{1}.png", name, index++));

                    //FileUtility.PrepareFolder(fileName);

                    //using (var bitmap = GetBitmap(item.Colors, pixel.Width, data.Length / pixel.Width, data))
                    //{
                    //    if (bitmap != null)
                    //    {
                    //        bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                    //    }
                    //}


                    //continue;


                    foreach (var item in group.Value)
                    {
                        var crop = ImageUtility.Crop(data, pixel.Width, (int)(pixel.Width * item.X), (int)(pixel.Height * item.Y), item.Width, item.Height);


                        var location = Path.GetDirectoryName(path);
                        var name = Path.GetFileNameWithoutExtension(path);
                        var fileName = Path.Combine(location, "png", string.Format("{0}_{1}.png", name, index++));

                        FileUtility.PrepareFolder(fileName);

                        using (var bitmap = GetBitmap(item.Colors, item.Width, item.Height, crop))
                        {
                            if (bitmap != null)
                            {
                                bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                }
            }
        }



        internal static Bitmap GetBitmap(List<Color> colors, int width, int height, byte[] data)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            var palette = bitmap.Palette;
            for (int j = 0; j < colors.Count; j++)
            {
                palette.Entries[j] = colors[j];
            }
            bitmap.Palette = palette;

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(data, 0, bmpData.Scan0, width * height);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
        private static byte[] Skip(byte[] pixelData, short skip1, short skip2)
        {
            if (skip1 == 0 && skip2 == 0)
            {
                return pixelData;
            }
            var a = new List<byte>();
            var start = 8 * 8 * skip2;

            for (int i = start; i < pixelData.Length; i++)
            {


                a.Add(pixelData[i]);
            }

            return a.ToArray();
        }

        private static void Dump(List<TxpEntity> list1, List<TxpEntitySub> list2)
        {
            var index = 0;
            foreach (var item in list1)
            {
                Debug.WriteLine(
                    "[{0}] {1,-3} * {2,-3} @{3,-5} : {4:N0} / {5:X8} {6:X8} {7:X8} {8:X8}",
                    index++, item.Width, item.Height, item.PixelStart, item.RawData.Length,
                    item.Unknown0, item.Unknown1, item.Unknown2, item.Unknown3);
            }

            index = 0;
            foreach (var item in list2)
            {


                Debug.WriteLine("#{14,-3} {0,-3} * {1,-3} * {2,-3} [{3}] @{4,-8} {5,-3} {6,-3} :  {7:X8} {8:X8} {9:X8} {10:0.000} {11:0.000} {12:0.000} {13:0.000}", item.Width, item.Height, item.Colors.Count, item.PixelDataIndex, item.PaletteStart,
                    item.Skip1, item.Skip2,
                    item.Unknown4, item.Unknown5, item.Unknown6, item.X, item.Y, item.RatioX, item.RatioY, index++);
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
        public int Width { get; internal set; }
        public int Height { get; internal set; }
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
        public float X { get; set; }
        public float Y { get; set; }
        public float RatioX { get; set; }
        public float RatioY { get; set; }
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
        public short Skip1 { get; internal set; }
        public short Skip2 { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} * {1} * {2}/ {3} ", Width, Height, Colors.Count, PaletteStart);
        }
    }
}