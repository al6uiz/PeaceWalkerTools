using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace PeaceWalkerTools
{

    enum FileType
    {
        PGF = 0x00,
        BWFON = 0x01,
    };

    enum FontFlags
    {
        BMP_H_ROWS = 0x01,
        BMP_V_ROWS = 0x02,
        BMP_OVERLAY = 0x03,
        // Metric names according to JPCSP findings
        METRIC_DIMENSION_INDEX = 0x04,
        METRIC_BEARING_X_INDEX = 0x08,
        METRIC_BEARING_Y_INDEX = 0x10,
        METRIC_ADVANCE_INDEX = 0x20,
        CHARGLYPH = 0x20,
        SHADOWGLYPH = 0x40,
    };

    enum Family
    {
        SansSerif = 1,
        Serif = 2,
    };

    enum Style
    {
        Regular = 1,
        Italic = 2,
        Bold = 5,
        BoldItalic = 6,
        SemiBold = 103, // Demi-Bold / semi-bold
    };

    enum Language
    {
        Japanese = 1,
        Latin = 2,
        Korean = 3,
        Chinese = 4,
    };

    enum FontPixelFormat
    {
        PSP_FONT_PIXELFORMAT_4 = 0, // 2 pixels packed in 1 byte (natural order)
        PSP_FONT_PIXELFORMAT_4_REV = 1, // 2 pixels packed in 1 byte (reversed order)
        PSP_FONT_PIXELFORMAT_8 = 2, // 1 pixel in 1 byte
        PSP_FONT_PIXELFORMAT_24 = 3, // 1 pixel in 3 bytes (RGB)
        PSP_FONT_PIXELFORMAT_32 = 4, // 1 pixel in 4 bytes (RGBA)
    };


    struct PGFFontStyle
    {
        public float fontH;
        public float fontV;
        public float fontHRes;
        public float fontVRes;
        public float fontWeight;
        public ushort fontFamily;
        public ushort fontStyle;
        // Check.
        public ushort fontStyleSub;
        public ushort fontLanguage;
        public ushort fontRegion;
        public ushort fontCountry;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string fontName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string fontFileName;
        public uint fontAttributes;
        public uint fontExpire;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Glyph
    {
        public int w;
        public int h;
        public int left;
        public int top;
        public int flags;
        public int shadowFlags;
        public int shadowID;
        public int advanceH;
        public int advanceV;
        public int dimensionWidth, dimensionHeight;
        public int xAdjustH, xAdjustV;
        public int yAdjustH, yAdjustV;
        public int ptr;
    };


    //#if COMMON_LITTLE_ENDIAN
    //typedef FontPixelFormat FontPixelFormat;
    //#else
    //typedef swap_struct_t<FontPixelFormat, swap_32_t<FontPixelFormat> > FontPixelFormat;
    //#endif


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GlyphImage
    {
        public FontPixelFormat pixelFormat;
        public int xPos64;
        public int yPos64;
        public ushort bufWidth;
        public ushort bufHeight;
        public ushort bytesPerLine;
        public ushort pad;
        public uint bufferPtr;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PGFHeader
    {
        public ushort headerOffset;
        public ushort headerSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string PGFMagic;
        public int revision;
        public int version;

        public int charMapLength;
        public int charPointerLength;
        public int charMapBpe;
        public int charPointerBpe;

        // TODO: This has values in it (0404)...
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] pad1;
        public byte bpp;
        public byte pad2;

        public int hSize;
        public int vSize;
        public int hResolution;
        public int vResolution;

        public byte pad3;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string fontName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string fontType;
        public byte pad4;

        public ushort firstGlyph;
        public ushort lastGlyph;

        // TODO: This has a few 01s in it in the official fonts.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public byte[] pad5;

        public int maxAscender;
        public int maxDescender;
        public int maxLeftXAdjust;
        public int maxBaseYAdjust;
        public int minCenterXAdjust;
        public int maxTopYAdjust;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] maxAdvance;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] maxSize;
        public ushort maxGlyphWidth;
        public ushort maxGlyphHeight;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] pad6;

        public byte dimTableLength;
        public byte xAdjustTableLength;
        public byte yAdjustTableLength;
        public byte advanceTableLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 102)]
        public byte[] pad7;

        public int shadowMapLength;
        public int shadowMapBpe;
        public float unknown1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] shadowScale;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] pad8;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PGFHeaderRev3Extra
    {
        public int compCharMapBpe1;
        public int compCharMapLength1;
        public int compCharMapBpe2;
        public int compCharMapLength2;
        public uint unknown;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PGFCharInfo
    {
        public uint bitmapWidth;
        public uint bitmapHeight;
        public uint bitmapLeft;
        public uint bitmapTop;
        // Glyph metrics (in 26.6 signed fixed-point).
        public uint sfp26Width;
        public uint sfp26Height;
        public int sfp26Ascender;
        public int sfp26Descender;
        public int sfp26BearingHX;
        public int sfp26BearingHY;
        public int sfp26BearingVX;
        public int sfp26BearingVY;
        public int sfp26AdvanceH;
        public int sfp26AdvanceV;
        public short shadowFlags;
        public short shadowId;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PGFFontInfo
    {
        // Glyph metrics (in 26.6 signed fixed-point).
        public int maxGlyphWidthI;
        public int maxGlyphHeightI;
        public int maxGlyphAscenderI;
        public int maxGlyphDescenderI;
        public int maxGlyphLeftXI;
        public int maxGlyphBaseYI;
        public int minGlyphCenterXI;
        public int maxGlyphTopYI;
        public int maxGlyphAdvanceXI;
        public int maxGlyphAdvanceYI;

        // Glyph metrics (replicated as float).
        public float maxGlyphWidthF;
        public float maxGlyphHeightF;
        public float maxGlyphAscenderF;
        public float maxGlyphDescenderF;
        public float maxGlyphLeftXF;
        public float maxGlyphBaseYF;
        public float minGlyphCenterXF;
        public float maxGlyphTopYF;
        public float maxGlyphAdvanceXF;
        public float maxGlyphAdvanceYF;

        // Bitmap dimensions.
        public short maxGlyphWidth;
        public short maxGlyphHeight;
        public int numGlyphs;
        public int shadowMapLength; // Number of elements in the font's shadow charmap.

        // Font style (used by font comparison functions).
        public PGFFontStyle fontStyle;

        public byte BPP; // Font's BPP.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        byte[] pad;
    };


    class PGF
    {

        PGFHeaderRev3Extra rev3extra;

        // Font character image data
        byte[] fontData;
        int fontDataSize;

        string fileName;

        List<int>[] dimensionTable = new List<int>[2];
        List<int>[] xAdjustTable = new List<int>[2];
        List<int>[] yAdjustTable = new List<int>[2];
        List<int>[] advanceTable = new List<int>[2];

        // Unused
        List<int>[] charmapCompressionTable1 = new List<int>[2];
        List<int>[] charmapCompressionTable2 = new List<int>[2];

        List<int> charmap_compr;
        Dictionary<int, int> charmap = new Dictionary<int, int>();
        List<char> charmap_unicode = new List<char>();

        Dictionary<int, Glyph> glyphs = new Dictionary<int, Glyph>();
        Dictionary<int, Glyph> shadowGlyphs = new Dictionary<int, Glyph>();
        int firstGlyph;


        public PGFHeader header;

        static List<int> getTable(byte[] buf, int offset, int bpe, int length)
        {
            var vec = new List<int>(length);
            for (var i = 0; i < length; i++)
            {
                vec.Add(getBits(bpe, buf, offset, bpe * i));
            }
            return vec;
        }

        public bool ReadPtr(byte[] data, int dataSize)
        {
            var ptr = 0;
            var startPtr = 0;

            if (dataSize < Marshal.SizeOf(typeof(PGFHeader)))
            {
                return false;
            }

            //DEBUG_LOG(SCEFONT, "Reading %d bytes of PGF header", (int)sizeof(header));

            Get(data, ptr, out header);
            ptr += Marshal.SizeOf(typeof(PGFHeader));

            fileName = header.fontName;

            if (header.revision == 3)
            {
                Get(data, ptr, out rev3extra);
                rev3extra.compCharMapLength1 &= 0xFFFF;
                rev3extra.compCharMapLength2 &= 0xFFFF;
                ptr += Marshal.SizeOf(typeof(PGFHeaderRev3Extra));
            }

            int wptr = ptr;
            dimensionTable[0] = new List<int>(header.dimTableLength);
            dimensionTable[1] = new List<int>(header.dimTableLength);

            for (int i = 0; i < header.dimTableLength; i++)
            {
                dimensionTable[0].Add(GetInt32(ref wptr, data));
                dimensionTable[1].Add(GetInt32(ref wptr, data));
            }

            xAdjustTable[0] = new List<int>(header.xAdjustTableLength);
            xAdjustTable[1] = new List<int>(header.xAdjustTableLength);
            for (int i = 0; i < header.xAdjustTableLength; i++)
            {
                xAdjustTable[0].Add(GetInt32(ref wptr, data));
                xAdjustTable[1].Add(GetInt32(ref wptr, data));
            }

            yAdjustTable[0] = new List<int>(header.yAdjustTableLength);
            yAdjustTable[1] = new List<int>(header.yAdjustTableLength);
            for (int i = 0; i < header.yAdjustTableLength; i++)
            {
                yAdjustTable[0].Add(GetInt32(ref wptr, data));
                yAdjustTable[1].Add(GetInt32(ref wptr, data));
            }

            advanceTable[0] = new List<int>(header.advanceTableLength);
            advanceTable[1] = new List<int>(header.advanceTableLength);
            for (int i = 0; i < header.advanceTableLength; i++)
            {
                advanceTable[0].Add(GetInt32(ref wptr, data));
                advanceTable[1].Add(GetInt32(ref wptr, data));
            }

            var uptr = wptr;

            if (uptr >= startPtr + dataSize)
            {
                return false;
            }

            int shadowCharMapSize = ((header.shadowMapLength * header.shadowMapBpe + 31) & ~31) / 8;
            var shadowCharMap = uptr;
            uptr += shadowCharMapSize;

            var sptr = uptr;
            if (header.revision == 3)
            {
                charmapCompressionTable1[0] = new List<int>(rev3extra.compCharMapLength1);
                charmapCompressionTable1[1] = new List<int>(rev3extra.compCharMapLength1);
                for (int i = 0; i < rev3extra.compCharMapLength1; i++)
                {
                    charmapCompressionTable1[0][i] = GetInt16(ref sptr, data);
                    charmapCompressionTable1[1][i] = GetInt16(ref sptr, data);
                }

                charmapCompressionTable2[0] = new List<int>(rev3extra.compCharMapLength2);
                charmapCompressionTable2[1] = new List<int>(rev3extra.compCharMapLength2);
                for (int i = 0; i < rev3extra.compCharMapLength2; i++)
                {
                    charmapCompressionTable2[0][i] = GetInt16(ref sptr, data);
                    charmapCompressionTable2[1][i] = GetInt16(ref sptr, data);
                }
            }

            uptr = sptr;

            if (uptr >= startPtr + dataSize)
            {
                return false;
            }

            int charMapSize = ((header.charMapLength * header.charMapBpe + 31) & ~31) / 8;
            var charMap = uptr;
            uptr += charMapSize;

            int charPointerSize = (((header.charPointerLength * header.charPointerBpe + 31) & ~31) / 8);
            var charPointerTable = uptr;
            uptr += charPointerSize;

            // PGF Fontdata.
            var fontDataOffset = (uptr - startPtr);

            fontDataSize = dataSize - fontDataOffset;
            fontData = new byte[fontDataSize];
            Buffer.BlockCopy(data, uptr, fontData, 0, fontDataSize);

            // charmap= new List<int>();
            //charmap = new List<int>(header.charMapLength);
            //for (int i = 0; i < header.charMapLength; i++)
            //{
            //    charmap.Add(0);
            //}

            int charmap_compr_len = header.revision == 3 ? 7 : 1;
            charmap_compr = new List<int>(charmap_compr_len * 4);

            //for (int i = 0; i < header.charPointerLength; i++)
            //{
            //    glyphs.Add(new Glyph());
            //}

            //shadowGlyphs = new List<Glyph>(header.charPointerLength);
            //for (int i = 0; i < header.charPointerLength; i++)
            //{
            //    shadowGlyphs.Add(new Glyph());
            //}

            firstGlyph = header.firstGlyph;

            // Parse out the char map (array where each entry is an irregular number of bits)
            // BPE = bits per entry, I think.
            for (int i = 0; i < header.charMapLength; i++)
            {
                var c = getBits(header.charMapBpe, data, charMap, i * header.charMapBpe);
                charmap[i] = c;
                var uc = (char)GetString(c)[0];
                charmap_unicode.Add(uc);

                // This check seems a little odd.
                if ((int)charmap[i] >= header.charPointerLength)
                    charmap[i] = 65535;
            }

            List<int> charPointers = getTable(data, charPointerTable, header.charPointerBpe, header.charPointerLength);
            List<int> shadowMap = getTable(data, shadowCharMap, header.shadowMapBpe, header.shadowMapLength);

            // Pregenerate glyphs.
            for (int i = 0; i < header.charPointerLength; i++)
            {
                Glyph g;

                ReadCharGlyph(fontData, charPointers[i] * 4 * 8  /* ??? */, out g);

                glyphs[i] = g;
            }

            // And shadow glyphs.
            for (int i = 0; i < header.charPointerLength; i++)
            {
                int shadowId = glyphs[i].shadowID;
                if (shadowId < shadowMap.Count)
                {
                    int charId = shadowMap[shadowId];
                    if (charId < shadowGlyphs.Count)
                    {
                        Glyph g;
                        // TODO: check for pre existing shadow glyph
                        ReadShadowGlyph(fontData, charPointers[charId] * 4 * 8  /* ??? */, out g);
                        shadowGlyphs[charId] = g;
                    }
                }
            }

            return true;
        }

        private string GetString(int c)
        {
            //var UnicodeIndex = c + header.firstGlyph;


            //return new string((char)UnicodeIndex, 1);


            var raw = BitConverter.GetBytes(c);
            if (c == 0) return new string('\0', 1);

            if (raw[3] == 0 && raw[2] == 0 && raw[1] == 0)
            {
                var s = Encoding.Unicode.GetString(new byte[] { raw[0] });

                return s;
            }
            else if (raw[3] == 0 && raw[2] == 0)
            {
                var s = Encoding.Unicode.GetString(new byte[] { raw[0], raw[1] });

                return s;
            }
            else if (raw[3] == 0)
            {
                var s = Encoding.Unicode.GetString(new byte[] { raw[0], raw[1], raw[2] });

                return s;
            }


            return Encoding.Unicode.GetString(new byte[] { raw[0], raw[1], raw[2], raw[3] });
        }

        private static int GetInt16(ref int sptr, byte[] data)
        {
            var result = BitConverter.ToInt16(data, sptr);

            sptr += 2;

            return result;
        }

        private static int GetInt32(ref int wptr, byte[] data)
        {
            var result = BitConverter.ToInt32(data, wptr);

            wptr += 4;

            return result;
        }


        private static uint GetUInt32(ref int wptr, byte[] data)
        {
            var result = BitConverter.ToUInt32(data, wptr);

            wptr += 4;

            return result;
        }

        private static void Get<T1>(byte[] data, int ptr, out T1 result) where T1 : struct
        {
            var size = Marshal.SizeOf(typeof(T1));
            var raw = new byte[size];
            Buffer.BlockCopy(data, ptr, raw, 0, size);
            result = default(T1);
            //result = StructConverter<T1>.GetStructure(raw);
        }

        public bool GetCharInfo(int charCode, out PGFCharInfo charInfo, int altCharCode, FontFlags glyphType = FontFlags.CHARGLYPH)
        {
            Glyph glyph;
            charInfo = new PGFCharInfo();

            if (!GetCharGlyph(charCode, glyphType, out glyph))
            {
                if (charCode < firstGlyph)
                {
                    // Character not in font, return zeroed charInfo as on real PSP.
                    return false;
                }
                if (!GetCharGlyph(altCharCode, glyphType, out glyph))
                {
                    return false;
                }
            }

            charInfo.bitmapWidth = (uint)glyph.w;
            charInfo.bitmapHeight = (uint)glyph.h;
            charInfo.bitmapLeft = (uint)glyph.left;
            charInfo.bitmapTop = (uint)glyph.top;
            charInfo.sfp26Width = (uint)glyph.dimensionWidth;
            charInfo.sfp26Height = (uint)glyph.dimensionHeight;
            charInfo.sfp26Ascender = glyph.yAdjustH;
            // Font y goes upwards.  If top is 10 and height is 11, the descender is approx. -1 (below 0.)
            charInfo.sfp26Descender = charInfo.sfp26Ascender - (int)charInfo.sfp26Height;
            charInfo.sfp26BearingHX = glyph.xAdjustH;
            charInfo.sfp26BearingHY = glyph.yAdjustH;
            charInfo.sfp26BearingVX = glyph.xAdjustV;
            charInfo.sfp26BearingVY = glyph.yAdjustV;
            charInfo.sfp26AdvanceH = glyph.advanceH;
            charInfo.sfp26AdvanceV = glyph.advanceV;
            charInfo.shadowFlags = (short)glyph.shadowFlags;
            charInfo.shadowId = (short)glyph.shadowID;
            return true;
        }
        public void GetFontInfo(out PGFFontInfo fi)
        {
            fi = new PGFFontInfo();
            fi.maxGlyphWidthI = header.maxSize[0];
            fi.maxGlyphHeightI = header.maxSize[1];
            fi.maxGlyphAscenderI = header.maxAscender;
            fi.maxGlyphDescenderI = header.maxDescender;
            fi.maxGlyphLeftXI = header.maxLeftXAdjust;
            fi.maxGlyphBaseYI = header.maxBaseYAdjust;
            fi.minGlyphCenterXI = header.minCenterXAdjust;
            fi.maxGlyphTopYI = header.maxTopYAdjust;
            fi.maxGlyphAdvanceXI = header.maxAdvance[0];
            fi.maxGlyphAdvanceYI = header.maxAdvance[1];
            fi.maxGlyphWidthF = (float)header.maxSize[0] / 64.0f;
            fi.maxGlyphHeightF = (float)header.maxSize[1] / 64.0f;
            fi.maxGlyphAscenderF = (float)header.maxAscender / 64.0f;
            fi.maxGlyphDescenderF = (float)header.maxDescender / 64.0f;
            fi.maxGlyphLeftXF = (float)header.maxLeftXAdjust / 64.0f;
            fi.maxGlyphBaseYF = (float)header.maxBaseYAdjust / 64.0f;
            fi.minGlyphCenterXF = (float)header.minCenterXAdjust / 64.0f;
            fi.maxGlyphTopYF = (float)header.maxTopYAdjust / 64.0f;
            fi.maxGlyphAdvanceXF = (float)header.maxAdvance[0] / 64.0f;
            fi.maxGlyphAdvanceYF = (float)header.maxAdvance[1] / 64.0f;

            fi.maxGlyphWidth = (short)header.maxGlyphWidth;
            fi.maxGlyphHeight = (short)header.maxGlyphHeight;
            fi.numGlyphs = header.charPointerLength;
            fi.shadowMapLength = 0;  // header.shadowMapLength; TODO

            fi.BPP = header.bpp;
        }
        public void DrawCharacter(GlyphImage image, int clipX, int clipY, int clipWidth, int clipHeight, int charCode, int altCharCode, FontFlags glyphType)
        {
            Glyph glyph;
            if (!GetCharGlyph(charCode, glyphType, out glyph))
            {
                // No Glyph available for this charCode, try to use the alternate char.
                charCode = altCharCode;
                if (!GetCharGlyph(charCode, glyphType, out glyph))
                {
                    return;
                }
            }

            if (glyph.w <= 0 || glyph.h <= 0)
            {
                return;
            }

            if (((glyph.flags & (int)FontFlags.BMP_OVERLAY) != (int)FontFlags.BMP_H_ROWS) &&
                ((glyph.flags & (int)FontFlags.BMP_OVERLAY) != (int)FontFlags.BMP_V_ROWS))
            {
                return;
            }

            int bitPtr = (int)(glyph.ptr * 8);
            int numberPixels = glyph.w * glyph.h;
            int pixelIndex = 0;

            int x = image.xPos64 >> 6;
            int y = image.yPos64 >> 6;

            // Negative means don't clip on that side.
            if (clipX < 0)
                clipX = 0;
            if (clipY < 0)
                clipY = 0;
            if (clipWidth < 0)
                clipWidth = 8192;
            if (clipHeight < 0)
                clipHeight = 8192;

            while (pixelIndex < numberPixels && bitPtr + 8 < fontDataSize * 8)
            {
                // This is some kind of nibble based RLE compression.
                int nibble = consumeBits(4, fontData, ref bitPtr);

                int count;
                int value = 0;
                if (nibble < 8)
                {
                    value = consumeBits(4, fontData, ref bitPtr);
                    count = nibble + 1;
                }
                else
                {
                    count = 16 - nibble;
                }

                for (int i = 0; i < count && pixelIndex < numberPixels; i++)
                {
                    if (nibble >= 8)
                    {
                        value = consumeBits(4, fontData, ref bitPtr);
                    }

                    int xx, yy;
                    if (((FontFlags)glyph.flags & FontFlags.BMP_OVERLAY) == FontFlags.BMP_H_ROWS)
                    {
                        xx = pixelIndex % glyph.w;
                        yy = pixelIndex / glyph.w;
                    }
                    else
                    {
                        xx = pixelIndex / glyph.h;
                        yy = pixelIndex % glyph.h;
                    }

                    int pixelX = x + xx;
                    int pixelY = y + yy;

                    if (pixelX >= clipX && pixelX < clipX + clipWidth && pixelY >= clipY && pixelY < clipY + clipHeight)
                    {
                        // 4-bit color value
                        int pixelColor = value;
                        switch ((FontPixelFormat)(int)image.pixelFormat)
                        {
                            case FontPixelFormat.PSP_FONT_PIXELFORMAT_8:
                            {
                                // 8-bit color value
                                pixelColor |= pixelColor << 4;
                                break;
                            }
                            case FontPixelFormat.PSP_FONT_PIXELFORMAT_24:
                            {
                                // 24-bit color value
                                pixelColor |= pixelColor << 4;
                                pixelColor |= pixelColor << 8;
                                pixelColor |= pixelColor << 8;
                                break;
                            }
                            case FontPixelFormat.PSP_FONT_PIXELFORMAT_32:
                            {
                                // 32-bit color value
                                pixelColor |= pixelColor << 4;
                                pixelColor |= pixelColor << 8;
                                pixelColor |= pixelColor << 16;
                                break;
                            }
                            case FontPixelFormat.PSP_FONT_PIXELFORMAT_4:
                            case FontPixelFormat.PSP_FONT_PIXELFORMAT_4_REV:
                            {
                                break;
                            }
                            default:
                            {
                                //ERROR_LOG_REPORT(SCEFONT, "Unhandled font pixel format: %d", (int)image.pixelFormat);
                                break;
                            }
                        }

                        SetFontPixel(image.bufferPtr, image.bytesPerLine, image.bufWidth, image.bufHeight, pixelX, pixelY, pixelColor, image.pixelFormat);
                    }

                    pixelIndex++;
                }
            }

            //gpu.InvalidateCache(image.bufferPtr, image.bytesPerLine * image.bufHeight, GPU_INVALIDATE_SAFE);
        }

        private static readonly byte[] fontPixelSizeInBytes = { 0, 0, 1, 3, 4 }; // 0 means 2 pixels per byte
        //public void DoState(PointerWrap &p){}

        void SetFontPixel(uint @base, int bpl, int bufWidth, int bufHeight, int x, int y, int pixelColor, FontPixelFormat pixelformat)
        {
            if (x < 0 || x >= bufWidth || y < 0 || y >= bufHeight)
            {
                return;
            }

            int pixelBytes = fontPixelSizeInBytes[(int)pixelformat];
            int bufMaxWidth = (pixelBytes == 0 ? bpl * 2 : bpl / pixelBytes);
            if (x >= bufMaxWidth)
            {
                return;
            }

            int framebufferAddr = (int)(@base + (y * bpl) + (pixelBytes == 0 ? x / 2 : x * pixelBytes));

            switch (pixelformat)
            {
                case FontPixelFormat.PSP_FONT_PIXELFORMAT_4:
                case FontPixelFormat.PSP_FONT_PIXELFORMAT_4_REV:
                {
                    int oldColor = Read_U8(framebufferAddr);
                    int newColor;
                    if ((x & 1) != (int)pixelformat)
                    {
                        newColor = (pixelColor << 4) | (oldColor & 0xF);
                    }
                    else
                    {
                        newColor = (oldColor & 0xF0) | pixelColor;
                    }
                    Write_U8(newColor, framebufferAddr);
                    break;
                }
                case FontPixelFormat.PSP_FONT_PIXELFORMAT_8:
                {
                    Write_U8((byte)pixelColor, framebufferAddr);
                    break;
                }
                case FontPixelFormat.PSP_FONT_PIXELFORMAT_24:
                {
                    Write_U8(pixelColor & 0xFF, framebufferAddr + 0);
                    Write_U8(pixelColor >> 8, framebufferAddr + 1);
                    Write_U8(pixelColor >> 16, framebufferAddr + 2);
                    break;
                }
                case FontPixelFormat.PSP_FONT_PIXELFORMAT_32:
                {
                    Write_U32(pixelColor, framebufferAddr);
                    break;
                }
            }
        }

        private int Read_U8(int framebufferAddr)
        {
            throw new NotImplementedException();
        }

        private void Write_U8(int v1, int v2)
        {
            throw new NotImplementedException();
        }

        private void Write_U32(int pixelColor, int framebufferAddr)
        {
            throw new NotImplementedException();
        }

        static bool isJPCSPFont(string fontName)
        {
            return !(fontName == "Liberation Sans") || !(fontName == "Liberation Serif") || !(fontName == "Sazanami") || !(fontName == "UnDotum") || !(fontName == "Microsoft YaHei");
        }

        private bool ReadCharGlyph(byte[] fontdata, int charPtr, out Glyph glyph)
        {
            glyph = new Glyph();
            // Skip size.
            charPtr += 14;

            glyph.w = consumeBits(7, fontdata, ref charPtr);
            glyph.h = consumeBits(7, fontdata, ref charPtr);

            glyph.left = consumeBits(7, fontdata, ref charPtr);
            if (glyph.left >= 64)
            {
                glyph.left -= 128;
            }

            glyph.top = consumeBits(7, fontdata, ref charPtr);
            if (glyph.top >= 64)
            {
                glyph.top -= 128;
            }

            glyph.flags = consumeBits(6, fontdata, ref charPtr);

            glyph.shadowFlags = consumeBits(2, fontdata, ref charPtr) << (2 + 3);
            glyph.shadowFlags |= consumeBits(2, fontdata, ref charPtr) << 3;
            glyph.shadowFlags |= consumeBits(3, fontdata, ref charPtr);

            glyph.shadowID = consumeBits(9, fontdata, ref charPtr);

            if (((FontFlags)glyph.flags & FontFlags.METRIC_DIMENSION_INDEX) == FontFlags.METRIC_DIMENSION_INDEX)
            {
                int dimensionIndex = consumeBits(8, fontdata, ref charPtr);

                if (dimensionIndex < header.dimTableLength)
                {
                    glyph.dimensionWidth = dimensionTable[0][dimensionIndex];
                    glyph.dimensionHeight = dimensionTable[1][dimensionIndex];
                }

                if (dimensionIndex == 0 && isJPCSPFont(fileName))
                {
                    // Fonts created by ttf2pgf do not contain complete Glyph information.
                    // Provide default values.
                    glyph.dimensionWidth = glyph.w << 6;
                    glyph.dimensionHeight = glyph.h << 6;
                }
            }
            else
            {
                glyph.dimensionWidth = consumeBits(32, fontdata, ref charPtr);
                glyph.dimensionHeight = consumeBits(32, fontdata, ref charPtr);
            }

            if (((FontFlags)glyph.flags & FontFlags.METRIC_BEARING_X_INDEX) == FontFlags.METRIC_BEARING_X_INDEX)
            {
                int xAdjustIndex = consumeBits(8, fontdata, ref charPtr);

                if (xAdjustIndex < header.xAdjustTableLength)
                {
                    glyph.xAdjustH = xAdjustTable[0][xAdjustIndex];
                    glyph.xAdjustV = xAdjustTable[1][xAdjustIndex];
                }

                if (xAdjustIndex == 0 && isJPCSPFont(fileName))
                {
                    // Fonts created by ttf2pgf do not contain complete Glyph information.
                    // Provide default values.
                    glyph.xAdjustH = glyph.left << 6;
                    glyph.xAdjustV = glyph.left << 6;
                }
            }
            else
            {
                glyph.xAdjustH = consumeBits(32, fontdata, ref charPtr);
                glyph.xAdjustV = consumeBits(32, fontdata, ref charPtr);
            }

            if (((FontFlags)glyph.flags & FontFlags.METRIC_BEARING_Y_INDEX) == FontFlags.METRIC_BEARING_Y_INDEX)
            {
                int yAdjustIndex = consumeBits(8, fontdata, ref charPtr);

                if (yAdjustIndex < header.yAdjustTableLength)
                {
                    glyph.yAdjustH = yAdjustTable[0][yAdjustIndex];
                    glyph.yAdjustV = yAdjustTable[1][yAdjustIndex];
                }

                if (yAdjustIndex == 0 && isJPCSPFont(fileName))
                {
                    // Fonts created by ttf2pgf do not contain complete Glyph information.
                    // Provide default values.
                    glyph.yAdjustH = glyph.top << 6;
                    glyph.yAdjustV = glyph.top << 6;
                }
            }
            else
            {
                glyph.yAdjustH = consumeBits(32, fontdata, ref charPtr);
                glyph.yAdjustV = consumeBits(32, fontdata, ref charPtr);
            }

            if (((FontFlags)glyph.flags & FontFlags.METRIC_ADVANCE_INDEX) == FontFlags.METRIC_ADVANCE_INDEX)
            {
                int advanceIndex = consumeBits(8, fontdata, ref charPtr);

                if (advanceIndex < header.advanceTableLength)
                {
                    glyph.advanceH = advanceTable[0][advanceIndex];
                    glyph.advanceV = advanceTable[1][advanceIndex];
                }
            }
            else
            {
                glyph.advanceH = consumeBits(32, fontdata, ref charPtr);
                glyph.advanceV = consumeBits(32, fontdata, ref charPtr);
            }

            glyph.ptr = (int)(charPtr / 8);
            return true;
        }

        private int consumeBits(int numBits, byte[] buf, ref int pos)
        {
            int v = getBits(numBits, buf, 0, pos);
            pos += numBits;
            return v;
        }

        static int getBits(int numBits, byte[] buffer, int offset, int position)
        {
            //_dbg_assert_msg_(SCEFONT, numBits <= 32, "Unable to return more than 32 bits, %d requested", numBits);

            int wordpos = position >> 5;
            byte[] wordbuf = buffer;
            byte bitoff = (byte)(position & 31);

            // Might just be in one, has to be within two.
            if (bitoff + numBits < 32)
            {
                int mask = (int)((1 << numBits) - 1);

                var woffset = offset + wordpos * 4;

                return (int)((GetInt32(ref woffset, wordbuf) >> bitoff) & mask);
            }
            else
            {
                var woffset = offset + wordpos * 4;
                int v = (int)(GetUInt32(ref woffset, wordbuf) >> bitoff);

                byte done = (byte)(32 - bitoff);
                byte remaining = (byte)(numBits - done);
                int mask = (int)((1 << remaining) - 1);

                woffset = offset + (wordpos + 1) * 4;

                v |= (int)((GetInt32(ref woffset, wordbuf) & mask) << done);
                return v;
            }
        }

        private bool ReadShadowGlyph(byte[] fontdata, int charPtr, out Glyph glyph)
        {
            glyph = new Glyph();
            // Most of the glyph info is from the char data.
            if (!ReadCharGlyph(fontdata, charPtr, out glyph))
                return false;

            // Skip over the char data.
            if (charPtr + 96 > fontDataSize * 8)
                return false;
            charPtr += getBits(14, fontdata, 0, charPtr) * 8;
            if (charPtr + 96 > fontDataSize * 8)
                return false;

            // Skip size.
            charPtr += 14;

            glyph.w = consumeBits(7, fontdata, ref charPtr);
            glyph.h = consumeBits(7, fontdata, ref charPtr);

            glyph.left = consumeBits(7, fontdata, ref charPtr);
            if (glyph.left >= 64)
            {
                glyph.left -= 128;
            }

            glyph.top = consumeBits(7, fontdata, ref charPtr);
            if (glyph.top >= 64)
            {
                glyph.top -= 128;
            }

            glyph.ptr = (int)(charPtr / 8);
            return true;
        }
        private bool GetCharGlyph(int charCode, FontFlags glyphType, out Glyph glyph)
        {
            glyph = new Glyph();
            if (charCode < firstGlyph)
                return false;
            charCode -= firstGlyph;
            if (charCode < (int)charmap.Count)
            {
                charCode = charmap[charCode];
            }
            if (glyphType == FontFlags.CHARGLYPH)
            {
                if (charCode >= (int)glyphs.Count)
                    return false;
                glyph = glyphs[charCode];
            }
            else
            {
                if (charCode >= (int)shadowGlyphs.Count)
                    return false;
                glyph = shadowGlyphs[charCode];
            }
            return true;
        }
    }
}
