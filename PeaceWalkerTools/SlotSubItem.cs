using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PeaceWalkerTools
{
    public class SlotSubItem
    {
        [XmlIgnore]
        public uint Key { get; set; }
        [XmlAttribute("Key")]
        public string KeyHex
        {
            get
            {
                return Key.ToString("X8");
            }
            set
            {
                Key = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
        }
        [XmlIgnore]
        public uint Offset { get; set; }
        [XmlIgnore]
        public uint Length { get; set; }
        [XmlAttribute]
        public EntityExtensions Extension { get; set; }
    }

    public class SlotFile
    {
        public static void Pack(string path)
        {
            Debug.WriteLine(Path.GetFileName(path));

            var list = SerializationHelper<List<SlotSubItem>>.Read(path);

            var output = path.RemoveExtension(1);
            var prefix = path.RemoveExtension(2);

            var offset = 0u;
            using (var writer = new BinaryWriter(File.Create(output)))
            {
                writer.BaseStream.Position = 0x800;

                foreach (var item in list)
                {
                    var fileName = string.Format("{0}_{1:X6}.{2}", prefix, item.Key & 0xFFFFFF, item.Extension);
                    byte[] data;

                    data = File.ReadAllBytes(fileName);

                    item.Offset = offset;
                    item.Length = (uint)Math.Ceiling(data.Length / 16.0) * 16;

                    if (data.Length != item.Length)
                    {
                        Array.Resize(ref data, (int)item.Length);
                    }
                    writer.Write(data);

                    offset += item.Length;
                }


                writer.BaseStream.Position = 0;
                writer.Write((int)(list.Count + 3));
                writer.Write((int)0x7F000002);
                writer.Write((int)offset);
                foreach (var item in list)
                {
                    writer.Write((int)item.Key);
                    writer.Write((int)item.Offset);
                }
                writer.Write((int)0x7F000000);
                writer.Write((int)offset);

                writer.Write(0);
                writer.Write(0);
            }

        }

        public static void Unpack(string path)
        {
            Debug.WriteLine(path);

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                var count = reader.ReadInt32();
                var unknown = reader.ReadInt32();
                var length = reader.ReadInt32();

                var list = new List<SlotSubItem>();

                while (true)
                {
                    var key = reader.ReadUInt32();
                    var value = reader.ReadUInt32();


                    if (list.Count > 0)
                    {
                        if (list.Last().Offset > value)
                        { }
                        var itemLength = value - list[list.Count - 1].Offset;

                        list[list.Count - 1].Length = itemLength;
                    }
                    if (key == 0x7F000000)
                    {
                        break;
                    }

                    var item = new SlotSubItem { Key = key, Offset = value };
                    var extension = ExtensionUtility.GetExtension((byte)(key >> 24));
                    if (extension == EntityExtensions.Unknown)
                    {

                    }
                    item.Extension = extension;
                    list.Add(item);
                }

                SerializationHelper.Save(list, path + ".xml");

                var prefix = path.RemoveExtension(1);

                foreach (var item in list)
                {
                    reader.BaseStream.Position = item.Offset + 0x800;
                    var data = reader.ReadBytes((int)item.Length);

                    var output = string.Format("{0}_{1:X6}.{2}", prefix, item.Key & 0xFFFFFF, item.Extension);

                    Debug.WriteLine("- " + Path.GetFileName(output));
                    File.WriteAllBytes(output, data);
                }

            }
        }

    }
}
