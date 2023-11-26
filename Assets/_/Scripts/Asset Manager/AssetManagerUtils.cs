using DataUtilities.ReadableFileFormat;

using System;

namespace AssetManager
{
    [Serializable]
    public class SingletonNotExistException<T> : Exception
    {
        public SingletonNotExistException() : base($"Singleton {typeof(T)} does not exist!") { }
    }
}
