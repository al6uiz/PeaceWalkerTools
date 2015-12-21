//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace PeaceWalkerTools
//{
//    public sealed class StructConverter<T> where T : struct
//    {
//        static Dictionary<Type, int> _sizeCache = new Dictionary<Type, int>();

//        public static T GetStructure(byte[] rawData)
//        {
//            int nSize;
//            Type type = typeof(T);
//            if (!_sizeCache.TryGetValue(type, out nSize))
//            {
//                nSize = Marshal.SizeOf(type);
//                _sizeCache[type] = nSize;
//            }

//            if (nSize != rawData.Length)
//            {
//                throw new ArgumentException();
//            }

//            var pRawData = GCHandle.Alloc(rawData, GCHandleType.Pinned);
//            try
//            {
//                IntPtr pTempHandle = pRawData.AddrOfPinnedObject();
//                return (T)Marshal.PtrToStructure(pTempHandle, typeof(T));
//            }
//            finally
//            {
//                pRawData.Free();
//            }
//        }

//        public static byte[] GetByteArray(T rawData)
//        {
//            int nSize;
//            Type type = typeof(T);
//            if (!_sizeCache.TryGetValue(type, out nSize))
//            {
//                nSize = Marshal.SizeOf(type);
//                _sizeCache[type] = nSize;
//            }

//            byte[] oResult = new byte[nSize];

//            GCHandle pRawData = GCHandle.Alloc(oResult, GCHandleType.Pinned);
//            try
//            {
//                IntPtr pTempHandle = pRawData.AddrOfPinnedObject();
//                Marshal.StructureToPtr(rawData, pTempHandle, false);
//            }
//            finally
//            {
//                pRawData.Free();
//            }

//            return oResult;
//        }
//    }
//}
