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
            var masterImages = new List<TxpMasterImage>();
            var subimages = new List<TxpSubImage>();

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
                    var entity = new TxpMasterImage();

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

                    masterImages.Add(entity);
                }

                fs.BaseStream.Position = header2Start;

                for (int i = 0; i < metaCount; i++)
                {
                    var entity = new TxpSubImage();

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
                    entity.OffsetX = fs.ReadInt16();
                    entity.OffsetY = fs.ReadInt16();

                    subimages.Add(entity);
                }


                fs.BaseStream.Position = subimages[0].PaletteStart;
                var current = 0;
                while (fs.BaseStream.Position < masterImages[0].PixelStart)
                {
                    if (current + 1 < subimages.Count && subimages[current + 1].PaletteStart == fs.BaseStream.Position)
                    {
                        current++;
                    }
                    var colors = subimages[current].Colors;

                    var r = fs.ReadByte();
                    var g = fs.ReadByte();
                    var b = fs.ReadByte();
                    var a = fs.ReadByte();

                    colors.Add(Color.FromArgb(a, r, g, b));

                    colorCount -= 4;
                }

                for (int i = 0; i < count; i++)
                {
                    fs.BaseStream.Position = masterImages[i].PixelStart;
                    var next = i + 1 < count ? masterImages[i + 1].PixelStart : (int)fs.BaseStream.Length;
                    var length = next - masterImages[i].PixelStart;
                    if (length <= 0)
                    {
                        masterImages[i].RawData = new byte[0];
                    }
                    else
                    {
                        var raw = fs.ReadBytes(length);

                        var pixel = masterImages[i];
                        pixel.RawData = raw;

                        if (pixel.IsCompressed)
                        {
                            var compressedSize = BitConverter.ToInt32(raw, 0);

                            var compressed = new byte[compressedSize];
                            Buffer.BlockCopy(raw, 4, compressed, 0, compressedSize);

                            pixel.RawData = ZlibStream.UncompressBuffer(compressed);
                        }

                        pixel.RawHeight = pixel.Unknown1 & 0xFFF;
                        pixel.RawWidth = pixel.RawData.Length / pixel.RawHeight;
                    }

                }




                foreach (var group in subimages.GroupBy(x => x.PixelDataIndex).ToDictionary(x => x.Key, x => x.ToList()))
                {
                    var whole = group.Value.First(x => x.X == 0f && x.Y == 0);
                    var pixel = masterImages[whole.PixelDataIndex];

                    pixel.Width = (int)(whole.Width / whole.RatioX);
                    pixel.Height = (int)(whole.Height / whole.RatioY);
                }

                Dump(masterImages, subimages);
                return;
                int index = 0;

                foreach (var group in subimages.GroupBy(x => x.PixelDataIndex).ToDictionary(x => x.Key, x => x.ToList()))
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

                    var pixel = masterImages[group.Key];

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

                    foreach (var item in group.Value)
                    {
                        var crop = ImageUtility.Crop(data, pixel.Width, (int)(pixel.Width * item.X), (int)(pixel.Height * item.Y), item.Width, item.Height);


                        var location = Path.GetDirectoryName(path);
                        var name = Path.GetFileNameWithoutExtension(path);
                        var fileName = Path.Combine(location, "png", string.Format("{0}_{1}.png", name, index++));

                        FileUtility.PrepareFolderFile(fileName);

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



        public static void Pack(string path)
        {
            File.Copy(path, path + "new", true);
            path = path + "new";
            var masterImages = new List<TxpMasterImage>();
            var subImages = new List<TxpSubImage>();

            using (var fs = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.ReadWrite)))
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
                    var entity = new TxpMasterImage();

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

                    masterImages.Add(entity);
                }

                fs.BaseStream.Position = header2Start;

                for (int i = 0; i < metaCount; i++)
                {
                    var entity = new TxpSubImage();

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
                    entity.OffsetX = fs.ReadInt16();
                    entity.OffsetY = fs.ReadInt16();

                    subImages.Add(entity);
                }


                fs.BaseStream.Position = subImages[0].PaletteStart;

                var current = 0;

                while (fs.BaseStream.Position < masterImages[0].PixelStart)
                {
                    if (current + 1 < subImages.Count && subImages[current + 1].PaletteStart == fs.BaseStream.Position)
                    {
                        current++;
                    }
                    var colors = subImages[current].Colors;

                    var r = fs.ReadByte();
                    var g = fs.ReadByte();
                    var b = fs.ReadByte();
                    var a = fs.ReadByte();

                    colors.Add(Color.FromArgb(a, r, g, b));

                    colorCount -= 4;
                }

                for (int i = 0; i < count; i++)
                {
                    fs.BaseStream.Position = masterImages[i].PixelStart;
                    var next = i + 1 < count ? masterImages[i + 1].PixelStart : (int)fs.BaseStream.Length;
                    var length = next - masterImages[i].PixelStart;
                    if (length <= 0)
                    {
                        masterImages[i].RawData = new byte[0];
                    }
                    else
                    {
                        var raw = fs.ReadBytes(length);
                        var pixel = masterImages[i];
                        pixel.RawData = raw;

                        if (pixel.IsCompressed)
                        {
                            var compressedSize = BitConverter.ToInt32(raw, 0);

                            var compressed = new byte[compressedSize];
                            Buffer.BlockCopy(raw, 4, compressed, 0, compressedSize);

                            pixel.RawData = ZlibStream.UncompressBuffer(compressed);
                        }

                        pixel.RawHeight = pixel.Unknown1 & 0xFFF;
                        pixel.RawWidth = pixel.RawData.Length / pixel.RawHeight;
                    }
                }

                Dump(masterImages, subImages);

                int index = 0;
                var groups = subImages.GroupBy(x => x.PixelDataIndex).ToDictionary(x => x.Key, x => x.ToList());

                foreach (var group in groups)
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

                    var pixel = masterImages[group.Key];

                    var data = ImageUtility.Unswizzle(format, pixel.RawData, pixel.RawWidth);

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


                    foreach (var item in group.Value)
                    {
                        var location = Path.GetDirectoryName(path);
                        var name = Path.GetFileNameWithoutExtension(path);
                        var fileName = Path.Combine(location, "png", string.Format("{0}_{1}.png", name, index++));

                        var image = Bitmap.FromFile(fileName) as Bitmap;
                        if (image.PixelFormat != PixelFormat.Format8bppIndexed || item.Colors.Count != image.Palette.Entries.Length)
                        {
                            if (item.Colors.Count < image.Palette.Entries.Length)
                            {                                 // Need to convert color
                                image = ImageUtility.ConvertTo8bppFormat(image);
                            }
                        }

                        var bitData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                        var newData = new byte[bitData.Stride * bitData.Height];
                        Marshal.Copy(bitData.Scan0, newData, 0, newData.Length);

                        image.UnlockBits(bitData);

                        ImageUtility.Write(data, pixel.RawWidth, newData, (int)(pixel.Width * item.X), (int)(pixel.Height * item.Y), item.Width, item.Height);

                        item.Colors.Clear();
                        item.Colors.AddRange(image.Palette.Entries);
                    }

                    if (format.Size == 0)
                    {
                        var data2 = new byte[data.Length / 2];
                        for (int j = 0; j < data.Length; j += 2)
                        {
                            data2[j / 2] = (byte)((data[j] & 0xF) | (data[j + 1] >> 4));
                        }
                        data = data2;
                    }

                    data = ImageUtility.Swizzle(format, data, (uint)pixel.RawWidth, (uint)pixel.RawHeight);


                    if (pixel.IsCompressed)
                    {
                        var originalSize = pixel.RawData.Length;
                        data = Compress(data, originalSize);

                        fs.BaseStream.Position = pixel.PixelStart;
                        fs.BaseStream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                        fs.BaseStream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        fs.BaseStream.Position = pixel.PixelStart;
                        fs.BaseStream.Write(data, 0, data.Length);
                    }
                }

                foreach (var item in subImages)
                {
                    fs.BaseStream.Position = item.PaletteStart;

                    for (int i = 0; i < item.Colors.Count; i++)
                    {
                        var color = item.Colors[i];

                        fs.BaseStream.WriteByte(color.R);
                        fs.BaseStream.WriteByte(color.G);
                        fs.BaseStream.WriteByte(color.B);
                        fs.BaseStream.WriteByte(color.A);
                    }
                }
            }
        }

        private static byte[] Compress(byte[] data, int originalSize)
        {
            var level = CompressionLevel.Default;
            ReTry:
            using (var ms = new MemoryStream())
            {
                using (var stream = new ZlibStream(ms, CompressionMode.Compress, level))
                {
                    stream.Write(data, 0, data.Length);
                }

                var compressed = ms.ToArray();
                if (compressed.Length > originalSize)
                {
                    if (level < CompressionLevel.BestCompression)
                    {
                        level = (level + 1);
                        goto ReTry;
                    }
                    else
                    {
                        return null;
                    }
                }

                return compressed;
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

        private static void Dump(List<TxpMasterImage> masters, List<TxpSubImage> subItems)
        {
            var index = 0;
            foreach (var item in masters)
            {
                Debug.WriteLine(
                    "[{0}] {1,-3} * {2,-3} @{3,-5} : {4:N0} / {5:X8} {6:X8} {7:X8} {8:X8}",
                    index++, item.Width, item.Height, item.PixelStart, item.RawData.Length,
                    item.Unknown0, item.Unknown1, item.Unknown2, item.Unknown3);
            }

            index = 0;
            foreach (var item in subItems)
            {


                Debug.WriteLine("#{14,-3} {0,-3} * {1,-3} * {2,-3} [{3}] @{4,-8} {5,-3} {6,-3} :  {7:X8} {8:X8} {9:X8} {10:0.000} {11:0.000} {12:0.000} {13:0.000}", item.Width, item.Height, item.Colors.Count, item.PixelDataIndex, item.PaletteStart,
                    item.OffsetX, item.OffsetY,
                    item.Unknown4, item.Unknown5, item.Unknown6, item.X, item.Y, item.RatioX, item.RatioY, index++);
            }
            Debug.WriteLine("");
        }
    }

    class TxpMasterImage
    {
        public TxpMasterImage()
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
        public int RawWidth { get; internal set; }
        public int RawHeight { get; internal set; }

        public bool IsValid { get { return PixelStart > 0; } }
    }

    class TxpSubImage
    {
        public TxpSubImage()
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
        public short OffsetX { get; internal set; }
        public short OffsetY { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} * {1} * {2}/ {3} ", Width, Height, Colors.Count, PaletteStart);
        }
    }
}