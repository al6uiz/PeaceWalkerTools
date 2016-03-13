using System;
using System.IO;
using System.Xml.Serialization;

namespace PeaceWalkerTools
{
    public class SerializationHelper
    {
        public static void Save<T>(T contents, string fileName) where T : class
        {
            SerializationHelper<T>.Save(contents, fileName);
        }
    }

    public class SerializationHelper<T> where T : class
    {
        private static XmlSerializer _serializer;

        static SerializationHelper()
        {
            try
            {
                _serializer = new XmlSerializer(typeof(T));
            }
            catch
            {

            }
        }

        public static T Read(string fileName)
        {
            using (TextReader reader = File.OpenText(fileName))
            {
                return _serializer.Deserialize(reader) as T;
            }
        }

        public static void SaveDefault(string fileName)
        {
            var instance = Activator.CreateInstance<T>();

            SetDefaultValue(instance);

            Save(instance, fileName);
        }

        private static void SetDefaultValue(object instance)
        {
            try
            {
                var type = instance.GetType();
                var fields = type.GetProperties();

                foreach (var field in fields)
                {
                    try
                    {
                        if (field.PropertyType == typeof(string))
                        {
                            if (field.GetValue(instance, null) == null)
                            {
                                field.SetValue(instance, string.Empty, null);
                            }
                        }
                        else if (!field.PropertyType.IsArray && !field.PropertyType.IsPrimitive)
                        {

                            var childInstance = Activator.CreateInstance(field.PropertyType);

                            SetDefaultValue(childInstance);

                            field.SetValue(instance, childInstance, null);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void Save(T contents, string fileName)
        {
            var location = Path.GetDirectoryName(Path.GetFullPath(fileName));

            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }


            using (var writer = new StreamWriter(fileName))
            {
                _serializer.Serialize(writer, contents);
            }
        }
    }
}