using System;
using System.IO;

namespace Utilities
{
    public interface ISerializable
    {
        public void Serialize(BinaryWriter writer);
        public void Deserialize(BinaryReader reader);
    }

    public static class Serializing
    {
        public static byte[] Serialize<T>(T v) where T : ISerializable
        {
            using MemoryStream memoryStream = new();
            using BinaryWriter writer = new(memoryStream);
            v.Serialize(writer);
            return memoryStream.ToArray();
        }

        public static void Serialize<T>(T v, Stream stream) where T : ISerializable
        {
            using BinaryWriter writer = new(stream);
            v.Serialize(writer);
        }

        public static void Deserialize<T>(T v, byte[] data) where T : ISerializable
        {
            using MemoryStream memoryStream = new(data, false);
            using BinaryReader reader = new(memoryStream);
            v.Deserialize(reader);
        }

        public static void Deserialize<T>(T v, Stream stream) where T : ISerializable
        {
            using BinaryReader reader = new(stream);
            v.Deserialize(reader);
        }

        public static T Deserialize<T>(byte[] data) where T : ISerializable
        {
            using MemoryStream memoryStream = new(data, false);
            using BinaryReader reader = new(memoryStream);
            T v = Activator.CreateInstance<T>();
            v.Deserialize(reader);
            return v;
        }

        public static T Deserialize<T>(Stream stream) where T : ISerializable
        {
            using BinaryReader reader = new(stream);
            T v = Activator.CreateInstance<T>();
            v.Deserialize(reader);
            return v;
        }
    }

    public static class SerializerExtensions
    {
        public static T[] ReadArray<T>(this BinaryReader reader, Func<T> itemReader)
        {
            int length = reader.ReadInt32();
            T[] result = new T[length];
            for (int i = 0; i < length; i++)
            { result[i] = itemReader.Invoke(); }
            return result;
        }

        public static void Write<T>(this BinaryWriter writer, T[] array, Action<T> itemWriter)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            { itemWriter.Invoke(array[i]); }
        }

        public static T ReadObj<T>(this BinaryReader reader) where T : ISerializable
        {
            T obj = Activator.CreateInstance<T>();
            obj.Deserialize(reader);
            return obj;
        }

        public static void Write<T>(this BinaryWriter writer, T obj) where T : ISerializable
        {
            obj.Serialize(writer);
        }
    }
}