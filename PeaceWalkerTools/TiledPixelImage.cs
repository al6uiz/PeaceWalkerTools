//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Runtime.InteropServices;

//namespace PeaceWalkerTools
//{
//    public partial class TiledPixelImage
//    {
//        public List<Color> Palette { get; private set; }
//        public Image Image { get; private set; }
//        public TextureFormat Format { get; private set; }

//        public byte[] ImageData { get; private set; }

//        internal static Bitmap Convert(TxpEntity pixel, TxpEntitySub sub)
//        {
//            var txp = new TiledPixelImage
//            {
//                Width = sub.Width,
//                Height = sub.Height,
//                Palette = sub.Colors,
//                Colors = sub.Colors.Count
//            };

//            if (txp.Colors == 16)
//            {
//                txp.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];
//            }
//            else if (txp.Colors == 256)
//            {
//                txp.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];
//            }

//            var x = (uint)(sub.X * pixel.Width);
//            var y = (uint)(sub.Y * pixel.Height);

//            var unswizzled = ImageUtility.Unswizzle(txp.Format, pixel.RawData, (uint)pixel.Width, x, y, (uint)txp.Width, (uint)txp.Height);

//            if (txp.Colors == 16)
//            {
//                var total = (txp.Width * txp.Height / 2);
//                var list = new List<byte>(total);
//                for (int i = 0; i < total; i++) // 4bpp to 8bpp
//                {
//                    var index = unswizzled[i];
//                    list.Add((byte)(index & 0x0F));
//                    list.Add((byte)((index & 0xF0) >> 4));
//                }

//                txp.ImageData = list.ToArray();
//            }
//            else if (txp.Colors == 256)
//            {
//                txp.ImageData = unswizzled;

//            }

//            return txp.GetImage();
//        }

//        internal Bitmap GetImage()
//        {
//            var bitmap = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
//            var palette = bitmap.Palette;
//            for (int j = 0; j < Palette.Count; j++)
//            {
//                palette.Entries[j] = Palette[j];
//            }
//            bitmap.Palette = palette;

//            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
//            Marshal.Copy(ImageData, 0, bmpData.Scan0, Width * Height);
//            bitmap.UnlockBits(bmpData);

//            return bitmap;
//        }
//        private void MakeImage()
//        {
//            var bmp = new Bitmap(this.Width, this.Height);
//            Color col;
//            byte index;

//            for (int y = 0; y < this.Height; y++)
//            {
//                for (int x = 0; x < this.Width; x++)
//                {
//                    index = ImageData[x + (y * this.Width)];

//                    col = Palette[index];
//                    bmp.SetPixel(x, y, col);
//                }
//            }
//            Image = bmp;
//        }

//        public byte[] SaveTXP(Image img)
//        {
//            byte[] output = null;
//            var bmp = ImageUtility.ConvertTo8bppFormat((Bitmap)img);
//            int maxcolor = 0;

//            if (bmp != null)
//            {
//                this.Width = (short)bmp.Width;
//                this.Height = (short)bmp.Height;

//                List<byte> imageData = new List<byte>();
//                byte b1, b2;

//                output = new byte[this.Width * bmp.Height];

//                for (int y = 0; y < this.Height; y++)
//                {
//                    for (int x = 0; x < this.Width; x++)
//                    {
//                        Color col = bmp.GetPixel(x, y);
//                        byte bIndex = GetColorIndex(col, bmp.Palette);

//                        if (bIndex > maxcolor)
//                            maxcolor = bIndex;


//                        output[x + (y * this.Width)] = bIndex;
//                    }
//                }

//                if (maxcolor <= 16)
//                {
//                    this.Colors = 16;
//                    this.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];

//                    for (int i = 0; i < this.Width * this.Height; i++) // 8bpp to 4bpp
//                    {
//                        b1 = output[i++];
//                        b2 = output[i];
//                        imageData.Add((byte)((b1 & 0xF) + ((b2 & 0xF) << 4)));
//                    }
//                    output = imageData.ToArray();
//                }
//                else if (maxcolor <= 256)
//                {
//                    this.Colors = 256;
//                    this.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];
//                }
//                else
//                {
//                    return null;
//                }

//                ImageData = ImageUtility.Swizzle(this.Format, output, (uint)this.Width, (uint)this.Height);
//            }

//            return output;
//        }

//        private byte GetColorIndex(Color color, ColorPalette pallete)
//        {
//            byte result = 0;
//            for (byte i = 0; i < pallete.Entries.Length; i++)
//            {
//                if (color == pallete.Entries[i])
//                {
//                    return i;
//                }
//            }
//            return result;
//        }

//        //public void ImportBMP(string infile, string outfile)
//        //{
//        //    if (File.Exists(infile))
//        //    {
//        //        List<byte> imageData = new List<byte>();

//        //        // read bmp
//        //        using (var br = new BinaryReader(File.OpenRead(infile)))
//        //        {
//        //            //skip header
//        //            br.BaseStream.Seek(0xE, SeekOrigin.Begin);

//        //            if (br.ReadInt32() == 0x28)
//        //            {
//        //                this.Width = (short)br.ReadInt32();
//        //                this.Height = (short)br.ReadInt32();
//        //                br.ReadInt16(); //planes
//        //                int bitcount = br.ReadInt16();

//        //                //skip rest of info header
//        //                br.BaseStream.Seek(0x36, SeekOrigin.Begin);

//        //                if (bitcount == 8) // indexed 8
//        //                {
//        //                    Palette = new List<RGBA>();
//        //                    Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];

//        //                    for (int i = 0; i < 256; i++)
//        //                    {
//        //                        Palette.Add(new RGBA()
//        //                        {
//        //                            r = br.ReadByte(),
//        //                            g = br.ReadByte(),
//        //                            b = br.ReadByte(),
//        //                            a = br.ReadByte()
//        //                        });
//        //                    }
//        //                }
//        //                else
//        //                {
//        //                    this.ImageData = null;
//        //                    return;
//        //                }

//        //                this.ImageData = br.ReadBytes(this.Width * this.Height * (bitcount / 8));
//        //                Array.Reverse(this.ImageData);

//        //                //convert 8bpp to 4bpp ?
//        //                for (int i = 0; i < this.Width * this.Height * (bitcount / 8); i++)
//        //                {
//        //                    imageData.Add((byte)((this.ImageData[i++] & 0xF) + ((this.ImageData[i] & 0xF) << 4)));
//        //                }
//        //                Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];


//        //                //swizzle image
//        //                this.ImageData = ImageUtility.Swizzle(TextureFormat.Formats[(int)TexturePixelStorage.Indexed4],
//        //                    imageData.ToArray(), (uint)this.Width, (uint)this.Height);

//        //                imageData = null;


//        //            }
//        //        }//using BinaryReader

//        //        //write txp
//        //        FileUtility.DeleteFileIfExists(outfile);

//        //        this.Colors = 16;


//        //    }
//        //}

//        public int Width { get; set; }
//        public int Height { get; set; }
//        public int Colors { get; set; }

//    }

//    public class RGBA
//    {
//        public RGBA()
//        {

//        }

//        public byte r { get; set; }
//        public byte g { get; set; }
//        public byte b { get; set; }
//        public byte a { get; set; }
//    }
//}
