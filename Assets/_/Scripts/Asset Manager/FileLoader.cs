#if false && !UNITY_EDITOR && PLATFORM_WEBGL
#define DOWNLOAD_ASSETS
#endif

using DataUtilities.FilePacker;
using DataUtilities.ReadableFileFormat;
using DataUtilities.Serializer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

namespace AssetManager
{
    namespace Exceptions
    {
        [Serializable]
        public class NotLoadedException : Exception
        {
            public NotLoadedException() { }
            public NotLoadedException(string message) : base(message) { }
            public NotLoadedException(string message, Exception inner) : base(message, inner) { }
            protected NotLoadedException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public class NotSupportedException : Exception
        {
            public NotSupportedException() { }
            public NotSupportedException(string message) : base(message) { }
            public NotSupportedException(string message, Exception inner) : base(message, inner) { }
            protected NotSupportedException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }

    #region Real Things

    public class RealFile : IModifiableFile
    {
        readonly FileInfo file;
        public RealFile(FileInfo file) => this.file = file;

        public string Name => file.Name;
        public string FullName => file.FullName;

        public byte[] Bytes
        {
            get => File.ReadAllBytes(file.FullName);
            set => File.WriteAllBytes(file.FullName, value);
        }
        public string Text
        {
            get => File.ReadAllText(file.FullName);
            set => File.WriteAllText(file.FullName, value);
        }
    }
    public class RealFolder : IModifiableFolder
    {
        readonly DirectoryInfo folder;
        public RealFolder(DirectoryInfo folder) => this.folder = folder;

        public string Name => folder.Name;
        public string FullName => folder.FullName;

        public IEnumerable<IFile> Files => folder.EnumerateFiles().Select(v => new RealFile(v));
        public IEnumerable<IFolder> Folders => folder.EnumerateDirectories().Select(v => new RealFolder(v));

        public void AddFile(string file)
        {
            string path = Path.Combine(folder.FullName, file);
            if (File.Exists(path)) return;
            using FileStream stream = File.Create(path);
        }

        public void AddFolder(string folder)
        {
            string path = Path.Combine(this.folder.FullName, folder);
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }
    }

    #endregion

    #region HTTP Things

    public abstract class HttpThing : IFileOrFolder
    {
        public abstract string Name { get; }
        public abstract string FullName { get; }

        protected bool isDownloading;
        protected bool isUploading;

        public bool IsBusy => isDownloading || isUploading;

        protected abstract void Download();

        public System.Collections.IEnumerator WaitWhileBusy(float timeoutSec)
        {
            System.TimeSpan started = System.DateTime.UtcNow.TimeOfDay;
            while (!this.IsBusy)
            {
                if ((System.DateTime.UtcNow.TimeOfDay - started).TotalMilliseconds > timeoutSec)
                {
                    UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: 'WaitWhileBusy' exceed {timeoutSec}ms (\"{this.FullName}\")");
                    yield break;
                }
                yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
            }
            yield break;
        }

        public System.Collections.IEnumerator Preload()
        {
            Download();
            yield return WaitWhileBusy(2000);
            yield break;
        }

        protected virtual void OnDownloaded(UnityEngine.Networking.UnityWebRequest req)
        {
            isDownloading = false;

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            { UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: HTTP Error while downloading {req.uri}. Result: {req.result} Error: {req.error}"); }
            else if (req.responseCode != 200)
            { UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: HTTP {req.responseCode} while downloading {req.uri}"); }
        }
        protected virtual void OnUploaded(UnityEngine.Networking.UnityWebRequest req)
        {
            isUploading = false;

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            { UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: HTTP Error while uploading {req.uri}. Result: {req.result} Error: {req.error}"); }
            else if (req.responseCode != 200)
            { UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: HTTP {req.responseCode} while uploading {req.uri}"); }
        }
    }

    public class HttpFile : HttpThing, IModifiableFile
    {
        readonly HttpFolder Parent;
        readonly string name;
        byte[] content = null;

        public HttpFile(string name, HttpFolder parent)
        {
            this.name = name;
            this.Parent = parent;
        }

        protected override void OnDownloaded(UnityEngine.Networking.UnityWebRequest req)
        {
            base.OnDownloaded(req);

            content = req.downloadHandler.data;

            if (content == null)
            { UnityEngine.Debug.LogWarning($"[{nameof(HttpFile)}]: Failed to download file content {FullName}"); }
            else
            { if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFile)}]: Downloaded {Utils.ReadableSize(req.downloadHandler.data.Length)} from {req.url}"); }
        }

        protected override void Download()
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFile)}]: Can't download file: currently downloading (file: \"{FullName}\")");
                return;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFile)}]: Can't download file: currently uploading (file: \"{FullName}\")");
                return;
            }

            isDownloading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFile)}]: Downloading {FullName} ...");
            DownloadManager.Get(FullName, OnDownloaded);
        }
        void Upload(byte[] data)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFile)}]: Can't upload file: currently downloading (file: \"{FullName}\")");
                return;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFile)}]: Can't upload file: currently uploading (file: \"{FullName}\")");
                return;
            }

            isUploading = true;
            content = data;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFile)}]: Uploading {Utils.ReadableSize(data.Length)} {FullName} ...");
            DownloadManager.Put(FullName, data, OnUploaded);
        }

        public byte[] Bytes
        {
            get
            {
                if (content != null) return content;
                if (isDownloading) return null;
                Download();
                return content;
            }
            set
            {
                if (isDownloading)
                {
                    UnityEngine.Debug.LogError($"Can't upload file: currently downloading (file: \"{FullName}\")");
                    return;
                }
                if (isUploading)
                {
                    UnityEngine.Debug.LogError($"Can't upload file: currently uploading (file: \"{FullName}\")");
                    return;
                }
                Upload(value);
            }
        }

        public string Text
        {
            get => System.Text.Encoding.UTF8.GetString(Bytes);
            set => Bytes = System.Text.Encoding.UTF8.GetBytes(value);
        }

        public override string Name => name;

        public override string FullName => (Parent == null) ? name : (Parent.FullName + '\\' + name);
    }
    public class HttpFolder : HttpThing, IModifiableFolder
    {
        static string[] SplitText(string text)
        {
            var normalized = text.Trim().Replace("\r\n", "\n");
            if (normalized.Contains('\n'))
            { return normalized.Split('\n'); }
            if (normalized.Contains('\r'))
            { return normalized.Split('\r'); }
            if (normalized.Contains(';'))
            { return normalized.Split(';'); }
            return new string[] { normalized };
        }
        string[] rawContent;
        readonly string name;
        readonly HttpFolder Parent;
        HttpFile[] files;
        HttpFolder[] folders;

        public HttpFolder(string text, string name)
        {
            this.rawContent = SplitText(text);
            this.name = name;
        }
        public HttpFolder(string text, string name, HttpFolder parent)
        {
            this.rawContent = SplitText(text);
            this.name = name;
            this.Parent = parent;
        }

        HttpFolder(string name, HttpFolder parent)
        {
            this.rawContent = null;
            this.name = name;
            this.Parent = parent;
        }

        protected override void OnDownloaded(UnityEngine.Networking.UnityWebRequest req)
        {
            base.OnDownloaded(req);

            if (req.GetResponseHeader("Content-Type") == "text/plain")
            { rawContent = SplitText(req.downloadHandler.text); }
            else
            { UnityEngine.Debug.LogWarning($"[{nameof(HttpFolder)}]: Unknown content type for folder {FullName}: \'{req.GetResponseHeader("Content-Type")}\'"); }

            if (rawContent == null)
            { UnityEngine.Debug.LogWarning($"[{nameof(HttpFolder)}]: Failed to download folder content {FullName}"); }
            else
            { if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFile)}]: Downloaded {Utils.ReadableSize(req.downloadHandler.data.Length)} from {req.url}"); }
        }

        protected override void Download()
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't download folder: currently downloading (folder: \"{FullName}\")");
                return;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't download folder: currently uploading (folder: \"{FullName}\")");
                return;
            }

            isDownloading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Downloading {FullName} ...");
            DownloadManager.Get(FullName, OnDownloaded);
        }

        public void AddFile(string fileName)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently downloading (folder: \"{FullName}\")");
                return;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently uploading (folder: \"{FullName}\")");
                return;
            }

            isUploading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Uploading {fileName} to {FullName} ...");
            DownloadManager.Put(Path.Combine(FullName, fileName), "", OnUploaded);
            files = new List<HttpFile>(files)
            {
                new HttpFile(fileName, this)
            }.ToArray();
        }
        public void AddFolder(string folderName)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently downloading (folder: \"{FullName}\")");
                return;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently uploading (folder: \"{FullName}\")");
                return;
            }

            isUploading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Uploading {folderName} to {FullName} ...");
            DownloadManager.Put(Path.Combine(FullName, folderName), "", OnUploaded);
            folders = new List<HttpFolder>(folders)
            {
                new HttpFolder(folderName, this)
            }.ToArray();
        }

        public System.Collections.IEnumerator AddFileAsync(string fileName)
        {
            yield return AddFileAsync(fileName, "");
        }
        public System.Collections.IEnumerator AddFileAsync(string fileName, string data)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently downloading (folder: \"{FullName}\")");
                yield break;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently uploading (folder: \"{FullName}\")");
                yield break;
            }

            isUploading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Uploading {fileName} to {FullName} ...");

            UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Put(Path.Combine(FullName, fileName), data);
            yield return req.SendWebRequest();
            OnUploaded(req);
            files = new List<HttpFile>(files)
            {
                new HttpFile(fileName, this)
            }.ToArray();
            yield break;
        }
        public System.Collections.IEnumerator AddFileAsync(string fileName, byte[] data)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently downloading (folder: \"{FullName}\")");
                yield break;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently uploading (folder: \"{FullName}\")");
                yield break;
            }

            isUploading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Uploading {fileName} to {FullName} ...");

            UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Put(Path.Combine(FullName, fileName), data);
            yield return req.SendWebRequest();
            OnUploaded(req);
            files = new List<HttpFile>(files)
            {
                new HttpFile(fileName, this)
            }.ToArray();
            yield break;
        }
        public System.Collections.IEnumerator AddFolderAsync(string folderName)
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently downloading (folder: \"{FullName}\")");
                yield break;
            }
            if (isUploading)
            {
                UnityEngine.Debug.LogError($"[{nameof(HttpFolder)}]: Can't upload folder: currently uploading (folder: \"{FullName}\")");
                yield break;
            }

            isUploading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(HttpFolder)}]: Uploading {folderName} to {FullName} ...");

            UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Put(Path.Combine(FullName, folderName), "");
            yield return req.SendWebRequest();
            OnUploaded(req);
            folders = new List<HttpFolder>(folders)
            {
                new HttpFolder(folderName, this)
            }.ToArray();
            yield break;
        }

        public IEnumerable<IFile> Files
        {
            get
            {
                List<HttpFile> result = new();

                if (files != null) return files;

                if (isDownloading) return files;
                if (rawContent == null) Download();
                if (rawContent == null) return result;

                for (int i = 0; i < rawContent.Length; i++)
                {
                    string element = rawContent[i];
                    if (string.IsNullOrWhiteSpace(element)) continue;
                    if (!element.Contains('.')) continue;
                    result.Add(new HttpFile(element, this));
                }
                files = result.ToArray();
                return result;
            }
        }

        public IEnumerable<IFolder> Folders
        {
            get
            {
                List<HttpFolder> result = new();

                if (folders != null) return folders;

                if (rawContent == null) Download();
                if (rawContent == null) return result;

                for (int i = 0; i < rawContent.Length; i++)
                {
                    string element = rawContent[i];
                    if (string.IsNullOrWhiteSpace(element)) continue;
                    if (element.Contains('.')) continue;
                    result.Add(new HttpFolder(element, this));
                }
                folders = result.ToArray();
                return result;
            }
        }

        public override string Name => name;

        public override string FullName => (Parent == null) ? (name ?? "") : (Parent.FullName + '\\' + name);
    }

    #endregion

    #region Netcode Things


    public abstract class NetcodeThing : IFileOrFolder
    {
        public abstract string Name { get; }
        public abstract string FullName { get; }

        protected bool isDownloading;

        public bool IsBusy => isDownloading;


        float downloadProgress = 0f;
        internal float DownloadProgress => downloadProgress;

        internal void OnProgress(Network.ChunkCollector chunkCollector) => downloadProgress = chunkCollector.Progress;

        protected abstract void Download();

        public System.Collections.IEnumerator WaitWhileBusy(float timeoutSec)
        {
            System.TimeSpan started = System.DateTime.UtcNow.TimeOfDay;
            while (!this.IsBusy)
            {
                if ((System.DateTime.UtcNow.TimeOfDay - started).TotalMilliseconds > timeoutSec)
                {
                    UnityEngine.Debug.LogError($"[{nameof(HttpThing)}]: 'WaitWhileBusy' exceed {timeoutSec}ms (\"{this.FullName}\")");
                    yield break;
                }
                yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
            }
            yield break;
        }

        public System.Collections.IEnumerator Preload()
        {
            Download();
            yield return WaitWhileBusy(2000);
            yield break;
        }

        protected virtual void OnDownloaded(byte[] data)
        {
            downloadProgress = 1f;
            isDownloading = false;
        }
    }

    public class NetcodeFile : NetcodeThing, IFile
    {
        readonly NetcodeFolder Parent;
        readonly string name;
        byte[] content = null;

        public NetcodeFile(string name, NetcodeFolder parent)
        {
            this.name = name;
            this.Parent = parent;
        }

        protected override void OnDownloaded(byte[] data)
        {
            base.OnDownloaded(data);

            content = data;

            if (content == null)
            { UnityEngine.Debug.LogWarning($"[{nameof(NetcodeFile)}]: Failed to download file content {FullName}"); }
            else
            { if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(NetcodeFile)}]: Downloaded {Utils.ReadableSize(data.Length)}"); }
        }

        protected override void Download()
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(NetcodeFile)}]: Can't download file: currently downloading (file: \"{FullName}\")");
                return;
            }

            isDownloading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(NetcodeFile)}]: Downloading {FullName} ...");
            NetcodeSynchronizer.Instance.SendRequest(Unity.Netcode.NetworkManager.ServerClientId, "assets_file:" + FullName, OnProgress, OnDownloaded);
        }

        public byte[] Bytes
        {
            get
            {
                if (content != null) return content;
                if (isDownloading) return null;
                Download();
                return content;
            }
        }

        public string Text
        {
            get => System.Text.Encoding.UTF8.GetString(Bytes);
        }

        public override string Name => name;

        public override string FullName => (Parent == null) ? name : (Parent.FullName + '\\' + name);
    }
    public class NetcodeFolder : NetcodeThing, IFolder
    {
        static string[] SplitText(string text)
        {
            var normalized = text.Trim().Replace("\r\n", "\n");
            if (normalized.Contains('\n'))
            { return normalized.Split('\n'); }
            if (normalized.Contains('\r'))
            { return normalized.Split('\r'); }
            if (normalized.Contains(';'))
            { return normalized.Split(';'); }
            return new string[] { normalized };
        }
        string[] rawContent;
        readonly string name;
        readonly NetcodeFolder Parent;
        NetcodeFile[] files;
        NetcodeFolder[] folders;

        public NetcodeFolder(string text, string name)
        {
            this.rawContent = SplitText(text);
            this.name = name;
        }
        public NetcodeFolder(string text, string name, NetcodeFolder parent)
        {
            this.rawContent = SplitText(text);
            this.name = name;
            this.Parent = parent;
        }

        NetcodeFolder(string name, NetcodeFolder parent)
        {
            this.rawContent = null;
            this.name = name;
            this.Parent = parent;
        }

        protected override void OnDownloaded(byte[] data)
        {
            base.OnDownloaded(data);

            rawContent = SplitText(System.Text.Encoding.ASCII.GetString(data));

            if (rawContent == null)
            { UnityEngine.Debug.LogWarning($"[{nameof(NetcodeFolder)}]: Failed to download folder content {FullName}"); }
            else
            { if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(NetcodeFolder)}]: Downloaded {Utils.ReadableSize(data.Length)}"); }
        }

        protected override void Download()
        {
            if (isDownloading)
            {
                UnityEngine.Debug.LogError($"[{nameof(NetcodeFolder)}]: Can't download folder: currently downloading (folder: \"{FullName}\")");
                return;
            }

            isDownloading = true;
            if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(NetcodeFolder)}]: Downloading {FullName} ...");
            NetcodeSynchronizer.Instance.SendRequest(Unity.Netcode.NetworkManager.ServerClientId, "assets_folder:" + FullName, OnProgress, OnDownloaded);
        }

        public IEnumerable<IFile> Files
        {
            get
            {
                List<NetcodeFile> result = new();

                if (files != null) return files;

                if (isDownloading) return files;
                if (rawContent == null) Download();
                if (rawContent == null) return result;

                for (int i = 0; i < rawContent.Length; i++)
                {
                    string element = rawContent[i];
                    if (string.IsNullOrWhiteSpace(element)) continue;
                    if (!element.Contains('.')) continue;
                    result.Add(new NetcodeFile(element, this));
                }
                files = result.ToArray();
                return result;
            }
        }

        public IEnumerable<IFolder> Folders
        {
            get
            {
                List<NetcodeFolder> result = new();

                if (folders != null) return folders;

                if (rawContent == null) Download();
                if (rawContent == null) return result;

                for (int i = 0; i < rawContent.Length; i++)
                {
                    string element = rawContent[i];
                    if (string.IsNullOrWhiteSpace(element)) continue;
                    if (element.Contains('.')) continue;
                    result.Add(new NetcodeFolder(element, this));
                }
                folders = result.ToArray();
                return result;
            }
        }

        public override string Name => name;

        public override string FullName => (Parent == null) ? (name ?? "") : (Parent.FullName + '\\' + name);
    }

    #endregion

    public class FolderLoader
    {
        internal static readonly bool EnableDebugLogging = true;
        internal static readonly bool EnableNetworkLogging = true;

        public bool IsLoaded => rootFolder != null && isLoaded;
        bool isLoaded = false;

        IFolder rootFolder;
        public IFolder Root => rootFolder;

        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public System.Collections.IEnumerator LoadAsnyc(string basePath)
        { yield return LoadAsnyc(basePath, () => { }); }

        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal System.Collections.IEnumerator LoadAsnyc(string basePath, Action onDone, Action<Network.ChunkCollector> progress = null)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            { throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath)); }

            if (FolderLoader.EnableDebugLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Try loading root \"{basePath}\" asynchronously");

            if (basePath == "netcode")
            {
                if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Downloading full-assets asynchronously ...");

                byte[] data = null;
                yield return NetcodeSynchronizer.Instance.SendRequestAsync(Unity.Netcode.NetworkManager.ServerClientId, "assets:", progress, response =>
                {
                    data = response;
                });

                this.rootFolder = this.LoadPackedFolder(data);

                this.isLoaded = true;
                if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Full-Assets downloaded");

                onDone?.Invoke();
                yield break;
            }

            if (basePath.StartsWith("netcode://"))
            {
                if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Downloading root asynchronously ...");

                byte[] data = null;
                yield return NetcodeSynchronizer.Instance.SendRequestAsync(Unity.Netcode.NetworkManager.ServerClientId, "assets_folder:/", progress, response =>
                {
                    data = response;
                });

                this.rootFolder = new NetcodeFolder(System.Text.Encoding.ASCII.GetString(data), "/");

                this.isLoaded = true;
                if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Root downloaded");

                onDone?.Invoke();
                yield break;
            }

            if (basePath.StartsWith("http://"))
            {
                var req = UnityEngine.Networking.UnityWebRequest.Get(basePath);
                if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Downloading root asynchronously from {req.url}...");

                yield return req.SendWebRequest();

                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                { UnityEngine.Debug.LogError($"[{nameof(FolderLoader)}]: HTTP Error while downloading {req.uri}. Result: {req.result} Error: {req.error}\r\n{req.downloadHandler?.text}"); }
                else if (req.responseCode != 200)
                { UnityEngine.Debug.LogError($"[{nameof(FolderLoader)}]: HTTP {req.responseCode} while downloading {req.uri}\r\n{req.downloadHandler?.text}"); }
                else
                {
                    if (req.GetResponseHeader("Content-Type") == "text/plain")
                    {
                        string yeah = req.uri.ToString();
                        if (yeah.EndsWith('/')) yeah = yeah[..^1];
                        this.rootFolder = new HttpFolder(req.downloadHandler.text, yeah);
                    }
                    else
                    {
                        var data = req.downloadHandler.data;
                        this.rootFolder = this.LoadPackedFolder(data);
                    }
                    this.isLoaded = true;
                    if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Root downloaded from {req.uri}\nContent-Type: {req.GetResponseHeader("Content-Type")}");
                }

                onDone?.Invoke();
                yield break;
            }

#if PLATFORM_WEBGL
            UnityEngine.Debug.LogWarning($"[{nameof(FolderLoader)}]: The path \"{basePath}\" probably cannot be loaded into the browser. An http address must be specified.");
#endif

            if (Path.HasExtension(basePath) && Path.GetExtension(basePath).ToLower() == ".bin")
            {
                this.rootFolder = this.LoadPackedFolder(basePath);
                this.isLoaded = true;
                onDone?.Invoke();
                yield break;
            }

            if (Directory.Exists(basePath))
            {
                this.rootFolder = this.LoadRealFolder(basePath);
                this.isLoaded = true;
                onDone?.Invoke();
                yield break;
            }

            throw new NotImplementedException();
        }

        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static FolderLoader Load(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            { throw new ArgumentException($"[{nameof(FolderLoader)}]: '{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath)); }

            FolderLoader loader = new();
            if (FolderLoader.EnableDebugLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Try loading root \"{basePath}\"");

            if (basePath.StartsWith("http://"))
            {
                if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WebGLPlayer)
                { throw new NotSupportedException("Thread blocking HTTP request is not supported on WebGL."); }

                var req = UnityEngine.Networking.UnityWebRequest.Get(basePath);
                var task = req.SendWebRequest();
                while (!task.isDone)
                {
                    System.Threading.Thread.Sleep(50);
                    if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Downloading {UnityEngine.Mathf.Round(task.progress * 100f)}% ...");
                }

                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                { UnityEngine.Debug.LogError($"[{nameof(FolderLoader)}]: HTTP Error while downloading {req.uri}. Result: {req.result} Error: {req.error}\r\n{req.downloadHandler?.text}"); }
                else if (req.responseCode != 200)
                { UnityEngine.Debug.LogError($"[{nameof(FolderLoader)}]: HTTP {req.responseCode} while downloading {req.uri}\r\n{req.downloadHandler?.text}"); }
                else
                {
                    if (FolderLoader.EnableNetworkLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Root downloaded from {req.uri}\nContent-Type: {task.webRequest.GetResponseHeader("Content-Type")}");
                    if (task.webRequest.GetResponseHeader("Content-Type") == "text/plain")
                    {
#if !PLATFORM_WEBGL
                        string yeah = req.uri.ToString();
                        if (yeah.EndsWith('/')) yeah = yeah[..^1];
                        loader.rootFolder = new HttpFolder(task.webRequest.downloadHandler.text, yeah);
#else
                        throw new NotSupportedException($"HttpFolder and HttpFile is not supported on WebGL");
#endif
                    }
                    else
                    {
                        var data = task.webRequest.downloadHandler.data;
                        loader.rootFolder = loader.LoadPackedFolder(data);
                    }
                }
                loader.isLoaded = true;
                return loader;
            }

#if PLATFORM_WEBGL
            UnityEngine.Debug.LogWarning($"[{nameof(FolderLoader)}]: The path \"{basePath}\" probably cannot be loaded into the browser. An http address must be specified.");
#endif

            if (Path.HasExtension(basePath) && Path.GetExtension(basePath).ToLower() == ".bin")
            {
                loader.rootFolder = loader.LoadPackedFolder(basePath);
                loader.isLoaded = true;
                return loader;
            }

            if (Directory.Exists(basePath))
            {
                loader.rootFolder = loader.LoadRealFolder(basePath);
                loader.isLoaded = true;
                return loader;
            }

            throw new NotImplementedException();
        }

        VirtualFolder LoadPackedFolder(string packedFolderPath)
        {
            if (FolderLoader.EnableDebugLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Loading packed folder \"{packedFolderPath}\"");
            return VirtualUnpacker.Unpack(packedFolderPath);
        }
        VirtualFolder LoadPackedFolder(byte[] data)
        {
            if (FolderLoader.EnableDebugLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Loading packed folder ({Utils.ReadableSize(data.Length)})");
            return VirtualUnpacker.Unpack(data);
        }
        IFolder LoadRealFolder(string folderPath)
        {
            if (FolderLoader.EnableDebugLogging) UnityEngine.Debug.Log($"[{nameof(FolderLoader)}]: Loading real folder \"{folderPath}\"");
            return new RealFolder(new DirectoryInfo(folderPath));
        }

        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal Value[] LoadAllFiles(string searchPattern)
        {
            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            IFile[] files = GetAllFiles(searchPattern);

            List<Value> result = new();

            for (int i = 0; i < files.Length; i++)
            { result.Add(Parser.Parse(files[i].Text)); }

            return result.ToArray();
        }

        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IFile[] GetAllFiles(string searchPattern)
        {
            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            List<IFile> result = new();

            CollectMoreFiles(result, rootFolder, searchPattern);

            return result.ToArray();
        }

        void CollectMoreFiles(List<IFile> list, IFolder directory, string searchPattern)
        {
            IEnumerable<IFolder> folders = directory.Folders;
            list.AddRange(directory.Files.Where(v => v.Like(searchPattern)));
            foreach (IFolder folder in folders)
            { CollectMoreFiles(list, folder, searchPattern); }
        }

        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IEnumerable<IFile> GetAllFilesEnumerable(string searchPattern)
        {
            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            IEnumerable<IFile> files = CollectMoreFilesEnumerable(rootFolder, searchPattern);
            foreach (IFile file in files) yield return file;
        }

        IEnumerable<IFile> CollectMoreFilesEnumerable(IFolder directory, string searchPattern)
        {
            IEnumerable<IFolder> folders = directory.Folders;
            IEnumerable<IFile> files = directory.Files.Where(v => v.Like(searchPattern));
            foreach (IFile file in files) yield return file;
            foreach (IFolder folder in folders)
            {
                IEnumerable<IFile> subfiles = CollectMoreFilesEnumerable(folder, searchPattern);
                foreach (IFile subfile in subfiles) yield return subfile;
            }
        }

        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IEnumerable<IFile> GetAllFilesEnumerable()
        {
            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            IEnumerable<IFile> files = CollectMoreFilesEnumerable(rootFolder);
            foreach (IFile file in files) yield return file;
        }

        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IEnumerable<IFolder> GetAllFoldersEnumerable()
        {
            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            yield return rootFolder;
            IEnumerable<IFolder> folders = CollectMoreFoldersEnumerable(rootFolder);
            foreach (IFolder folder in folders) yield return folder;
        }

        IEnumerable<IFolder> CollectMoreFoldersEnumerable(IFolder directory)
        {
            IEnumerable<IFolder> folders = directory.Folders;
            foreach (IFolder folder in folders)
            {
                yield return folder;
                IEnumerable<IFolder> subfolders = CollectMoreFoldersEnumerable(folder);
                foreach (IFolder subfolder in subfolders) yield return subfolder;
            }
        }
        IEnumerable<IFile> CollectMoreFilesEnumerable(IFolder directory)
        {
            IEnumerable<IFolder> folders = directory.Folders;
            IEnumerable<IFile> files = directory.Files;
            foreach (IFile file in files) yield return file;
            foreach (IFolder folder in folders)
            {
                IEnumerable<IFile> subfiles = CollectMoreFilesEnumerable(folder);
                foreach (IFile subfile in subfiles) yield return subfile;
            }
        }
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IFile GetFile(string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            { throw new ArgumentException($"'{nameof(searchPattern)}' cannot be null or whitespace.", nameof(searchPattern)); }

            if (!IsLoaded) throw new Exceptions.NotLoadedException();

            IEnumerable<IFile> files = GetAllFilesEnumerable(searchPattern);
            foreach (IFile foundBaseFile in files) return foundBaseFile;
            return null;
        }
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IFile GetAbsoluteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            { throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path)); }
            if (!IsLoaded) return null;
            IEnumerable<IFile> files = GetAllFilesEnumerable();
            string normalizedPath = path.Replace('\\', '/');

            foreach (var _file in files)
            {
                if (_file.FullName.Replace('\\', '/') == normalizedPath)
                { return _file; }
            }

            try
            {
                foreach (var _file in files)
                {
                    string relativePath = Path.GetRelativePath(rootFolder.FullName, _file.FullName);
                    if (relativePath == normalizedPath)
                    { return _file; }
                }
            }
            catch (ArgumentNullException)
            { return null; }

            return null;
        }
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exceptions.NotLoadedException"></exception>
        internal IFolder GetAbsoluteFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            { throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path)); }
            if (!IsLoaded) return null;
            IEnumerable<IFolder> folders = GetAllFoldersEnumerable();
            string normalizedPath = path.Replace('\\', '/');
            foreach (var _folder in folders)
            {
                string folderNormalizedPath = _folder.FullName.Replace('\\', '/');
                if (_folder is RealFolder)
                {
                    folderNormalizedPath = folderNormalizedPath[rootFolder.FullName.Length..];
                    if (folderNormalizedPath.Length == 0) folderNormalizedPath = "/";
                    if (folderNormalizedPath == normalizedPath)
                    { return _folder; }
                    continue;
                }
                if (folderNormalizedPath == normalizedPath)
                { return _folder; }
            }

            try
            {
                foreach (var _folder in folders)
                {
                    string folderNormalizedPath = _folder.FullName.Replace('/', '\\');
                    string relativePath = Path.GetRelativePath(rootFolder.FullName, folderNormalizedPath);
                    if (relativePath.Replace('\\', '/') == normalizedPath)
                    { return _folder; }
                }
            }
            catch (ArgumentNullException)
            { return null; }

            return null;
        }

        /*
        /// <exception cref="NotLoadedException"></exception>
        internal Value[] LoadAllAbsoluteFiles(string filename)
        {
            if (!IsLoaded) throw new NotLoadedException();

            IFile[] files = GetAllAbsoluteFiles(filename);

            List<Value> result = new();

            for (int i = 0; i < files.Length; i++)
            { result.Add(Parser.Parse(files[i].Text)); }

            return result.ToArray();
        }
        */

        /*
        /// <exception cref="NotLoadedException"></exception>
        internal IFile[] GetAllAbsoluteFiles(string filename)
        {
            if (!IsLoaded) throw new NotLoadedException();

            List<IFile> result = new();

            CollectMoreAbsoluteFiles(result, RootFolder, filename);

            return result.ToArray();
        }
        */

        /*
        /// <exception cref="NotLoadedException"></exception>
        internal IEnumerable<IFile> GetAllAbsoluteFilesEnumerable(string filename)
        {
            if (!IsLoaded) throw new NotLoadedException();

            var files = CollectMoreAbsoluteFilesEnumerable(RootFolder, filename);
            foreach (var file in files) yield return file;
        }
        */

        /*
        void CollectMoreAbsoluteFiles(List<IFile> list, IFolder directory, string filename)
        {
            var folders = directory.Folders;
            var files = directory.Files;

            foreach (var file in files)
            { if (file.Name == filename) list.Add(file); }

            foreach (var folder in folders)
            { CollectMoreAbsoluteFiles(list, folder, filename); }
        }
        */

        /*
        IEnumerable<IFile> CollectMoreAbsoluteFilesEnumerable(IFolder directory, string filename)
        {
            var folders = directory.Folders;
            var files = directory.Files;

            foreach (var file in files) if (file.Name == filename) yield return file;

            foreach (var folder in folders)
            {
                var subfiles = CollectMoreAbsoluteFilesEnumerable(folder, filename);
                foreach (var subfile in subfiles) yield return subfile;
            }
        }
        */

        /*
        /// <exception cref="NotLoadedException"></exception>
        internal IFile GetAbsoluteFile(string filename)
        {
            if (!IsLoaded) throw new NotLoadedException();

            var files = GetAllAbsoluteFilesEnumerable(filename);
            foreach (var foundBaseFile in files) return foundBaseFile;
            return null;
        }
        */
    }

    public static class FileLoader
    {
        static bool GetOrCreateFile(string basePath, string filename, out IModifiableFile result)
        {
            string path = Path.Combine(basePath, filename);

            FolderLoader loader = FolderLoader.Load(basePath);
            IFile file = loader.GetAbsoluteFile(path);

            if (file is null)
            {
                if (loader.Root is not IModifiableFolder modifiableFolder)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(FileLoader)}]: Can't create file: folder is readonly (file: \"{file.FullName}\")");
                    result = null;
                    return false;
                }
                modifiableFolder.AddFile(filename);
            }

            file = loader.GetAbsoluteFile(path);
            if (file is null)
            {
                UnityEngine.Debug.LogError($"[{nameof(FileLoader)}]: Failed to create file (file: \"{filename}\", folder: \"{basePath}\")");
                result = null;
                return false;
            }

            if (file is not IModifiableFile modifiableFile)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(FileLoader)}]: Can't modify file: it is readonly (file: \"{file.FullName}\")");
                result = null;
                return false;
            }

            result = modifiableFile;
            return true;
        }
        static System.Collections.IEnumerator GetOrCreateFile(string basePath, string filename, System.Action<bool, IModifiableFile> callback)
        {
            string path = Path.Combine(basePath, filename);

            FolderLoader loader = new();
            yield return loader.LoadAsnyc(basePath);

            if (loader.Root is HttpFolder httpFolder)
            { yield return httpFolder.Preload(); }

            IFile file = loader.GetAbsoluteFile(path);

            if (file is null)
            {
                if (loader.Root is not IModifiableFolder modifiableFolder)
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(FileLoader)}]: Can't create file: folder is readonly (file: \"{file.FullName}\")");
                    callback?.Invoke(false, null);
                    yield break;
                }
                if (file is HttpFolder httpFolder1)
                { yield return httpFolder1.AddFileAsync(filename); }
                else
                { modifiableFolder.AddFile(filename); }
            }

            file = loader.GetAbsoluteFile(path);
            if (file is null)
            {
                UnityEngine.Debug.LogError($"[{nameof(FileLoader)}]: Failed to create file (file: \"{filename}\", folder: \"{basePath}\")");
                callback?.Invoke(false, null);
                yield break;
            }

            if (file is not IModifiableFile modifiableFile)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(FileLoader)}]: Can't modify file: it is readonly (file: \"{file.FullName}\")");
                callback?.Invoke(false, null);
                yield break;
            }

            callback?.Invoke(true, modifiableFile);
            yield break;
        }

        #region Save byte[]
        public static bool Save(string basePath, string filename, byte[] data)
        {
            if (!GetOrCreateFile(basePath, filename, out IModifiableFile file)) return false;

            file.Bytes = data;
            return true;
        }
        public static System.Collections.IEnumerator SaveAsync(string basePath, string filename, byte[] data, System.Action<bool> callback)
        {
            bool success = false;
            IModifiableFile file = null;
            yield return GetOrCreateFile(basePath, filename, (_success, _file) =>
            {
                success = _success;
                file = _file;
            });

            if (!success)
            {
                callback?.Invoke(false);
                yield break;
            }

            file.Bytes = data;

            if (file is HttpFile httpFile)
            { yield return httpFile.WaitWhileBusy(4000); }

            callback?.Invoke(true);
            yield break;
        }
        #endregion

        #region Save string
        public static bool Save(string basePath, string filename, string data)
        {
            if (!GetOrCreateFile(basePath, filename, out IModifiableFile file)) return false;

            file.Text = data;
            return true;
        }
        public static System.Collections.IEnumerator SaveAsync(string basePath, string filename, string data, System.Action<bool> callback)
        {
            bool success = false;
            IModifiableFile file = null;
            yield return GetOrCreateFile(basePath, filename, (_success, _file) =>
            {
                success = _success;
                file = _file;
            });

            if (!success)
            {
                callback?.Invoke(false);
                yield break;
            }

            file.Text = data;

            if (file is HttpFile httpFile)
            { yield return httpFile.WaitWhileBusy(4000); }

            callback?.Invoke(true);
            yield break;
        }
        #endregion

        #region Save ISerializable
        public static bool Save<T>(string basePath, string filename, DataUtilities.Serializer.ISerializable<T> data) where T : DataUtilities.Serializer.ISerializable<T>
        {
            if (!GetOrCreateFile(basePath, filename, out IModifiableFile file)) return false;

            DataUtilities.Serializer.Serializer serializer = new();
            serializer.Serialize<T>(data);
            file.Bytes = serializer.Result;
            return true;
        }
        public static System.Collections.IEnumerator SaveAsync<T>(string basePath, string filename, DataUtilities.Serializer.ISerializable<T> data, System.Action<bool> callback) where T : DataUtilities.Serializer.ISerializable<T>
        {
            bool success = false;
            IModifiableFile file = null;
            yield return GetOrCreateFile(basePath, filename, (_success, _file) =>
            {
                success = _success;
                file = _file;
            });

            if (!success)
            {
                callback?.Invoke(false);
                yield break;
            }

            DataUtilities.Serializer.Serializer serializer = new();
            serializer.Serialize<T>(data);
            file.Bytes = serializer.Result;

            if (file is HttpFile httpFile)
            { yield return httpFile.WaitWhileBusy(4000); }

            callback?.Invoke(true);
            yield break;
        }
        #endregion

        #region Save ISerializableText
        public static bool Save(string basePath, string filename, DataUtilities.ReadableFileFormat.ISerializableText data, bool minimal = false)
        {
            if (!GetOrCreateFile(basePath, filename, out IModifiableFile file)) return false;

            file.Text = data.SerializeText().ToSDF(minimal);
            return true;
        }
        public static System.Collections.IEnumerator SaveAsync(string basePath, string filename, DataUtilities.ReadableFileFormat.ISerializableText data, System.Action<bool> callback, bool minimal = false)
        {
            bool success = false;
            IModifiableFile file = null;
            yield return GetOrCreateFile(basePath, filename, (_success, _file) =>
            {
                success = _success;
                file = _file;
            });

            if (!success)
            {
                callback?.Invoke(false);
                yield break;
            }

            file.Text = data.SerializeText().ToSDF(minimal);

            if (file is HttpFile httpFile)
            { yield return httpFile.WaitWhileBusy(4000); }

            callback?.Invoke(true);
            yield break;
        }
        #endregion

        public static IFile Load(string basePath, string filename)
        {
            string path = Path.Combine(basePath, filename);

            FolderLoader loader = FolderLoader.Load(basePath);
            IFile file = loader.GetAbsoluteFile(path);
            return file;
        }
        public static System.Collections.IEnumerator LoadAsync(string basePath, string filename, System.Action<IFile> callback)
        {
            string path = Path.Combine(basePath, filename);

            FolderLoader loader = new();
            yield return loader.LoadAsnyc(basePath);

            if (loader.Root is HttpFolder httpFolder1)
            { yield return httpFolder1.Preload(); }

            IFile file = loader.GetAbsoluteFile(path);

            callback?.Invoke(file);
            yield break;
        }

        public static byte[] LoadBinary(string basePath, string filename)
        {
            IFile file = Load(basePath, filename);
            byte[] result = file.Bytes;
            return result;
        }
        public static System.Collections.IEnumerator LoadBinaryAsync(string basePath, string filename, System.Action<byte[]> callback)
        {
            IFile file = null;
            yield return LoadAsync(basePath, filename, (_file) =>
            {
                file = _file;
            });

            if (file == null)
            {
                callback?.Invoke(null);
                yield break;
            }

            if (file is HttpFile httpFile)
            { yield return httpFile.Preload(); }

            byte[] result = file.Bytes;
            callback?.Invoke(result);
            yield break;
        }

        public static T LoadBinary<T>(string basePath, string filename) where T : DataUtilities.Serializer.ISerializable<T>
        {
            byte[] data = LoadBinary(basePath, filename);
            DataUtilities.Serializer.Deserializer deserializer = new(data);
            T result = deserializer.DeserializeObject<T>();
            return result;
        }
        public static System.Collections.IEnumerator LoadBinaryAsync<T>(string basePath, string filename, System.Action<T> callback) where T : DataUtilities.Serializer.ISerializable<T>
        {
            byte[] data = null;
            yield return LoadBinaryAsync(basePath, filename, (_data) =>
            {
                data = _data;
            });

            DataUtilities.Serializer.Deserializer deserializer = new(data);
            T result = deserializer.DeserializeObject<T>();
            callback?.Invoke(result);
            yield break;
        }

        public static string LoadText(string basePath, string filename)
        {
            IFile file = Load(basePath, filename);
            string result = file.Text;
            return result;
        }
        public static System.Collections.IEnumerator LoadTextAsync(string basePath, string filename, System.Action<string> callback)
        {
            IFile file = null;
            yield return LoadAsync(basePath, filename, (_file) =>
            {
                file = _file;
            });

            if (file == null)
            {
                callback?.Invoke(null);
                yield break;
            }

            if (file is HttpFile httpFile)
            { yield return httpFile.Preload(); }

            string result = file.Text;
            callback?.Invoke(result);
            yield break;
        }

        public static T LoadSDF<T>(string basePath, string filename) where T : DataUtilities.ReadableFileFormat.IDeserializableText
        {
            IFile file = Load(basePath, filename);
            string data = file.Text;
            DataUtilities.ReadableFileFormat.Value parsed = DataUtilities.ReadableFileFormat.Parser.Parse(data);
            T result = parsed.Deserialize<T>();
            return result;
        }
        public static System.Collections.IEnumerator LoadSDFAsync<T>(string basePath, string filename, System.Action<T> callback) where T : DataUtilities.ReadableFileFormat.IDeserializableText
        {
            string data = null;
            yield return LoadTextAsync(basePath, filename, (_data) =>
            {
                data = _data;
            });

            DataUtilities.ReadableFileFormat.Value parsed = DataUtilities.ReadableFileFormat.Parser.Parse(data);
            T result = parsed.Deserialize<T>();
            callback?.Invoke(result);
            yield break;
        }

        public static bool IsExists(string basePath, string filename) => Load(basePath, filename) != null;
        public static System.Collections.IEnumerator IsExistsAsync(string basePath, string filename, System.Action<bool> callback)
        {
            IFile file = null;
            yield return LoadAsync(basePath, filename, (_file) =>
            {
                file = _file;
            });

            callback?.Invoke(file != null);
            yield break;
        }
        public static System.Collections.IEnumerator IsExistsAsync(string basePath, string filename, System.Action callback)
        {
            IFile file = null;
            yield return LoadAsync(basePath, filename, (_file) =>
            {
                file = _file;
            });

            if (file != null) callback?.Invoke();
            yield break;
        }
    }

    public static class Storage
    {
        static string sessionId;

        public static string SessionID
        {
            get
            {
                if (sessionId == null)
                {
#if UNITY_EDITOR
                    sessionId = "editor";
#else
                    sessionId = "temp-" + System.Guid.NewGuid().ToString();
#endif
                }
                return System.Uri.EscapeUriString(sessionId);
            }
            set => sessionId = value;
        }

        public static string Path
        {
#if PLATFORM_WEBGL && DOWNLOAD_ASSETS
            get => "http://192.168.1.100:7777/storage" + (SessionID == null ? "" : ("/" + SessionID));
#else
            get => UnityEngine.Application.persistentDataPath;
#endif
        }

        static void CreatePath(string path)
        {
            path = path.Replace('\\', '/');
            var file = path.Split('/')[^1];
            var folder = path[..(path.Length - file.Length - 1)];
            if (!Directory.Exists(folder))
            { Directory.CreateDirectory(folder); }
        }

        public static void Write(string data, params string[] path)
        {
            string _path = GetPath(path);

            CreatePath(_path);
            File.WriteAllText(_path, data);
        }

        public static void Write(byte[] data, params string[] path)
        {
            string _path = GetPath(path);

            CreatePath(_path);
            File.WriteAllBytes(_path, data);
        }

        public static void Write<T>(ISerializable<T> data, params string[] path)
        {
            string _path = GetPath(path);

            Serializer serializer = new();
            data.Serialize(serializer);
            Write(serializer.Result, _path);
        }

        public static string[] GetFiles(params string[] path)
        {
            string _path = GetPath(path);

            if (!Directory.Exists(_path))
            { return new string[0]; }

            return Directory.GetFiles(_path);
        }

        public static string ReadString(params string[] path)
        {
            string _path = GetPath(path);

            if (!File.Exists(_path))
            { return null; }
            return File.ReadAllText(_path);
        }

        public static byte[] ReadBytes(params string[] path)
        {
            string _path = GetPath(path);

            if (!File.Exists(_path))
            { return null; }
            return File.ReadAllBytes(_path);
        }

        public static T ReadObject<T>(params string[] path) where T : ISerializable<T>
        {
            string _path = GetPath(path);

            if (!File.Exists(_path))
            { return default; }
            byte[] bytes = ReadBytes(_path);
            Deserializer deserializer = new(bytes);
            return deserializer.DeserializeObject<T>();
        }

        static string GetPath(string path)
        {
            if (!System.IO.Path.IsPathFullyQualified(path))
            { return System.IO.Path.Combine(Path, path).Replace('\\', '/'); }
            else
            { return path.Replace('\\', '/'); }
        }

        static string GetPath(params string[] paths)
        {
            if (paths.Length == 0) return Path.Replace('\\', '/');
            if (System.IO.Path.IsPathFullyQualified(paths[0]))
            {
                return System.IO.Path.Combine(paths).Replace('\\', '/');
            }
            else
            {
                List<string> newPath = new();
                newPath.Add(Path);
                newPath.AddRange(paths);
                return System.IO.Path.Combine(newPath.ToArray()).Replace('\\', '/');
            }
        }
    }
}