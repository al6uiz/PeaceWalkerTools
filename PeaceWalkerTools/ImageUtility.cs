using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PeaceWalkerTools
{
    class ImageUtility
    {
        private static int _currentPallete;

        #region 32/24 bit image to 8 bit index

        //code from http://www.codeproject.com/Articles/17162/Fast-Color-Depth-Change-for-Bitmaps

        // Stores known information


        private static Dictionary<int, byte> _knownColors = new Dictionary<int, byte>((int)Math.Pow(2, 20));

        /// <summary>
        /// Converts input bitmap to 8bpp format
        /// </summary>
        /// <param name="bmpSource" />Bitmap to convert</param />
        /// <returns>Converted bitmap</returns>
        public static Bitmap ConvertTo8bppFormat(Bitmap bmpSource)
        {
            _knownColors.Clear();

            var imageWidth = bmpSource.Width;
            var imageHeight = bmpSource.Height;

            Bitmap bmpDest = null;
            BitmapData bmpDataDest = null;
            BitmapData bmpDataSource = null;

            _currentPallete = 0;
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

                var pixelSize = GetPixelInfoSize(bmpDataSource.PixelFormat);
                var buffer = new byte[imageWidth * imageHeight * pixelSize];
                var destBuffer = new byte[imageWidth * imageHeight];

                // Read all data to buffer
                ReadBmpData(bmpDataSource, buffer, pixelSize, imageWidth, imageHeight);

                // Get color indexes
                MatchColors(buffer, destBuffer, pixelSize, bmpDest.Palette);

                // Copy all colors to destination bitmaps
                WriteBmpData(bmpDataDest, destBuffer, imageWidth, imageHeight);

                ColorPalette colPal = bmpDest.Palette;

                foreach (var entry in _knownColors)
                {
                    var col = Color.FromArgb((int)entry.Key);
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
        private static void ReadBmpData(BitmapData bmpDataSource, byte[] buffer, int pixelSize, int width, int height)
        {
            // Get unmanaged data start address
            var addrStart = bmpDataSource.Scan0.ToInt32();

            for (int i = 0; i < height; i++)
            {
                // Get address of next row
                var realByteAddr = new IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataSource.Stride));

                // Perform copy from unmanaged memory
                // to managed buffer
                Marshal.Copy(realByteAddr, buffer, (int)(i * width * pixelSize), (int)(width * pixelSize));
            }
        }

        /// <summary>
        /// Writes bitmap data to unmanaged memory
        /// </summary>
        private static void WriteBmpData(BitmapData bmpDataDest, byte[] destBuffer, int imageWidth, int imageHeight)
        {
            // Get unmanaged data start address
            var addrStart = bmpDataDest.Scan0.ToInt32();

            for (int i = 0; i < imageHeight; i++)
            {
                // Get address of next row
                var realByteAddr = new IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataDest.Stride));

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
        private static void MatchColors(byte[] buffer, byte[] destBuffer, int pixelSize, ColorPalette pallete)
        {
            int length = destBuffer.Length;

            // Temp storage for color info
            var temp = new byte[pixelSize];
            var palleteSize = pallete.Entries.Length;
            var currentKey = 0;

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
                    destBuffer[i] = (byte)_currentPallete; //GetSimilarColor(pallete, temp, palleteSize);
                    _knownColors.Add(currentKey, destBuffer[i]);
                    _currentPallete++;
                }
                else
                {
                    destBuffer[i] = (byte)_knownColors[currentKey];
                }
            }// for
        }

        internal static byte[] Crop(byte[] input, int stride, int x, int y, int width, int height)
        {
            width = (int)Math.Ceiling(width / 4.0) * 4;

            byte[] output = new byte[width * height];

            //for (int cy = 0; cy < height; cy++)
            //{
            //    for (int cx = 0; cx < width; cx++)
            //    {
            //        output[cy * width + cx] = input[stride * (y + cy) + x + cx];
            //    }
            //}

            //return output;



            unsafe
            {
                fixed (byte* pIn = input)
                fixed (byte* pOut = output)
                {
                    Crop(pIn, pOut, stride, x, y, width, height);
                }
            }

            return output;
        }

        private static unsafe void Crop(byte* pIn, byte* pOut, int stride, int x, int y, int w, int h)
        {
            byte* src = pIn;
            byte* dest = pOut;

            for (int cy = 0; cy < h; cy++)
            {
                src = pIn + stride * (y + cy) + x;
                dest = pOut + cy * w;
                for (int cx = 0; cx < w; cx++)
                {
                    *(dest++) = *(src++);
                }
            }
        }

        internal static byte[] Unswizzle(TextureFormat format, byte[] input, int width)
        {
            var height = (input.Length / width);
            byte[] output = new byte[width * height];

            unsafe
            {
                fixed (byte* pIn = input)
                fixed (byte* pOut = output)
                {
                    Unswizzle(format, pIn, pOut, (uint)width, (uint)height);
                }
            }

            return output;
        }

        private static unsafe byte* Unswizzle(
         TextureFormat format, byte* pin, byte* pout, uint width, uint height)
        {
            uint rowWidth;
            uint byc;
            if (format.Size == 0)
                rowWidth = (width / 2);
            else
                rowWidth = width * format.Size;

            uint pitch = (rowWidth - 16) / 4;
            uint bxc = rowWidth / 16;
            if (format.Size == 0)
            {
                byc = height / 4;
            }
            else
            {
                byc = height / 8;
            }


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


        /// <summary>
        /// Returns Similar color
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private byte GetSimilarColor(ColorPalette palette, byte[] color, int palleteSize)
        {
            var minDiff = byte.MaxValue;
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

        private static int GetPixelInfoSize(PixelFormat format)
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


        #region PSP_SWIZZLE





        public static byte[] Unswizzle(TextureFormat format, byte[] input, uint dataWidth, uint x, uint y, uint width, uint height)
        {
            byte[] output = new byte[input.Length];

            unsafe
            {
                fixed (byte* pIn = input)
                fixed (byte* pOut = output)
                {
                    Unswizzle(format, pIn, pOut, dataWidth, x, y, width, height);
                }
            }

            return output;
        }

        public static byte[] Swizzle(TextureFormat format, byte[] input, uint width, uint height)
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
            uint blockCountX = (rowWidth / 16);
            uint blockCountY = (height / 8);

            uint* dest = (uint*)pout;
            byte* ysrc = pin;

            for (int by = 0; by < blockCountY; by++)
            {
                byte* xsrc = ysrc;

                for (int bx = 0; bx < blockCountX; bx++)
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

        private static unsafe byte* Unswizzle(
            TextureFormat format, byte* pin, byte* pout,
            uint dataWidth, uint x, uint y, uint width, uint height)
        {
            uint rowWidth;
            if (format.Size == 0)
                rowWidth = (dataWidth / 2);
            else
                rowWidth = dataWidth * format.Size;

            uint pitch = (rowWidth - 16) / 4;
            uint bxc = width / 16;
            uint byc = height / 8;

            uint* src = (uint*)pin;
            byte* ydest = pout;

            var sy = y / 8;
            var sx = x / 16;

            for (int by = 0; by < byc; by++)
            {
                ydest = pout + by * rowWidth * 8;

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
            }
            return pout;
        }

        #endregion

    }

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
        public static TextureFormat[] Formats { get; private set; } = new TextureFormat[]{
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

        public TexturePixelStorage Format { get; private set; }
        public uint Size { get; private set; }
        public TextureFormat(TexturePixelStorage format, uint size)
        {
            Format = format;
            Size = size;
        }
    }
}
