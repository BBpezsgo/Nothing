using DataUtilities.ReadableFileFormat;

using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace AssetManager
{
    public static class AssetFileUtils
    {
        internal static FileInfo FindFile(string basePath, string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(searchPattern)) return null;

            if (File.Exists(searchPattern)) return new FileInfo(searchPattern);

            string combinedPath = Path.Combine(basePath, searchPattern);
            if (combinedPath != null)
            {
                if (File.Exists(combinedPath))
                { return new FileInfo(combinedPath); }
            }

            return GetFile(basePath, searchPattern);
        }

        internal static FileInfo FindAbsoluteFile(string basePath, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return null;

            if (File.Exists(filename)) return new FileInfo(filename);

            string combinedPath = Path.Combine(basePath, filename);
            if (combinedPath != null)
            {
                if (File.Exists(combinedPath))
                { return new FileInfo(combinedPath); }
            }

            return GetAbsoluteFile(basePath, filename);
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static Value[] LoadAllFiles(string basePath, string searchPattern)
        {
            FileInfo[] files = GetAllFiles(basePath, searchPattern);

            List<Value> result = new();

#if !PLATFORM_WEBGL
            for (int i = 0; i < files.Length; i++)
            { result.Add(Parser.Parse(File.ReadAllText(files[i].FullName))); }
#endif

            return result.ToArray();
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static Value[] LoadAllAbsoluteFiles(string basePath, string filename)
        {
            FileInfo[] files = GetAllAbsoluteFiles(basePath, filename);

            List<Value> result = new();

#if !PLATFORM_WEBGL
            for (int i = 0; i < files.Length; i++)
            { result.Add(Parser.Parse(File.ReadAllText(files[i].FullName))); }
#endif

            return result.ToArray();
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static FileInfo[] GetAllFiles(string basePath, string searchPattern)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.LogError($"Directory \"{basePath}\" does not exists!");
                return new FileInfo[0];
            }

            List<FileInfo> result = new();

            DirectoryInfo root = new(basePath);
            CollectMoreFiles(result, root, searchPattern);

            return result.ToArray();
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static IEnumerable<FileInfo> GetAllFilesEnumerable(string basePath, string searchPattern)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.LogError($"Directory \"{basePath}\" does not exists!");
                yield break;
            }

            DirectoryInfo root = new(basePath);
            var files = CollectMoreFilesEnumerable(root, searchPattern);
            foreach (var file in files) yield return file;
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static FileInfo[] GetAllAbsoluteFiles(string basePath, string filename)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.LogError($"Directory \"{basePath}\" does not exists!");
                return new FileInfo[0];
            }

            List<FileInfo> result = new();

            DirectoryInfo root = new(basePath);
            CollectMoreAbsoluteFiles(result, root, filename);

            return result.ToArray();
        }

        /// <exception cref="SingletonNotExistException{T}"></exception>
        internal static IEnumerable<FileInfo> GetAllAbsoluteFilesEnumerable(string basePath, string filename)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.LogError($"Directory \"{basePath}\" does not exists!");
                yield break;
            }

            DirectoryInfo root = new(basePath);
            var files = CollectMoreAbsoluteFilesEnumerable(root, filename);
            foreach (var file in files) yield return file;
        }

        static void CollectMoreFiles(List<FileInfo> list, DirectoryInfo directory, string searchPattern)
        {
            var folders = directory.EnumerateDirectories();
            list.AddRange(directory.EnumerateFiles(searchPattern));
            foreach (var folder in folders)
            { CollectMoreFiles(list, folder, searchPattern); }
        }

        static IEnumerable<FileInfo> CollectMoreFilesEnumerable(DirectoryInfo directory, string searchPattern)
        {
            var folders = directory.EnumerateDirectories();
            var files = directory.EnumerateFiles(searchPattern);
            foreach (var file in files) yield return file;
            foreach (var folder in folders)
            {
                var subfiles = CollectMoreFilesEnumerable(folder, searchPattern);
                foreach (var subfile in subfiles) yield return subfile;
            }
        }

        static void CollectMoreAbsoluteFiles(List<FileInfo> list, DirectoryInfo directory, string filename)
        {
            var folders = directory.GetDirectories();
            var files = directory.GetFiles();
            for (int i = 0; i < files.Length; i++)
            { if (files[i].Name == filename) list.Add(files[i]); }
            for (int i = 0; i < folders.Length; i++)
            { CollectMoreAbsoluteFiles(list, folders[i], filename); }
        }

        static IEnumerable<FileInfo> CollectMoreAbsoluteFilesEnumerable(DirectoryInfo directory, string filename)
        {
            var folders = directory.EnumerateDirectories();
            var files = directory.EnumerateFiles();
            foreach (var file in files) if (file.Name == filename) yield return file;
            foreach (var folder in folders)
            {
                var subfiles = CollectMoreAbsoluteFilesEnumerable(folder, filename);
                foreach (var subfile in subfiles) yield return subfile;
            }
        }

        internal static FileInfo GetFile(string basePath, string searchPattern)
        {
            var files = GetAllFilesEnumerable(basePath, searchPattern);
            foreach (var foundBaseFile in files) return foundBaseFile;
            return null;
        }

        internal static FileInfo GetAbsoluteFile(string basePath, string filename)
        {
            var files = GetAllAbsoluteFilesEnumerable(basePath, filename);
            foreach (var foundBaseFile in files) return foundBaseFile;
            return null;
        }
    }
}
