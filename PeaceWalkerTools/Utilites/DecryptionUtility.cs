using System;

namespace PeaceWalkerTools
{
    static class DecryptionUtility
    {
        private static void Write(byte[] data, int offset, int value)
        {
            data[offset + 0] = (byte)((value >> 0) & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
            data[offset + 2] = (byte)((value >> 16) & 0xFF);
            data[offset + 3] = (byte)((value >> 24) & 0xFF);
        }


        public static void Decrypt(byte[] listData, ref Hash hash)
        {
            Decrypt(listData, 0, listData.Length, ref hash);
        }

        public static void Decrypt(byte[] raw, int offset, int length, ref Hash hash)
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
    }
}
