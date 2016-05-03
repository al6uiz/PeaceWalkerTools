using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    partial class TiledPixelImage
    {

        //public void LoadTXP(string sPath, bool bMakeImage = true)
        //{
        //    if (File.Exists(sPath))
        //    {
        //        Image = null;
        //        ImageData = null;
        //        Palette = new List<RGBA>();
        //        List<byte> imageData = new List<byte>();
        //        byte index;

        //        using (BinaryReader br = new BinaryReader(File.OpenRead(sPath)))
        //        {
        //            this.Width = br.ReadInt16();
        //            this.Height = br.ReadInt16();
        //            this.Colors = br.ReadInt16();
        //            this.unk1 = br.ReadByte();
        //            this.unk2 = br.ReadByte();
        //            this.Colors2 = br.ReadInt16();
        //            this.Colors3 = br.ReadInt16();
        //            this.unk3 = br.ReadInt16();
        //            this.unk4 = br.ReadInt16();

        //            for (int i = 0; i < this.Colors * this.Colors3; i++)
        //            {
        //                Palette.Add(new RGBA
        //                {
        //                    r = br.ReadByte(),
        //                    g = br.ReadByte(),
        //                    b = br.ReadByte(),
        //                    a = br.ReadByte()
        //                });
        //            }

        //            if (this.Colors == 16)
        //            {
        //                ImageData = br.ReadBytes((this.Width * this.Height / 2));
        //                Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];

        //                if (EnableSwizzle == true)
        //                    ImageData = ImageUtility.Unswizzle(Format, ImageData, (uint)this.Width, (uint)this.Height);

        //                for (int i = 0; i < (this.Width * this.Height / 2); i++) // 4bpp to 8bpp
        //                {
        //                    index = ImageData[i];
        //                    imageData.Add((byte)(index & 0x0F));
        //                    imageData.Add((byte)((index & 0xF0) >> 4));
        //                }

        //                ImageData = imageData.ToArray();
        //                imageData = null;
        //            }
        //            else if (this.Colors == 256)
        //            {
        //                ImageData = br.ReadBytes(this.Width * this.Height);
        //                Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];

        //                if (EnableSwizzle == true)
        //                    ImageData = ImageUtility.Unswizzle(Format, ImageData, (uint)this.Width, (uint)this.Height);
        //            }
        //            else
        //            {
        //                Image = null;
        //                return;
        //            }

        //            if (bMakeImage)
        //                MakeImage();
        //        }//end using BinaryReader
        //    }//end if
        //}

        //private void MakeImage()
        //{
        //    var bmp = new Bitmap(this.Width, this.Height);
        //    Color col;
        //    byte index;

        //    for (int y = 0; y < this.Height; y++)
        //    {
        //        for (int x = 0; x < this.Width; x++)
        //        {
        //            index = ImageData[x + (y * this.Width)];

        //            if (EnableAlpha)
        //            {
        //                col = Color.FromArgb(Palette[index].a, Palette[index].r, Palette[index].g, Palette[index].b);
        //            }
        //            else
        //            {
        //                col = Color.FromArgb(Palette[index].r, Palette[index].g, Palette[index].b);
        //            }

        //            bmp.SetPixel(x, y, col);
        //        }
        //    }
        //    Image = bmp;
        //}

        //public byte[] SaveTXP(Image img)
        //{
        //    byte[] output = null;
        //    var bmp = ImageUtility.ConvertTo8bppFormat((Bitmap)img);
        //    int maxcolor = 0;

        //    if (bmp != null)
        //    {
        //        this.Width = (short)bmp.Width;
        //        this.Height = (short)bmp.Height;
        //        this.unk1 = 0;
        //        this.unk2 = 0;
        //        this.unk3 = 1;
        //        this.unk4 = 1;
        //        List<byte> imageData = new List<byte>();
        //        byte b1, b2;

        //        output = new byte[this.Width * bmp.Height];

        //        for (int y = 0; y < this.Height; y++)
        //        {
        //            for (int x = 0; x < this.Width; x++)
        //            {
        //                Color col = bmp.GetPixel(x, y);
        //                byte bIndex = GetColorIndex(col, bmp.Palette);

        //                if (bIndex > maxcolor)
        //                    maxcolor = bIndex;


        //                output[x + (y * this.Width)] = bIndex;
        //            }
        //        }

        //        if (maxcolor <= 16)
        //        {
        //            this.Colors = 16;
        //            this.Colors2 = 16;
        //            this.Colors3 = 16;
        //            this.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];

        //            for (int i = 0; i < this.Width * this.Height; i++) // 8bpp to 4bpp
        //            {
        //                b1 = output[i++];
        //                b2 = output[i];
        //                imageData.Add((byte)((b1 & 0xF) + ((b2 & 0xF) << 4)));
        //            }
        //            output = imageData.ToArray();
        //        }
        //        else if (maxcolor <= 256)
        //        {
        //            this.Colors = 256;
        //            this.Colors2 = 256;
        //            this.Colors3 = 1;
        //            this.Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];
        //        }
        //        else
        //        {
        //            return null;
        //        }

        //        if (EnableSwizzle == true)
        //            output = ImageUtility.Swizzle(this.Format, output, (uint)this.Width, (uint)this.Height);

        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (BinaryWriter bw = new BinaryWriter(ms))
        //            {
        //                bw.Write(this.Width);
        //                bw.Write(this.Height);
        //                bw.Write(this.Colors);
        //                bw.Write(this.unk1);
        //                bw.Write(this.unk2);
        //                bw.Write(this.Colors2);
        //                bw.Write(this.Colors3);
        //                bw.Write(this.unk3);
        //                bw.Write(this.unk4);

        //                for (int i = 0; i < this.Colors * this.Colors3; i++)
        //                {
        //                    bw.Write(bmp.Palette.Entries[i].R);
        //                    bw.Write(bmp.Palette.Entries[i].G);
        //                    bw.Write(bmp.Palette.Entries[i].B);
        //                    bw.Write(bmp.Palette.Entries[i].A);
        //                }

        //                bw.Write(output);
        //            }//using bw
        //            output = ms.ToArray();
        //        }//using ms
        //    }

        //    return output;
        //}

        //private byte GetColorIndex(Color color, ColorPalette pallete)
        //{
        //    byte result = 0;
        //    for (byte i = 0; i < pallete.Entries.Length; i++)
        //    {
        //        if (color == pallete.Entries[i])
        //        {
        //            return i;
        //        }
        //    }
        //    return result;
        //}

        //public void ExportBMP(string infile, string outfile)
        //{
        //    if (File.Exists(infile))
        //    {
        //        LoadTXP(infile, false);
        //        FileUtility.DeleteFileIfExists(outfile);

        //        using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(outfile)))
        //        {
        //            //Bitmap file header
        //            bw.Write((byte)0x42); //B
        //            bw.Write((byte)0x4D); //M
        //            bw.Write((Width * Height) + 1024 + 54); //filesize - width*height + palette + header
        //            bw.Write((int)0); // reserved 1&2
        //            bw.Write((int)1078); //OffBits

        //            //Bitmap info header
        //            bw.Write((int)40); // size of header
        //            bw.Write((int)Width);
        //            bw.Write((int)Height);
        //            bw.Write((short)1); //Planes
        //            bw.Write((short)8); //Bitcount
        //            bw.Write((int)0); //compression
        //            bw.Write(Width * Height); // size of image
        //            bw.Write((int)0); //x
        //            bw.Write((int)0); //y
        //            bw.Write((int)0); //used
        //            bw.Write((int)0); //important

        //            //palette, Note: may need to change this into a fixed palette
        //            for (int i = 0; i < this.Palette.Count; i++)
        //            {
        //                bw.Write(this.Palette[i].r);
        //                bw.Write(this.Palette[i].g);
        //                bw.Write(this.Palette[i].b);
        //                bw.Write(this.Palette[i].a);
        //            }

        //            //image data, reversed
        //            Array.Reverse(this.ImageData);
        //            bw.Write(this.ImageData);
        //            Array.Reverse(this.ImageData);
        //        }
        //    }
        //}

        //public void ImportBMP(string infile, string outfile)
        //{
        //    if (File.Exists(infile))
        //    {
        //        List<byte> imageData = new List<byte>();

        //        // read bmp
        //        using (var br = new BinaryReader(File.OpenRead(infile)))
        //        {
        //            //skip header
        //            br.BaseStream.Seek(0xE, SeekOrigin.Begin);

        //            if (br.ReadInt32() == 0x28)
        //            {
        //                this.Width = (short)br.ReadInt32();
        //                this.Height = (short)br.ReadInt32();
        //                br.ReadInt16(); //planes
        //                int bitcount = br.ReadInt16();

        //                //skip rest of info header
        //                br.BaseStream.Seek(0x36, SeekOrigin.Begin);

        //                if (bitcount == 8) // indexed 8
        //                {
        //                    Palette = new List<RGBA>();
        //                    Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed8];

        //                    for (int i = 0; i < 256; i++)
        //                    {
        //                        Palette.Add(new RGBA()
        //                        {
        //                            r = br.ReadByte(),
        //                            g = br.ReadByte(),
        //                            b = br.ReadByte(),
        //                            a = br.ReadByte()
        //                        });
        //                    }
        //                }
        //                else
        //                {
        //                    this.ImageData = null;
        //                    return;
        //                }

        //                this.ImageData = br.ReadBytes(this.Width * this.Height * (bitcount / 8));
        //                Array.Reverse(this.ImageData);

        //                //convert 8bpp to 4bpp ?
        //                for (int i = 0; i < this.Width * this.Height * (bitcount / 8); i++)
        //                {
        //                    imageData.Add((byte)((this.ImageData[i++] & 0xF) + ((this.ImageData[i] & 0xF) << 4)));
        //                }
        //                Format = TextureFormat.Formats[(int)TexturePixelStorage.Indexed4];


        //                //swizzle image
        //                this.ImageData = ImageUtility.Swizzle(TextureFormat.Formats[(int)TexturePixelStorage.Indexed4],
        //                    imageData.ToArray(), (uint)this.Width, (uint)this.Height);

        //                imageData = null;


        //            }
        //        }//using BinaryReader

        //        //write txp
        //        FileUtility.DeleteFileIfExists(outfile);

        //        this.Colors = 16;
        //        this.Colors2 = 16;
        //        this.Colors3 = 16;
        //        this.unk1 = 0;
        //        this.unk2 = 0;
        //        this.unk3 = 1;
        //        this.unk4 = 1;
        //        using (var bw = new BinaryWriter(File.OpenWrite(outfile)))
        //        {
        //            bw.Write(this.Width);
        //            bw.Write(this.Height);
        //            bw.Write(this.Colors);
        //            bw.Write(this.unk1);
        //            bw.Write(this.unk2);
        //            bw.Write(this.Colors2);
        //            bw.Write(this.Colors3);
        //            bw.Write(this.unk3);
        //            bw.Write(this.unk4);

        //            for (int i = 0; i < this.Colors * this.Colors3; i++)
        //            {
        //                bw.Write(Palette[i].r);
        //                bw.Write(Palette[i].g);
        //                bw.Write(Palette[i].b);
        //                bw.Write(Palette[i].a);
        //            }

        //            bw.Write(this.ImageData);
        //        }
        //    }
        //}

        //public short Width { get; set; }
        //public short Height { get; set; }
        //public short Colors { get; set; }
        //public byte unk1 { get; set; }
        //public byte unk2 { get; set; }
        //public short Colors2 { get; set; }
        //public short Colors3 { get; set; }
        //public short unk3 { get; set; }
        //public short unk4 { get; set; }

        //public bool EnableSwizzle { get; set; }
        //public bool EnableAlpha { get; set; }
        
    }
}
