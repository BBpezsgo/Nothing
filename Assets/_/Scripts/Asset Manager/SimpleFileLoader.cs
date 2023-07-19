using DataUtilities.FilePacker;
using DataUtilities.ReadableFileFormat;

using System;
using System.Collections.Generic;

namespace AssetManager
{
    public delegate IFile GetFile(string path);

    public static class Files
    {
        public static Value ProcessInheritance(Value current, IFile file, GetFile getFile, int maxInheritanceDepth)
            => ProcessInheritance(current, file, getFile, maxInheritanceDepth);

        static Value ProcessInheritance(Value current, IFile file, GetFile getFile, int maxInheritanceDepth, int currentDepth)
        {
            if (currentDepth > maxInheritanceDepth)
            {
                AssetLogger.LogWarning($"Inheritance exceeded max {currentDepth} in file {file.FullName}");
                return current;
            }
            if (!current.Has("Base")) return current;
            string basePath = current["Base"].String ?? "";
            if (string.IsNullOrWhiteSpace(basePath))
            {
                AssetLogger.LogWarning($"Invalid base value in file {file.FullName}:{current["Base"].Location}");
                return current;
            }
            IFile baseFile = getFile.Invoke(basePath + ".sdf");
            if (baseFile == null)
            {
                AssetLogger.LogWarning($"File {basePath} not found at {file.FullName}:{current["Base"].Location}");
                return current;
            }
            if (baseFile.FullName == file.FullName)
            {
                AssetLogger.LogWarning($"Base file is self in file {file.FullName}:{current["Base"].Location}");
                return current;
            }
            Value? @base = LoadFile(baseFile);
            if (!@base.HasValue) return current;
            Value baseValue = @base.Value;
            current.RemoveNode("Base");
            baseValue.Combine(current);
            return ProcessInheritance(baseValue, baseFile, getFile, maxInheritanceDepth, currentDepth + 1);
        }

        public static Value LoadFile(IFile file)
            => Parser.Parse(file.Text);

        public static Value[] LoadFiles(IFolder folder)
        {
            if (folder == null) return new Value[0];
            List<Value> result = new();
            var files = folder.Files;
            foreach (var file in files)
            {
                try
                {
                    Value v = Parser.Parse(file.Text);
                    result.Add(v);
                }
                catch (Exception)
                { }
            }
            return result.ToArray();
        }
        public static Value[] LoadFilesWithInheritacne(IFolder folder, GetFile getFile, int maxInheritanceDepth)
        {
            if (folder == null) return new Value[0];
            List<Value> result = new();
            var files = folder.Files;
            foreach (var file in files)
            {
                try
                {
                    Value v = LoadFileWithInheritacne(file, getFile, maxInheritanceDepth);
                    result.Add(v);
                }
                catch (Exception)
                { }
            }
            return result.ToArray();
        }

        public static Value LoadFileWithInheritacne(IFile file, GetFile getFile, int maxInheritanceDepth)
        {
            Value content = Parser.Parse(file.Text);
            Value result = ProcessInheritance(content, file, getFile, maxInheritanceDepth);
            return result;
        }
    }
}
