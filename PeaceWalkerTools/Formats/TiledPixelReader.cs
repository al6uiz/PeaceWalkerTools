using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class TiledPixelReader
    {
        public byte[] Data { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Color> Colors { get; private set; }
        public TiledPixelReader(byte[] data, int width, int height, List<Color> colors)
        {
            Data = data;
            Width = width;
            Height = height;
            Colors = colors;
        }

        public Bitmap GetImage()
        {
            var dataWidth = Math.Max(16, (int)Math.Pow(2, Math.Ceiling(Math.Log(Width, 2))));


            var length = (int)(Width * Math.Ceiling(Height / 8.0) * 8);
            byte[] pixelData;

            switch (Colors.Count)
            {
                case 0x10: length /= 2; break;
                case 0x100: break;

                default:
                    break;
            }

            pixelData = new byte[Width * Height];

            var dataPerTile = 16 * 8;
            var tilesPerRow = Math.Max(1, dataWidth / (Colors.Count == 16 ? 32 : 16));

            //Debug.WriteLine(string.Format("{0} * {1} * {2}", Width, Height, Colors.Count));

            for (int i = 0; i < Data.Length; i++)
            {
                var colorIndex = Data[i];
                var tileIndex = i / dataPerTile;
                var remainder = i % dataPerTile;
                var rowOnTile = remainder / 16;
                var columnOnTile = remainder % 16;

                var tileRow = tileIndex / tilesPerRow;
                var tileColumn = tileIndex % tilesPerRow;

                var x = (tileColumn * 16 + columnOnTile) * (Colors.Count == 16 ? 2 : 1);
                var y = tileRow * 8 + rowOnTile;

                if (x >= Width || y >= Height)
                {
                    continue;
                }
                //Debug.WriteLine("[{0:X2}]{1},{2} ({3:X2}:{4:X2},{5:X2})", i, x, y, tileIndex, tileRow, tileColumn);

                var pIndex = (y * Width) + x;

                if (Colors.Count == 16)
                {
                    pixelData[pIndex] = (byte)(colorIndex & 0xF);
                    if (pIndex + 1 < pixelData.Length)
                    {
                        pixelData[pIndex + 1] = (byte)(colorIndex >> 4);
                    }
                }
                else
                {
                    pixelData[pIndex] = colorIndex;
                }
            }



            var bitmap = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
            var palette = bitmap.Palette;
            for (int j = 0; j < Colors.Count; j++)
            {
                palette.Entries[j] = Colors[j];
            }
            bitmap.Palette = palette;

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
    }
}
