using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace PeaceWalkerTools
{
    public class YPK
    {
        public List<Entity> Entities { get; set; } = new List<Entity>();

        public static YPK Read(string path)
        {
            var ypk = new YPK();

            using (var reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(path)), Encoding.UTF8))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var entity = new Entity();
                    ypk.Entities.Add(entity);
                    var start = reader.BaseStream.Position;
                    reader.ReadInt32(); // 'GTT\0'
                    var lineCount = reader.ReadInt32(); // LineCount 
                    var textStart = reader.ReadInt32(); // TextStart 
                    reader.ReadInt32(); // TotalLength 

                    for (int i = 0; i < lineCount; i++)
                    {
                        var sub = new Line();
                        entity.Lines.Add(sub);

                        sub.SyncStart = reader.ReadInt16();
                        sub.SyncEnd = reader.ReadInt16();
                        sub.Unknown = reader.ReadInt16();

                        sub.TextStart = reader.ReadInt16();
                        reader.ReadInt16();

                        sub.TextEnd = reader.ReadInt16();
                        reader.ReadInt16();
                        reader.ReadInt16();
                        reader.ReadInt16();
                        reader.ReadInt16();
                    }

                    for (int i = 0; i < lineCount; i++)
                    {
                        reader.BaseStream.Position = start + textStart + entity.Lines[i].TextStart;
                        var textLength = entity.Lines[i].TextEnd - entity.Lines[i].TextStart;
                        var text = Encoding.UTF8.GetString(reader.ReadBytes(textLength), 0, textLength - 1);
                        entity.Lines[i].Text = text.Replace("\\n", "\n");
                    }

                    reader.BaseStream.Position += (16 - reader.BaseStream.Position % 16) % 16;
                }
            }

            return ypk;
        }

        public void Write(string path)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    foreach (var entity in Entities)
                    {
                        using (var msSub = new MemoryStream())
                        {
                            using (var subWriter = new BinaryWriter(msSub))
                            {
                                subWriter.Write(0x00545447);// 'GTT\0'
                                subWriter.Write(entity.Lines.Count);

                                var textStart = 0x10 + 0x14 * entity.Lines.Count + 0x14;
                                subWriter.Write(textStart);

                                subWriter.BaseStream.Position = textStart;

                                var offset = 0;
                                foreach (var line in entity.Lines)
                                {
                                    line.TextStart = (short)offset;
                                    var rawLine = Encoding.UTF8.GetBytes(ConvertText(line.Text));
                                    subWriter.Write(rawLine);
                                    subWriter.Write((byte)0);
                                    offset += rawLine.Length + 1;
                                    line.TextEnd = (short)offset;
                                }
                                var textEnd = subWriter.BaseStream.Position;

                                subWriter.BaseStream.Position = 12;
                                subWriter.Write((int)textEnd);

                                for (int i = 0; i < entity.Lines.Count; i++)
                                {
                                    var sub = entity.Lines[i];
                                    subWriter.Write(sub.SyncStart);
                                    subWriter.Write(sub.SyncEnd);
                                    subWriter.Write(sub.Unknown);
                                    subWriter.Write(sub.TextStart);
                                    subWriter.Write(sub.TextStart);
                                    subWriter.Write(sub.TextEnd);
                                    subWriter.Write(sub.TextEnd);
                                    subWriter.Write(sub.TextEnd);
                                    subWriter.Write(sub.TextEnd);
                                    subWriter.Write(sub.TextEnd);
                                }

                                subWriter.BaseStream.Position += (16 - subWriter.BaseStream.Position % 16) % 16;
                            }
                            writer.Write(msSub.ToArray());

                            var fill = (16 - writer.BaseStream.Position % 16) % 16;
                            for (int i = 0; i < fill; i++)
                            {
                                writer.Write((byte)0);
                            }
                            
                        }
                    }
                }

                File.WriteAllBytes(path, ms.ToArray());
            }
        }
        
        private static Regex _regex = new Regex("[\r\n]+");
        private string ConvertText(string text)
        {
            return _regex.Replace(text, "\\n");
        }

        private static void Print(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Write(string.Format("{0:X2} ", data[i]));
            }

            Debug.WriteLine("");
        }


        public class Entity
        {
            public Entity()
            {
                Lines = new List<Line>();
            }
            public List<Line> Lines { get; set; }
        }

        public class Line
        {
            [XmlAttribute]
            public short SyncStart { get; set; }
            [XmlAttribute]
            public short SyncEnd { get; set; }
            [XmlAttribute]
            public short Unknown { get; set; }
            [XmlIgnore]
            public short TextStart { get; set; }
            [XmlIgnore]
            public short TextEnd { get; set; }
            [XmlAttribute]
            public string Text { get; set; }
        }
    }

}
