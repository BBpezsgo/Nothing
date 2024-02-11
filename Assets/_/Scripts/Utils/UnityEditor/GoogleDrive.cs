using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityGoogleDrive;
using File = UnityGoogleDrive.Data.File;

namespace Utilities.Editor
{
    public class GoogleDrive : EditorWindow
    {
        public const string DriveBuildFolderId = "1QpbJCXBCNpRTqGNOCAd3Dyt_J1YTJHyG";
        public const string DriveBuildFolderPath = "Nothing Game/Build/";

        public static readonly string[] DoNotTouch = new string[]
        {
            BuildTool.BaseOutputPath + "compress.bat",
            BuildTool.BaseOutputPath + "Game Hosting Server",
            BuildTool.BaseOutputPath + "Page Compiler",
        };

        enum TaskVisibility : int
        {
            Hidden,
            VisibleInContext,
            Visible,
        }

        class Task
        {
            public readonly TaskVisibility Visibility;
            public readonly string Name;
            public readonly GoogleDriveRequest RequestTask;
            public readonly System.Threading.Tasks.Task ThreadingTask;
            readonly double CreatedAt;

            public Texture SpinIcon
            {
                get
                {
                    double t = DateTime.UtcNow.TimeOfDay.TotalSeconds - CreatedAt;
                    int i = (int)t;
                    i = i < 0 ? 0 : i;
                    i %= GoogleDrive.LoadingIcon.Length;
                    return GoogleDrive.LoadingIcon[i];
                }
            }

            public bool IsRunning
            {
                get
                {
                    if (ThreadingTask != null)
                    {
                        return ThreadingTask.Status switch
                        {
                            TaskStatus.Created => true,
                            TaskStatus.Running => true,
                            TaskStatus.WaitingForActivation => true,
                            TaskStatus.WaitingForChildrenToComplete => true,

                            TaskStatus.Canceled => false,
                            TaskStatus.Faulted => false,
                            TaskStatus.RanToCompletion => false,
                            TaskStatus.WaitingToRun => false,
                            _ => false,
                        };
                    }

                    if (RequestTask != null)
                    {
                        return RequestTask.IsRunning;
                    }

                    return false;
                }
            }

            public float Progress
            {
                get
                {
                    if (RequestTask != null)
                    {
                        if (RequestTask.IsDone) return 1f;
                        return RequestTask.Progress;
                    }

                    if (ThreadingTask != null)
                    {
                        if (ThreadingTask.IsCompleted) return 1f;
                        return ThreadingTask.GetProgress();
                    }

                    return 0f;
                }
            }

            public bool IsDone
            {
                get
                {
                    if (ThreadingTask != null)
                    { return ThreadingTask.IsCompleted; }

                    if (RequestTask != null)
                    { return RequestTask.IsDone; }

                    return true;
                }
            }

            public bool IsCanceled
            {
                get
                {
                    if (ThreadingTask != null)
                    { return ThreadingTask.IsCanceled; }

                    if (RequestTask != null)
                    { return false; }

                    return false;
                }
            }
            public bool IsFaulted
            {
                get
                {
                    if (ThreadingTask != null)
                    { return ThreadingTask.IsFaulted; }

                    if (RequestTask != null)
                    { return RequestTask.IsError; }

                    return false;
                }
            }
            public Exception Exception
            {
                get
                {
                    if (ThreadingTask != null)
                    { return ThreadingTask.Exception; }

                    if (RequestTask != null)
                    { return null; }

                    return null;
                }
            }
            public string Error
            {
                get
                {
                    if (ThreadingTask != null)
                    { return ThreadingTask.Exception.Message; }

                    if (RequestTask != null)
                    { return RequestTask.Error; }

                    return null;
                }
            }

            public Task(string name, TaskVisibility visibility, System.Threading.Tasks.Task task)
            {
                Name = name;
                Visibility = visibility;
                ThreadingTask = task;
                CreatedAt = DateTime.UtcNow.TimeOfDay.TotalSeconds;
            }

            public Task(string name, TaskVisibility visibility, GoogleDriveRequest task)
            {
                Name = name;
                Visibility = visibility;
                RequestTask = task;
                CreatedAt = DateTime.UtcNow.TimeOfDay.TotalSeconds;
            }

            public override string ToString()
            {
                if (ThreadingTask != null)
                { return $"Thread {{ AsyncState: {ThreadingTask.AsyncState}, Status: {ThreadingTask.Status} }}"; }

                if (RequestTask != null)
                { return $"Request {{ {RequestTask.Method} {RequestTask.Uri} }}"; }

                return string.Empty;
            }
        }

        bool IsLoading => false;
        bool FirstTick = true;

        readonly Queue<GoogleDriveRequest> RequestQueue = new();

        readonly List<Task> Tasks = new();
        readonly List<(string, Task)> LocalFileTasks = new();
        readonly List<(string, Task)> CloudFileTasks = new();

        readonly List<File> CloudFiles = new();
        readonly List<FileSystemInfo> LocalFiles = new();

        static Texture[] LoadingIcon;

        bool ShouldRefreshCloudFiles = false;
        bool ShouldFullyRefreshCloudFiles = false;
        readonly List<string> ShouldGetCloudFiles = new();

        string LastError = null;

        [MenuItem("Tools/Google Drive")]
        public static void OnShow()
        {
            EditorWindow.GetWindow<GoogleDrive>("Google Drive", true);
        }

        static Texture BuildNameIcon(string name, bool isFile) => name switch
        {
            "StandaloneWindows" => EditorGUIUtility.IconContent("d_BuildSettings.Metro.Small", "Windows 32x").image,
            "StandaloneWindows-dev" => EditorGUIUtility.IconContent("d_BuildSettings.Metro.Small", "Windows 32x Dev").image,
            "StandaloneWindows64" => EditorGUIUtility.IconContent("d_BuildSettings.Metro.Small", "Windows 64x").image,
            "StandaloneWindows64-dev" => EditorGUIUtility.IconContent("d_BuildSettings.Metro.Small", "Windows 64x Dev").image,
            "Android" => EditorGUIUtility.IconContent("d_BuildSettings.Android.Small", "Android").image,
            "Android-dev" => EditorGUIUtility.IconContent("d_BuildSettings.Android.Small", "Android Dev").image,
            "WebGL" => EditorGUIUtility.IconContent("d_BuildSettings.WebGL.Small", "WebGL").image,
            "WebGL-dev" => EditorGUIUtility.IconContent("d_BuildSettings.WebGL.Small", "WebGL Dev").image,
            _ => EditorGUIUtility.IconContent(isFile ? "d_DefaultAsset Icon" : "d_Project").image,
        };

        static string BuildNameLabel(string name, string @default = null) => name switch
        {
            "StandaloneWindows" => "Windows 32x",
            "StandaloneWindows-dev" => "Windows 32x Dev",
            "StandaloneWindows64" => "Windows 64x",
            "StandaloneWindows64-dev" => "Windows 64x Dev",
            "Android" => "Android",
            "Android-dev" => "Android Dev",
            "WebGL" => "WebGL",
            "WebGL-dev" => "WebGL Dev",
            _ => @default,
        };

        void OnEnable()
        {
            titleContent = new GUIContent("Google Drive", EditorGUIUtility.IconContent("d_CloudConnect").image);
            LoadingIcon = new Texture[]
            {
                EditorGUIUtility.IconContent("d_WaitSpin00").image,
                EditorGUIUtility.IconContent("d_WaitSpin01").image,
                EditorGUIUtility.IconContent("d_WaitSpin02").image,
                EditorGUIUtility.IconContent("d_WaitSpin03").image,
                EditorGUIUtility.IconContent("d_WaitSpin04").image,
                EditorGUIUtility.IconContent("d_WaitSpin05").image,
                EditorGUIUtility.IconContent("d_WaitSpin06").image,
                EditorGUIUtility.IconContent("d_WaitSpin07").image,
                EditorGUIUtility.IconContent("d_WaitSpin08").image,
                EditorGUIUtility.IconContent("d_WaitSpin09").image,
                EditorGUIUtility.IconContent("d_WaitSpin10").image,
                EditorGUIUtility.IconContent("d_WaitSpin11").image,
            };
        }

        void Update()
        {
            if (!IsLoading &&
                ShouldFullyRefreshCloudFiles)
            {
                ShouldRefreshCloudFiles = false;
                ShouldFullyRefreshCloudFiles = false;
                GetCloudFilesWithDetails();
                EditorUtility.SetDirty(this);
            }

            if (!IsLoading &&
                ShouldRefreshCloudFiles)
            {
                ShouldRefreshCloudFiles = false;
                GetCloudFiles();
                EditorUtility.SetDirty(this);
            }

            if (!IsLoading &&
                ShouldGetCloudFiles.Count > 0)
            {
                string fileId = ShouldGetCloudFiles.PopOrDefault();
                GetCloudFile(fileId);
                EditorUtility.SetDirty(this);
            }

            if (!IsLoading &&
                RequestQueue.Count > 0)
            {
                bool canStart = true;

                if (canStart) foreach (var _task in Tasks)
                    {
                        if (!_task.IsRunning)
                        { continue; }
                        canStart = false;
                        break;
                    }

                if (canStart) foreach (var _task in LocalFileTasks)
                    {
                        if (!_task.Item2.IsRunning)
                        { continue; }
                        canStart = false;
                        break;
                    }

                if (canStart) foreach (var _task in CloudFileTasks)
                    {
                        if (!_task.Item2.IsRunning)
                        { continue; }
                        canStart = false;
                        break;
                    }

                if (canStart)
                {
                    GoogleDriveRequest task = RequestQueue.Dequeue();
                    task.SendNonGeneric();
                }
            }

            for (int i = Tasks.Count - 1; i >= 0; i--)
            {
                Task task = Tasks[i];

                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted");
                        Debug.LogException(task.Exception);
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted: {task.Error}");
                    }
                }

                if (task.IsCanceled)
                {
                    Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} cancelled");
                }

                if (task.IsDone)
                {
                    Tasks.RemoveAt(i);
                    EditorUtility.SetDirty(this);
                }
            }

            for (int i = LocalFileTasks.Count - 1; i >= 0; i--)
            {
                Task task = LocalFileTasks[i].Item2;

                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted");
                        Debug.LogException(task.Exception);
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted: {task.Error}");
                    }
                }

                if (task.IsCanceled)
                {
                    Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} cancelled");
                }

                if (task.IsDone)
                {
                    LocalFileTasks.RemoveAt(i);
                    EditorUtility.SetDirty(this);
                }
            }

            for (int i = CloudFileTasks.Count - 1; i >= 0; i--)
            {
                Task task = CloudFileTasks[i].Item2;

                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted");
                        Debug.LogException(task.Exception);
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} faulted: {task.Error}");
                    }
                }

                if (task.IsCanceled)
                {
                    Debug.LogError($"[{nameof(GoogleDrive)}]: Task {task} cancelled");
                }

                if (task.IsDone)
                {
                    CloudFileTasks.RemoveAt(i);
                    EditorUtility.SetDirty(this);
                }
            }

            if (FirstTick)
            {
                FirstTick = false;
                RefreshLocalFiles();
            }
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                Rect r1_ = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight), new GUIContent("Local"), EditorStyles.boldLabel);
                Vector2 r2_ = EditorStyles.linkLabel.CalcSize(new GUIContent("Refresh"));

                if (EditorGUI.LinkButton(new Rect(r1_.xMax - r2_.x, r1_.y, r2_.x, r1_.height), "Refresh"))
                {
                    LastError = null;
                    if (Directory.Exists(BuildTool.BaseOutputPath))
                    {
                        RefreshLocalFiles();
                        EditorUtility.SetDirty(this);
                    }
                }

                for (int i = 0; i < LocalFiles.Count; i++)
                {
                    FileSystemInfo file = LocalFiles[i];

                    Task fileTask = null;

                    foreach ((string, Task) task in LocalFileTasks)
                    {
                        if (task.Item1 == file.Name)
                        {
                            fileTask = task.Item2;
                            break;
                        }
                    }

                    GUIContent content = new(file.Name);

                    if (file is FileInfo)
                    {
                        content.image = BuildNameIcon(Path.GetFileNameWithoutExtension(file.Name), true);
                        content.text = BuildNameLabel(Path.GetFileNameWithoutExtension(file.Name), content.text);
                    }
                    else if (file is DirectoryInfo)
                    {
                        content.image = EditorGUIUtility.IconContent("d_Project").image;
                    }

                    Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    rect = EditorGUI.PrefixLabel(rect, content);
                    Rect subrect;

                    if (file is FileInfo fileInfo)
                    {
                        File cloudFile = null;
                        foreach (File item in CloudFiles)
                        {
                            if (item.Name != null && item.Name == fileInfo.Name)
                            {
                                cloudFile = item;
                            }
                        }

                        using (GUIUtils.Enabled(!IsLoading && fileTask is null))
                        {
                            content = EditorGUIUtility.IconContent("Update-Available", cloudFile == null ? "Upload" : "Override");
                            rect = rect.CutLeft(20, out subrect);

                            if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading && fileTask is null)
                            {
                                LastError = null;
                                if (cloudFile != null)
                                {
                                    File newCloudFile = new()
                                    {
                                        Content = System.IO.File.ReadAllBytes(file.FullName),
                                    };

                                    GoogleDriveFiles.UpdateRequest req = GoogleDriveFiles.Update(cloudFile.Id, newCloudFile);
                                    LocalFileTasks.Add((file.Name, new Task("Update File", TaskVisibility.VisibleInContext, req)));
                                    CloudFileTasks.Add((cloudFile.Id, new Task("Update File", TaskVisibility.Hidden, req)));
                                    RequestQueue.Enqueue(req);
                                }
                                else
                                {
                                    File newCloudFile = new()
                                    {
                                        Name = file.Name,
                                        Content = System.IO.File.ReadAllBytes(file.FullName),
                                        Parents = new List<string>()
                                        {
                                            DriveBuildFolderId,
                                        },
                                    };

                                    GoogleDriveFiles.CreateRequest req = GoogleDriveFiles.Create(newCloudFile);
                                    LocalFileTasks.Add((file.Name, new Task("Create File", TaskVisibility.VisibleInContext, req)));
                                    RequestQueue.Enqueue(req);
                                }
                            }

                            content = EditorGUIUtility.IconContent("d_TreeEditor.Trash", "Delete");
                            rect = rect.CutLeft(20, out subrect);

                            if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading && fileTask is null)
                            {
                                LastError = null;
                                DeleteLocalFile(file.FullName);
                                EditorUtility.SetDirty(this);
                            }

                            if (cloudFile == null)
                            {
                                content = EditorGUIUtility.IconContent("d_Linked", "Sync");
                                rect = rect.CutLeft(20, out subrect);

                                if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading && fileTask is null)
                                {
                                    LastError = null;
                                    var task = GetCloudFileByName(file.Name);
                                    LocalFileTasks.Add((file.Name, new Task("Get File", TaskVisibility.VisibleInContext, task)));
                                }
                            }
                        }
                    }
                    else if (file is DirectoryInfo directoryInfo)
                    {
                        using (GUIUtils.Enabled(fileTask is null))
                        {
                            content = EditorGUIUtility.IconContent("d_PrefabModel On Icon", "Compress");
                            rect = rect.CutLeft(20, out subrect);

                            if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading)
                            {
                                LastError = null;
                                System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(() =>
                                {
                                    string dest = Path.Combine(directoryInfo.Parent.FullName, directoryInfo.Name + ".zip");
                                    if (System.IO.File.Exists(dest))
                                    { System.IO.File.Delete(dest); }
                                    System.IO.Compression.ZipFile.CreateFromDirectory(directoryInfo.FullName, dest);
                                    RefreshLocalFiles();
                                    return System.Threading.Tasks.Task.CompletedTask;
                                });
                                LocalFileTasks.Add((file.Name, new Task("Compressing", TaskVisibility.VisibleInContext, task)));
                                LocalFileTasks.Add((file.Name + ".zip", new Task("Compressing", TaskVisibility.Hidden, task)));
                            }
                        }
                    }

                    if (fileTask is not null && fileTask.Visibility == TaskVisibility.VisibleInContext)
                    {
                        rect = rect.CutLeft(200f, out subrect);
                        DrawTask(subrect, fileTask);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                Rect r1_ = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight), new GUIContent("Cloud"), EditorStyles.boldLabel);
                Vector2 r2_ = EditorStyles.linkLabel.CalcSize(new GUIContent("Refresh"));

                using (GUIUtils.Enabled(!IsLoading))
                {
                    if (EditorGUI.LinkButton(new Rect(r1_.xMax - r2_.x, r1_.y, r2_.x, r1_.height), "Refresh") && !IsLoading)
                    {
                        LastError = null;
                        ShouldFullyRefreshCloudFiles = true;
                    }
                }

                for (int i = 0; i < CloudFiles.Count; i++)
                {
                    File file = CloudFiles[i];

                    Task fileTask = null;

                    foreach ((string, Task) task in CloudFileTasks)
                    {
                        if (task.Item1 == file.Id)
                        {
                            fileTask = task.Item2;
                            break;
                        }
                    }

                    GUIContent content = new(file.Name ?? file.Id ?? "null");

                    if (file.Kind.EndsWith("#file"))
                    {
                        content.image = BuildNameIcon(Path.GetFileNameWithoutExtension(file.Name), true);
                        content.text = BuildNameLabel(Path.GetFileNameWithoutExtension(file.Name), content.text);
                    }
                    else if (file.Kind.EndsWith("#folder"))
                    { content.image = EditorGUIUtility.IconContent("d_Project").image; }

                    Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    rect = EditorGUI.PrefixLabel(rect, content);

                    content = EditorGUIUtility.IconContent("d_Refresh", "Refresh");
                    rect = rect.CutLeft(20, out Rect subrect);

                    using (GUIUtils.Enabled(!IsLoading && fileTask is null))
                    {
                        if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading && fileTask is null)
                        {
                            LastError = null;
                            GetCloudFile(file.Id);
                            EditorUtility.SetDirty(this);
                        }
                    }

                    content = EditorGUIUtility.IconContent("d_TreeEditor.Trash", "Delete");
                    rect = rect.CutLeft(20, out subrect);

                    using (GUIUtils.Enabled(!IsLoading && fileTask is null))
                    {
                        if (GUI.Button(subrect, content, EditorStyles.iconButton) && !IsLoading && fileTask is null)
                        {
                            LastError = null;
                            DeleteCloudFile(file.Id);
                            EditorUtility.SetDirty(this);
                        }
                    }

                    content = new GUIContent("Open");
                    rect = rect.CutLeft(EditorStyles.linkLabel.CalcSize(content).x, out subrect);

                    if (EditorGUI.LinkButton(subrect, content))
                    {
                        Application.OpenURL($"https://drive.google.com/file/d/{file.Id}/view");
                    }

                    content = new GUIContent("Copy Download Link");
                    rect = rect.CutLeft(EditorStyles.linkLabel.CalcSize(content).x, out subrect);

                    if (EditorGUI.LinkButton(subrect, content))
                    {
                        EditorGUIUtility.systemCopyBuffer = GetDownloadLink(file.Id);
                    }

                    if (fileTask is not null && fileTask.Visibility == TaskVisibility.VisibleInContext)
                    {
                        rect = rect.CutLeft(200f, out subrect);
                        DrawTask(subrect, fileTask);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            if (!string.IsNullOrWhiteSpace(LastError))
            {
                EditorGUILayout.HelpBox(LastError, MessageType.Error, true);
            }

            {
                bool verticalBegan = false;

                for (int i = Tasks.Count - 1; i >= 0; i--)
                {
                    Task task = Tasks[i];

                    if ((int)task.Visibility < (int)TaskVisibility.Visible) continue;

                    if (!verticalBegan)
                    {
                        verticalBegan = true;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);
                    }

                    DrawTask(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight), task);
                }

                for (int i = LocalFileTasks.Count - 1; i >= 0; i--)
                {
                    Task task = LocalFileTasks[i].Item2;

                    if ((int)task.Visibility < (int)TaskVisibility.Visible) continue;

                    if (!verticalBegan)
                    {
                        verticalBegan = true;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);
                    }

                    DrawTask(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight), task);
                }

                for (int i = CloudFileTasks.Count - 1; i >= 0; i--)
                {
                    Task task = CloudFileTasks[i].Item2;

                    if ((int)task.Visibility < (int)TaskVisibility.Visible) continue;

                    if (!verticalBegan)
                    {
                        verticalBegan = true;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);
                    }

                    DrawTask(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight), task);
                }

                if (verticalBegan)
                { EditorGUILayout.EndVertical(); }
            }

            if (RequestQueue.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Request Queue", EditorStyles.boldLabel);
                foreach (GoogleDriveRequest request in RequestQueue)
                {
                    if (request.IsRunning)
                    {
                        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        EditorGUI.ProgressBar(rect, request.Progress, $"{request.Method} {request.Uri}");
                    }
                    else if (request.IsDone)
                    {
                        if (request.IsError)
                        {
                            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.IconContent("Error"));
                            EditorGUI.LabelField(rect, request.Error);
                        }
                    }
                    else
                    {
                        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(rect, $"{request.Method} {request.Uri}");
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawTask(Rect rect, Task task)
        {
            if (task.Visibility == TaskVisibility.Hidden) return;

            if (task.IsRunning)
            {
                EditorGUI.ProgressBar(rect, task.Progress, task.Name);
            }
            else
            {
                EditorGUI.LabelField(rect, new GUIContent("In Queue", task.SpinIcon));
            }
        }

        void RefreshLocalFiles()
        {
            LocalFiles.Clear();

            string[] folders = Directory.GetDirectories(BuildTool.BaseOutputPath);
            foreach (string folder in folders)
            {
                string path = Path.Combine(BuildTool.BaseOutputPath, folder);
                if (DoNotTouch.Contains(path)) continue;
                LocalFiles.Add(new DirectoryInfo(path));
            }

            string[] files = Directory.GetFiles(BuildTool.BaseOutputPath);
            foreach (string file in files)
            {
                string path = Path.Combine(BuildTool.BaseOutputPath, file);
                if (DoNotTouch.Contains(path)) continue;
                LocalFiles.Add(new FileInfo(path));
            }
        }

        public static byte[] ConvertToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static string ToBinary(byte[] data)
        {
            return string.Join(" ", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        void OnDriveList(Task<List<File>> task)
        {
            if (task.IsFaulted)
            { LastError = task.Exception.Message; }

            if (task.Result == null) return;

            CloudFiles.Clear();
            CloudFiles.AddRange(task.Result);

            foreach (File item in task.Result)
            {
                if (!string.IsNullOrEmpty(item.Name)) continue;

                if (!ShouldGetCloudFiles.Contains(item.Id))
                { ShouldGetCloudFiles.Add(item.Id); }
            }
        }

        void OnDriveListSimple(Task<List<File>> task)
        {
            if (task.IsFaulted)
            { LastError = task.Exception.Message; }

            if (task.Result == null) return;

            CloudFiles.Clear();
            CloudFiles.AddRange(task.Result);
        }

        System.Threading.Tasks.Task GetCloudFiles()
        {
            ShouldRefreshCloudFiles = false;

            var task = Helpers.FindFilesByPathAsync(DriveBuildFolderPath).ContinueWith(OnDriveListSimple);
            Tasks.Add(new Task("Get Files", TaskVisibility.Visible, task));
            return task;
        }

        System.Threading.Tasks.Task GetCloudFilesWithDetails()
        {
            ShouldRefreshCloudFiles = false;
            ShouldFullyRefreshCloudFiles = false;

            var task = Helpers.FindFilesByPathAsync(DriveBuildFolderPath).ContinueWith(OnDriveList);
            Tasks.Add(new Task("Get Files", TaskVisibility.Visible, task));
            return task;
        }

        GoogleDriveFiles.GetRequest GetCloudFile(string fileId)
        {
            var req = GoogleDriveFiles.Get(fileId);
            req.OnDone += OnCloudFileGet;
            CloudFileTasks.Add((fileId, new Task("Get File", TaskVisibility.VisibleInContext, req)));

            RequestQueue.Enqueue(req);
            return req;
        }

        System.Threading.Tasks.Task GetCloudFileByName(string name)
        {
            var task = Helpers.FindFilesByPathAsync(DriveBuildFolderPath).ContinueWith(_task =>
            {
                File result = _task.Result.Find(_file => _file.Name == name);
                OnCloudFileGet(result);
            });
            Tasks.Add(new Task("Get File", TaskVisibility.Visible, task));
            return task;
        }

        GoogleDriveFiles.DeleteRequest DeleteCloudFile(string fileId)
        {
            var req = GoogleDriveFiles.Delete(fileId);
            req.OnDone += OnCloudFileDelete;
            CloudFileTasks.Add((fileId, new Task("Delete File", TaskVisibility.VisibleInContext, req)));

            RequestQueue.Enqueue(req);
            return req;
        }

        void DeleteLocalFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                if (DoNotTouch.Contains(path)) return;
                System.IO.File.Delete(path);
            }

            for (int i = LocalFiles.Count - 1; i >= 0; i--)
            {
                if (LocalFiles[i].FullName == path)
                {
                    LocalFiles.RemoveAt(i);
                    break;
                }
            }
        }

        void OnCloudFileDelete(string obj)
        {
            ShouldRefreshCloudFiles = true;
        }

        void OnCloudFileGet(File file)
        {
            if (file == null) return;
            for (int i = 0; i < CloudFiles.Count; i++)
            {
                if (CloudFiles[i].Id == file.Id)
                {
                    CloudFiles[i] = file;
                    return;
                }
            }
            CloudFiles.Add(file);
        }

        public static string GetDownloadLink(string fileId) => $"https://drive.google.com/uc?export=download&id={fileId}";
    }
}