using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeaceWalkerTools
{
    class ExtensionUtility
    {
        public static Dictionary<string, int> ExtensionMap { get; private set; } = Enum.GetValues(typeof(EntityExtensions)).Cast<EntityExtensions>().ToDictionary(x => x.ToString(), x => (int)x);

        private static readonly byte[] EXTENSION_HASH =
        {
             0x1A,  0xF1,  0x21,  0x20,
             0x30,  0x05,  0x6E,  0x1D,
             0x35,  0x6B,  0x0A,  0x64,
             0xFF,  0x1B,  0x18,  0x02,

             0x10,  0x38,  0x1E,  0x03,
             0x16,  0x13,  0x65,  0x24,
             0x15,  0x5F,  0x33,  0x23,
             0x17,  0x6D,  0x6C,  0x06,

             0x5D,  0x09,  0x36,  0x61,
             0x04,  0x1F,  0x01,  0x19,
             0x69,  0x13,  0x6A,  0x12,
             0x34,  0x14,  0x22,  0x0F,

             0x63,  0x68,  0x5E,  0x66,
             0xF0,  0x1C,  0x32,  0x6F,
             0x0C,  0x07,  0x31,  0x08,
             0x11,  0x60,  0x13,  0x0B,

             0x37,  0x13,  0xF2,
        };

        private static Dictionary<byte, EntityExtensions> GetReverseExtension()
        {
            var map = new Dictionary<byte, EntityExtensions>();
            for (int i = 0; i < EXTENSION_HASH.Length; i++)
            {
                map[EXTENSION_HASH[i]] = (EntityExtensions)i;
            }
            return map;
        }


        public static int GetFileNameHash(string input)
        {
            var indexOfDot = input.IndexOf('.');

            var extension = input.Substring(indexOfDot + 1);

            var hash = 0;

            for (int i = 0; input[i] != 0 && input[i] != '.'; i++)
            {
                var temp1 = hash >> 0x13;
                var temp2 = hash << 5;
                temp2 |= temp1;
                hash = temp2 + input[i];

                hash &= 0x00FFFFFF;
            }


            int typeIndex;
            ExtensionMap.TryGetValue(extension, out typeIndex);
            hash |= EXTENSION_HASH[typeIndex] << 24;

            return hash;
        }

        public static Dictionary<byte, EntityExtensions> ReverseExtensionMap { get; private set; } = GetReverseExtension();

        internal static EntityExtensions GetExtension(byte v)
        {
            EntityExtensions extension;
            if (!ReverseExtensionMap.TryGetValue(v, out extension))
            {
                extension = EntityExtensions.Unknown;
            }
            return extension;
        }
    }


    public enum EntityExtensions
    {
        mdpe, qar, vrdv, vrd,
        mgm, mds, row, spk,
        cap, rat, mtfa, eqp,
        psq, dcd, mtst, gcx,

        cvd, bgp, ohd, tri,
        rpd, mdp, vlm, vcpg,
        kms, la2, ptcp, vcp,
        fcx, ola, rcm, lt2,

        olang, mtsq, pcmp, vram,
        mdh, mmd, bin, mdpb,
        img, mdc, vib, zon,
        cddl, txp, vrdt, nav,

        cmf, png, la3, lst,
        dar, ypk, rlc, mtra,
        geom, cv2, prx, mtar,
        eft, slot, mdl, mtcm,

        sep, mdb, cnf,

        Unknown = -1,
    }

}
