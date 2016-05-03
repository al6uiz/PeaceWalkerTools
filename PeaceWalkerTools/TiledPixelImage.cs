using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools.Formats
{
    public class TiledPixelImage
    {
        public List<RGBA> Palette { get; private set; } = new List<RGBA>();
        public Image Image { get; private set; }
        public TextureFormat Format { get; private set; }

        private int curPal;
        public byte[] ImageData { get; private set; }

        #region PSP_SWIZZLE

        public enum TexturePixelStorage
        {
            BGR5650 = 0,
            ABGR5551 = 1,
            ABGR4444 = 2,
            ABGR8888 = 3,
            Indexed4 = 4,
            Indexed8 = 5,
            Indexed16 = 6,
            Indexed32 = 7,
            DXT1 = 8,
            DXT3 = 9,
            DXT5 = 10,
        }

        public class TextureFormat
        {
            public TexturePixelStorage Format { get; private set; }
            public uint Size { get; private set; }
            public TextureFormat(TexturePixelStorage format, uint size)
            {
                Format = format;
                Size = size;
            }
        }

        public TextureFormat[] TextureFormats = new TextureFormat[]{
            new TextureFormat( TexturePixelStorage.BGR5650,     2),
            new TextureFormat( TexturePixelStorage.ABGR5551,    2),
            new TextureFormat( TexturePixelStorage.ABGR4444,    2),
            new TextureFormat( TexturePixelStorage.ABGR8888,    4),
            new TextureFormat( TexturePixelStorage.Indexed4,    0),
            new TextureFormat( TexturePixelStorage.Indexed8,    1),
            new TextureFormat( TexturePixelStorage.Indexed16,   2),
            new TextureFormat( TexturePixelStorage.Indexed32,   4),
            new TextureFormat( TexturePixelStorage.DXT1,        4),
            new TextureFormat( TexturePixelStorage.DXT3,        4),
            new TextureFormat( TexturePixelStorage.DXT5,        4),
        };


        public byte[] Unswizzle(TextureFormat format, byte[] input, uint width, uint height)
        {
            byte[] output = new byte[input.Length];

            unsafe
            {
                fixed (byte* pIn = input)
                fixed (byte* pOut = output)
                {
                    Unswizzle(format, pIn, pOut, width, height);
                }
            }

            return output;
        }

        public byte[] Swizzle(TextureFormat format, byte[] input, uint width, uint height)
        {
            byte[] output = new byte[input.Length];

            unsafe
            {
                fixed (byte* pIn = input)
                fixed (byte* pOut = output)
                {
                    Swizzle(format, pIn, pOut, width, height);
                }
            }

            return output;
        }

        private static unsafe byte* Swizzle(TextureFormat format, byte* pin, byte* pout, uint width, uint height)
        {
            uint rowWidth;
            if (format.Size == 0)
                rowWidth = (width / 2);
            else
                rowWidth = width * format.Size;

            uint pitch = (rowWidth - 16) / 4;
            uint bxc = (rowWidth / 16);
            uint byc = (height / 8);

            uint* dest = (uint*)pout;
            byte* ysrc = pin;

            for (int by = 0; by < byc; by++)
            {
                byte* xsrc = ysrc;
                for (int bx = 0; bx < bxc; bx++)
                {
                    uint* src = (uint*)xsrc;
                    for (int j = 0; j < 8; ++j)
                    {
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        src += pitch;
                    }
                    xsrc += 16;
                }
                ysrc += rowWidth * 8;
            }
            return pout;
        }

        private static unsafe byte* Unswizzle(TextureFormat format, byte* pin, byte* pout, uint width, uint height)
        {
            uint rowWidth;
            if (format.Size == 0)
                rowWidth = (width / 2);
            else
                rowWidth = width * format.Size;

            uint pitch = (rowWidth - 16) / 4;
            uint bxc = rowWidth / 16;
            uint byc = height / 8;

            uint* src = (uint*)pin;
            byte* ydest = pout;

            for (int by = 0; by < byc; by++)
            {
                byte* xdest = ydest;
                for (int bx = 0; bx < bxc; bx++)
                {
                    uint* dest = (uint*)xdest;
                    for (int n = 0; n < 8; n++)
                    {
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        *(dest++) = *(src++);
                        dest += pitch;
                    }
                    xdest += 16;
                }
                ydest += rowWidth * 8;
            }
            return pout;
        }

        #endregion

        #region 32/24 bit image to 8 bit index

        //code from http://www.codeproject.com/Articles/17162/Fast-Color-Depth-Change-for-Bitmaps

        // Stores known information


        private Dictionary<int, byte> _knownColors = new Dictionary<int, byte>((int)Math.Pow(2, 20));

        /// <summary>
        /// Converts input bitmap to 8bpp format
        /// </summary>
        /// <param name="bmpSource" />Bitmap to convert</param />
        /// <returns>Converted bitmap</returns>
        public Bitmap ConvertTo8bppFormat(Bitmap bmpSource)
        {
            int imageWidth = bmpSource.Width;
            int imageHeight = bmpSource.Height;

            Bitmap bmpDest = null;
            BitmapData bmpDataDest = null;
            BitmapData bmpDataSource = null;

            this.curPal = 0;
            try
            {
                // Create new image with 8BPP format
                bmpDest = new Bitmap(
                    imageWidth,
                    imageHeight,
                    PixelFormat.Format8bppIndexed
                    );

                // Lock bitmap in memory
                bmpDataDest = bmpDest.LockBits(
                    new Rectangle(0, 0, imageWidth, imageHeight),
                    ImageLockMode.ReadWrite,
                    bmpDest.PixelFormat
                    );

                bmpDataSource = bmpSource.LockBits(
                    new Rectangle(0, 0, imageWidth, imageHeight),
                    ImageLockMode.ReadOnly,
                    bmpSource.PixelFormat
                );

                int pixelSize = GetPixelInfoSize(bmpDataSource.PixelFormat);
                byte[] buffer = new byte[imageWidth * imageHeight * pixelSize];
                byte[] destBuffer = new byte[imageWidth * imageHeight];

                // Read all data to buffer
                ReadBmpData(bmpDataSource, buffer, pixelSize, imageWidth, imageHeight);

                // Get color indexes
                MatchColors(buffer, destBuffer, pixelSize, bmpDest.Palette);

                // Copy all colors to destination bitmaps
                WriteBmpData(bmpDataDest, destBuffer, imageWidth, imageHeight);

                ColorPalette colPal = bmpDest.Palette;

                foreach (var entry in _knownColors)
                {
                    Color col = Color.FromArgb((int)entry.Key);
                    int index = (byte)entry.Value;

                    colPal.Entries[index] = col;
                }
                bmpDest.Palette = colPal;

                return bmpDest;
            }
            finally
            {
                if (bmpDest != null)
                    bmpDest.UnlockBits(bmpDataDest);
                if (bmpSource != null)
                    bmpSource.UnlockBits(bmpDataSource);
            }
        }

        /// <summary>
        /// Reads all bitmap data at once
        /// </summary>
        private void ReadBmpData(BitmapData bmpDataSource, byte[] buffer, int pixelSize, int width, int height)
        {
            // Get unmanaged data start address
            int addrStart = bmpDataSource.Scan0.ToInt32();

            for (int i = 0; i < height; i++)
            {
                // Get address of next row
                IntPtr realByteAddr = new IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataSource.Stride));

                // Perform copy from unmanaged memory
                // to managed buffer
                Marshal.Copy(realByteAddr, buffer, (int)(i * width * pixelSize), (int)(width * pixelSize));
            }
        }

        /// <summary>
        /// Writes bitmap data to unmanaged memory
        /// </summary>
        private void WriteBmpData(BitmapData bmpDataDest, byte[] destBuffer, int imageWidth, int imageHeight)
        {
            // Get unmanaged data start address
            int addrStart = bmpDataDest.Scan0.ToInt32();

            for (int i = 0; i < imageHeight; i++)
            {
                // Get address of next row
                IntPtr realByteAddr = new IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataDest.Stride));

                // Perform copy from managed buffer
                // to unmanaged memory
                Marshal.Copy(destBuffer, i * imageWidth, realByteAddr, imageWidth);
            }
        }

        /// <summary>
        /// This method matches indices from pallete ( 256 colors )
        /// for each given 24bit color
        /// </summary>
        /// <param name="buffer">Buffer that contains color for each pixel</param>
        /// <param name="destBuffer">Destination buffer that will contain index 
        /// for each color</param>
        /// <param name="pixelSize">Size of pixel info ( 24bit colors supported )</param>
        /// <param name="pallete">Colors pallete ( 256 colors )</param>
        private void MatchColors(byte[] buffer, byte[] destBuffer, int pixelSize, ColorPalette pallete)
        {
            int length = destBuffer.Length;

            // Temp storage for color info
            byte[] temp = new byte[pixelSize];
            int palleteSize = pallete.Entries.Length;
            int currentKey = 0;

            // For each color
            for (int i = 0; i < length; i++)
            {
                // Get next color
                Array.Copy(buffer, i * pixelSize, temp, 0, pixelSize);

                // Build key for hash table
                if (pixelSize == 3)
                    currentKey = temp[0] + (temp[1] << 8) + (temp[2] << 16);
                else if (pixelSize == 4)
                    currentKey = temp[0] + (temp[1] << 8) + (temp[2] << 16) + (temp[3] << 24);

                // If hash table already contains such color - fetch it
                // Otherwise perform calculation of similar color and save it to HT
                if (!_knownColors.ContainsKey(currentKey))
                {
                    destBuffer[i] = (byte)curPal; //GetSimilarColor(pallete, temp, palleteSize);
                    _knownColors.Add(currentKey, destBuffer[i]);
                    curPal++;
                }
                else
                {
                    destBuffer[i] = (byte)_knownColors[currentKey];
                }
            }// for
        }

        /// <summary>
        /// Returns Similar color
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private byte GetSimilarColor(ColorPalette palette, byte[] color, int palleteSize)
        {
            byte minDiff = byte.MaxValue;
            byte index = 0;

            if (color.Length == 3)// Implemented for 24bpp color
            {
                // Loop all pallete ( 256 colors )
                for (int i = 0; i < palleteSize - 1; i++)
                {
                    // Calculate similar color
                    byte currentDiff = GetMaxDiff(color, palette.Entries[i], color.Length);

                    if (currentDiff < minDiff)
                    {
                        minDiff = currentDiff;
                        index = (byte)i;
                    }
                }// for
            }
            else if (color.Length == 4)// Implemented for 32bpp color
            {
                // Loop all pallete ( 256 colors )
                for (int i = 0; i < palleteSize - 1; i++)
                {
                    // Calculate similar color
                    byte currentDiff = GetMaxDiff(color, palette.Entries[i], color.Length);

                    if (currentDiff < minDiff)
                    {
                        minDiff = currentDiff;
                        index = (byte)i;
                    }
                }// for
            }
            else// TODO implement it for other color types
            {
                throw new ApplicationException("Only 24bit colors supported now");
            }

            return index;
        }

        /// <summary>
        /// Return similar color
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static byte GetMaxDiff(byte[] a, Color b, int pixelSize)
        {
            // Get difference between components ( red green blue )
            // of given color and appropriate components of pallete color
            byte bDiff = a[0] > b.B ? (byte)(a[0] - b.B) : (byte)(b.B - a[0]);
            byte gDiff = a[1] > b.G ? (byte)(a[1] - b.G) : (byte)(b.G - a[1]);
            byte rDiff = a[2] > b.R ? (byte)(a[2] - b.R) : (byte)(b.R - a[2]);
            byte aDiff = a[3] > b.A ? (byte)(a[3] - b.A) : (byte)(b.A - a[3]);
            byte max = 0;

            // Get max difference
            if (pixelSize == 3)
            {
                max = bDiff > gDiff ? bDiff : gDiff;
                max = max > rDiff ? max : rDiff;
            }
            else if (pixelSize == 4)
            {
                max = bDiff > gDiff ? bDiff : gDiff;
                max = max > rDiff ? max : rDiff;
                max = max > aDiff ? max : aDiff;
            }
            return max;
        }

        private int GetPixelInfoSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                return 3;
                case PixelFormat.Format32bppArgb:
                return 4;

                default:
                throw new ApplicationException("Only 24bit colors supported now");
            }

        }

        #endregion

        public void LoadTXP(string sPath, bool bMakeImage = true)
        {
            if (File.Exists(sPath))
            {
                Image = null;
                ImageData = null;
                Palette = new List<RGBA>();
                List<byte> imageData = new List<byte>();
                byte index;

                using (BinaryReader br = new BinaryReader(File.OpenRead(sPath)))
                {
                    this.Width = br.ReadInt16();
                    this.Height = br.ReadInt16();
                    this.Colors = br.ReadInt16();
                    this.unk1 = br.ReadByte();
                    this.unk2 = br.ReadByte();
                    this.Colors2 = br.ReadInt16();
                    this.Colors3 = br.ReadInt16();
                    this.unk3 = br.ReadInt16();
                    this.unk4 = br.ReadInt16();

                    for (int i = 0; i < this.Colors * this.Colors3; i++)
                    {
                        Palette.Add(new RGBA
                        {
                            r = br.ReadByte(),
                            g = br.ReadByte(),
                            b = br.ReadByte(),
                            a = br.ReadByte()
                        });
                    }

                    if (this.Colors == 16)
                    {
                        ImageData = br.ReadBytes((this.Width * this.Height / 2));
                        Format = TextureFormats[(int)TexturePixelStorage.Indexed4];

                        if (EnableSwizzle == true)
                            ImageData = Unswizzle(Format, ImageData, (uint)this.Width, (uint)this.Height);

                        for (int i = 0; i < (this.Width * this.Height / 2); i++) // 4bpp to 8bpp
                        {
                            index = ImageData[i];
                            imageData.Add((byte)(index & 0x0F));
                            imageData.Add((byte)((index & 0xF0) >> 4));
                        }

                        ImageData = imageData.ToArray();
                        imageData = null;
                    }
                    else if (this.Colors == 256)
                    {
                        ImageData = br.ReadBytes(this.Width * this.Height);
                        Format = TextureFormats[(int)TexturePixelStorage.Indexed8];

                        if (EnableSwizzle == true)
                            ImageData = Unswizzle(Format, ImageData, (uint)this.Width, (uint)this.Height);
                    }
                    else
                    {
                        Image = null;
                        return;
                    }

                    if (bMakeImage)
                        MakeImage();
                }//end using BinaryReader
            }//end if
        }

        private void MakeImage()
        {
            var bmp = new Bitmap(this.Width, this.Height);
            Color col;
            byte index;

            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    index = ImageData[x + (y * this.Width)];

                    if (EnableAlpha)
                    {
                        col = Color.FromArgb(Palette[index].a, Palette[index].r, Palette[index].g, Palette[index].b);
                    }
                    else
                    {
                        col = Color.FromArgb(Palette[index].r, Palette[index].g, Palette[index].b);
                    }

                    bmp.SetPixel(x, y, col);
                }
            }
            Image = bmp;
        }

        public byte[] SaveTXP(Image img)
        {
            byte[] output = null;
            Bitmap bmp = ConvertTo8bppFormat((Bitmap)img);
            int maxcolor = 0;

            if (bmp != null)
            {
                this.Width = (short)bmp.Width;
                this.Height = (short)bmp.Height;
                this.unk1 = 0;
                this.unk2 = 0;
                this.unk3 = 1;
                this.unk4 = 1;
                List<byte> imageData = new List<byte>();
                byte b1, b2;

                output = new byte[this.Width * bmp.Height];

                for (int y = 0; y < this.Height; y++)
                {
                    for (int x = 0; x < this.Width; x++)
                    {
                        Color col = bmp.GetPixel(x, y);
                        byte bIndex = GetColorIndex(col, bmp.Palette);

                        if (bIndex > maxcolor)
                            maxcolor = bIndex;


                        output[x + (y * this.Width)] = bIndex;
                    }
                }

                if (maxcolor <= 16)
                {
                    this.Colors = 16;
                    this.Colors2 = 16;
                    this.Colors3 = 16;
                    this.Format = TextureFormats[(int)TexturePixelStorage.Indexed4];

                    for (int i = 0; i < this.Width * this.Height; i++) // 8bpp to 4bpp
                    {
                        b1 = output[i++];
                        b2 = output[i];
                        imageData.Add((byte)((b1 & 0xF) + ((b2 & 0xF) << 4)));
                    }
                    output = imageData.ToArray();
                }
                else if (maxcolor <= 256)
                {
                    this.Colors = 256;
                    this.Colors2 = 256;
                    this.Colors3 = 1;
                    this.Format = TextureFormats[(int)TexturePixelStorage.Indexed8];
                }
                else
                {
                    return null;
                }

                if (EnableSwizzle == true)
                    output = Swizzle(this.Format, output, (uint)this.Width, (uint)this.Height);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(this.Width);
                        bw.Write(this.Height);
                        bw.Write(this.Colors);
                        bw.Write(this.unk1);
                        bw.Write(this.unk2);
                        bw.Write(this.Colors2);
                        bw.Write(this.Colors3);
                        bw.Write(this.unk3);
                        bw.Write(this.unk4);

                        for (int i = 0; i < this.Colors * this.Colors3; i++)
                        {
                            bw.Write(bmp.Palette.Entries[i].R);
                            bw.Write(bmp.Palette.Entries[i].G);
                            bw.Write(bmp.Palette.Entries[i].B);
                            bw.Write(bmp.Palette.Entries[i].A);
                        }

                        bw.Write(output);
                    }//using bw
                    output = ms.ToArray();
                }//using ms
            }

            return output;
        }

        private byte GetColorIndex(Color col, ColorPalette pal)
        {
            byte result = 0;
            for (byte i = 0; i < pal.Entries.Length; i++)
            {
                if (col == pal.Entries[i])
                {
                    return i;
                }
            }
            return result;
        }

        public void ExportBMP(string infile, string outfile)
        {
            if (File.Exists(infile))
            {
                LoadTXP(infile, false);
                tools.DeleteFileIfExists(outfile);

                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(outfile)))
                {
                    //Bitmap file header
                    bw.Write((byte)0x42); //B
                    bw.Write((byte)0x4D); //M
                    bw.Write((Width * Height) + 1024 + 54); //filesize - width*height + palette + header
                    bw.Write((int)0); // reserved 1&2
                    bw.Write((int)1078); //OffBits

                    //Bitmap info header
                    bw.Write((int)40); // size of header
                    bw.Write((int)Width);
                    bw.Write((int)Height);
                    bw.Write((short)1); //Planes
                    bw.Write((short)8); //Bitcount
                    bw.Write((int)0); //compression
                    bw.Write(Width * Height); // size of image
                    bw.Write((int)0); //x
                    bw.Write((int)0); //y
                    bw.Write((int)0); //used
                    bw.Write((int)0); //important

                    //palette, Note: may need to change this into a fixed palette
                    for (int i = 0; i < this.Palette.Count; i++)
                    {
                        bw.Write(this.Palette[i].r);
                        bw.Write(this.Palette[i].g);
                        bw.Write(this.Palette[i].b);
                        bw.Write(this.Palette[i].a);
                    }

                    //image data, reversed
                    Array.Reverse(this.ImageData);
                    bw.Write(this.ImageData);
                    Array.Reverse(this.ImageData);
                }
            }
        }

        public void ImportBMP(string infile, string outfile)
        {
            if (File.Exists(infile))
            {
                List<byte> imageData = new List<byte>();

                // read bmp
                using (var br = new BinaryReader(File.OpenRead(infile)))
                {
                    //skip header
                    br.BaseStream.Seek(0xE, SeekOrigin.Begin);

                    if (br.ReadInt32() == 0x28)
                    {
                        this.Width = (short)br.ReadInt32();
                        this.Height = (short)br.ReadInt32();
                        br.ReadInt16(); //planes
                        int bitcount = br.ReadInt16();

                        //skip rest of info header
                        br.BaseStream.Seek(0x36, SeekOrigin.Begin);

                        if (bitcount == 8) // indexed 8
                        {
                            Palette = new List<RGBA>();
                            Format = this.TextureFormats[(int)TexturePixelStorage.Indexed8];

                            for (int i = 0; i < 256; i++)
                            {
                                Palette.Add(new RGBA()
                                {
                                    r = br.ReadByte(),
                                    g = br.ReadByte(),
                                    b = br.ReadByte(),
                                    a = br.ReadByte()
                                });
                            }
                        }
                        else
                        {
                            this.ImageData = null;
                            return;
                        }

                        this.ImageData = br.ReadBytes(this.Width * this.Height * (bitcount / 8));
                        Array.Reverse(this.ImageData);

                        //convert 8bpp to 4bpp ?
                        for (int i = 0; i < this.Width * this.Height * (bitcount / 8); i++)
                        {
                            imageData.Add((byte)((this.ImageData[i++] & 0xF) + ((this.ImageData[i] & 0xF) << 4)));
                        }
                        Format = this.TextureFormats[(int)TexturePixelStorage.Indexed4];


                        //swizzle image
                        this.ImageData = this.Swizzle(this.TextureFormats[(int)TexturePixelStorage.Indexed4],
                            imageData.ToArray(), (uint)this.Width, (uint)this.Height);

                        imageData = null;


                    }
                }//using BinaryReader

                //write txp
                FileUtility.DeleteFileIfExists(outfile);

                this.Colors = 16;
                this.Colors2 = 16;
                this.Colors3 = 16;
                this.unk1 = 0;
                this.unk2 = 0;
                this.unk3 = 1;
                this.unk4 = 1;
                using (var bw = new BinaryWriter(File.OpenWrite(outfile)))
                {
                    bw.Write(this.Width);
                    bw.Write(this.Height);
                    bw.Write(this.Colors);
                    bw.Write(this.unk1);
                    bw.Write(this.unk2);
                    bw.Write(this.Colors2);
                    bw.Write(this.Colors3);
                    bw.Write(this.unk3);
                    bw.Write(this.unk4);

                    for (int i = 0; i < this.Colors * this.Colors3; i++)
                    {
                        bw.Write(Palette[i].r);
                        bw.Write(Palette[i].g);
                        bw.Write(Palette[i].b);
                        bw.Write(Palette[i].a);
                    }

                    bw.Write(this.ImageData);
                }
            }
        }

        public short Width { get; set; }
        public short Height { get; set; }
        public short Colors { get; set; }
        public byte unk1 { get; set; }
        public byte unk2 { get; set; }
        public short Colors2 { get; set; }
        public short Colors3 { get; set; }
        public short unk3 { get; set; }
        public short unk4 { get; set; }

        public bool EnableSwizzle { get; set; }
        public bool EnableAlpha { get; set; }
        
    }

    public class RGBA
    {
        public RGBA()
        {

        }

        public byte r { get; set; }
        public byte g { get; set; }
        public byte b { get; set; }
        public byte a { get; set; }
    }
}
