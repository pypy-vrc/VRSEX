using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace VRSEX
{
    public static class Utils
    {
        public static void Serialize(string path, object obj)
        {
            try
            {
                using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
                {
                    new BinaryFormatter().Serialize(file, obj);
                }
            }
            catch
            {
            }
        }

        public static bool Deserialize<T>(string path, ref T obj)
        {
            try
            {
                using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    obj = (T)new BinaryFormatter().Deserialize(file);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}