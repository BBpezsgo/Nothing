using System;
using System.Collections.Generic;
using System.Linq;
using BoxLayout;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using UnityEngine;
using UnityEngine.Networking;
using Color = UnityEngine.Color;

namespace InGameComputer
{
    [Serializable]
    public class ProgramWebBrowser : Program
    {
        class DownloadTask : IDisposable
        {
            public delegate void CompletedEvent<T>(DownloadTask sender, T data);

            public readonly UnityWebRequest Request;
            public readonly UnityWebRequestAsyncOperation CurrentWebRequestOperation;
            public readonly DownloadHandler DownloadHandler;
            public readonly UploadHandler UploadHandler;

            public readonly Uri Uri;

            readonly CompletedEvent<string> CallbackText;
            readonly CompletedEvent<byte[]> CallbackBinary;
            readonly CompletedEvent<Texture2D> CallbackImage;

            bool CallbackInvoked;
            bool _RequestSent;
            public bool RequestSent => _RequestSent;
            public bool IsDone => CallbackInvoked;

            public float DownloadProgress => Request?.downloadProgress ?? -1;
            public float UploadProgress => Request?.uploadProgress ?? -1;

            DownloadTask(UnityWebRequest request, Uri uri)
            {
                Request = request;
                DownloadHandler = Request.downloadHandler;
                UploadHandler = Request.uploadHandler;
                CallbackInvoked = false;
                _RequestSent = false;
                Uri = uri;
            }

            public DownloadTask(UnityWebRequest request, Uri uri, CompletedEvent<string> callback, bool autoStart)
                : this(request, uri)
            {
                CallbackText = callback;

                if (autoStart)
                { SendWebRequest(); }
            }

            public DownloadTask(UnityWebRequest request, Uri uri, CompletedEvent<byte[]> callback, bool autoStart)
                : this(request, uri)
            {
                CallbackBinary = callback;

                if (autoStart)
                { SendWebRequest(); }
            }

            public DownloadTask(UnityWebRequest request, Uri uri, CompletedEvent<Texture2D> callback, bool autoStart)
                : this(request, uri)
            {
                CallbackImage = callback;

                if (autoStart)
                { SendWebRequest(); }
            }

            public void SendWebRequest()
            {
                if (_RequestSent) return;
                if (Request == null) return;

                Debug.Log($"[{nameof(DownloadTask)}]: Downloading {Uri}");
                Request.SendWebRequest();
                _RequestSent = true;
            }

            public void Tick()
            {
                if (!(Request?.isDone ?? false)) return;
                if (CallbackInvoked) return;
                if (DownloadHandler != null && !DownloadHandler.isDone) return;

                if (CallbackText != null)
                {
                    CallbackText.Invoke(this, DownloadHandler?.text);
                }
                else if (CallbackBinary != null)
                {
                    CallbackBinary.Invoke(this, DownloadHandler?.data);
                }
                else if (CallbackImage != null)
                {
                    if (DownloadHandler is DownloadHandlerTexture downloadHandler)
                    {
                        CallbackImage.Invoke(this, downloadHandler.texture);
                    }
                }

                CallbackInvoked = true;
            }

            static UnityWebRequest InitializeRequest(UnityWebRequest request)
            {
                request.timeout = 5;

                UnityWebRequest.ClearCookieCache();

                request.SetRequestHeader("user-agent", "Bruh");

                return request;
            }

            public static DownloadTask Get(Uri uri, CompletedEvent<Texture2D> callback, bool autoStart = true)
            {
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri, true);
                DownloadTask.InitializeRequest(request);

                return new DownloadTask(request, uri, callback, autoStart);
            }

            public static DownloadTask Get(Uri uri, CompletedEvent<string> callback, bool autoStart = true)
            {
                UnityWebRequest request = UnityWebRequest.Get(uri);
                DownloadTask.InitializeRequest(request);

                return new DownloadTask(request, uri, callback, autoStart);
            }

            public static DownloadTask Get(Uri uri, CompletedEvent<byte[]> callback, bool autoStart = true)
            {
                UnityWebRequest request = UnityWebRequest.Get(uri);
                DownloadTask.InitializeRequest(request);

                return new DownloadTask(request, uri, callback, autoStart);
            }

            public static DownloadTask Post(Uri uri, Dictionary<string, string> form, CompletedEvent<string> callback, bool autoStart = true)
            {
                UnityWebRequest request = UnityWebRequest.Post(uri, form);
                DownloadTask.InitializeRequest(request);

                return new DownloadTask(request, uri, callback, autoStart);
            }

            public void Dispose()
            {
                Request?.Dispose();
                DownloadHandler?.Dispose();
                UploadHandler?.Dispose();
            }
        }

        readonly struct PageLoadResult
        {
            public readonly string Error;
            public readonly string Method;
            public readonly Uri Uri;
            public readonly long ResponseCode;
            public readonly UnityWebRequest.Result Result;
            public readonly IReadOnlyDictionary<string, string> ResponseHeaders;

            public PageLoadResult(UnityWebRequest request)
            {
                if (request is null)
                {
                    this.Error = null;
                    this.Method = null;
                    this.ResponseCode = 0;
                    this.Result = UnityWebRequest.Result.InProgress;
                    this.ResponseHeaders = new Dictionary<string, string>();
                    this.Uri = null;
                }
                else
                {
                    this.Error = request.error;
                    this.Method = request.method;
                    this.ResponseCode = request.responseCode;
                    this.Result = request.result;
                    this.ResponseHeaders = request.GetResponseHeaders();
                    this.Uri = request.uri ?? ((request.url != null) ? new Uri(request.url) : null);
                }
            }

            public static PageLoadResult Default => new(null);
        }

        Uri CurrentURI;
        int FontSize;
        HtmlDocument Document;

        public override string ID => "web";

        readonly List<DownloadTask> Tasks = new();
        bool HasTasks => Tasks.Count > 0;

        ushort SelectedElementID = 0;
        ushort HoveringLinkID = 0;

        Blinker Blinker;

        record Cache
        {
            public enum DataType
            {
                Text,
                Binary,
                Image,
            }

            public Uri Uri;
            public string Text;
            public byte[] Binary;
            public Texture2D Image;
            public DataType Type;

            public Cache(Uri uri, string data)
            {
                Uri = uri;
                Text = data;
                Type = DataType.Text;
            }

            public Cache(Uri uri, byte[] data)
            {
                Uri = uri;
                Binary = data;
                Type = DataType.Binary;
            }

            public Cache(Uri uri, Texture2D data)
            {
                Uri = uri;
                Image = data;
                Type = DataType.Image;
            }

            public void Dispose()
            {
                if (Image != null)
                { Texture2D.Destroy(Image); }

                Uri = null;
                Text = null;
                Binary = null;
                Image = null;
            }
        }

        readonly List<Cache> Caches;
        readonly Dictionary<byte, Texture2D> Images;

        int ScrollbarWidth = 4;
        Vector2Int CurrentScroll;
        readonly GraphicsElementGenerator.Generator Page;
        readonly Stylesheet DefaultStylesheet;
        PageLoadResult LastPageLoadResult;

        RectInt GetPageArea()
        {
            RectInt result = Rect;

            result.y += 8;
            result.height -= 8;

            result.x += 1;
            result.width -= 1;

            result.height -= 1;
            result.width -= 1;

            result.width -= ScrollbarWidth;

            return result;
        }

        public ProgramWebBrowser(OperatingSystem computer, string[] arguments) : base(computer, arguments)
        {
            FontSize = 8;

            Caches = new List<Cache>();
            Images = new Dictionary<byte, Texture2D>();
            Page = new GraphicsElementGenerator.Generator();
            SelectedElementID = 0;
            HoveringLinkID = 0;

            Blinker = new();

            LastPageLoadResult = PageLoadResult.Default;

            computer.Keyboard.OnDown += OnKeyDown;

            if (computer.Hardware.Assets.Any(asset => asset.name == "DefaultStylesheet"))
            { DefaultStylesheet = new Parser().Parse(computer.Hardware.Assets.First(v => v.name == "DefaultStylesheet").text); }
            else
            {
                Debug.LogWarning($"[Computer/ProgramWebBrowser]: Default stylesheet not found", computer.Hardware);
            }
        }

        public override void Start()
        {
            if (Arguments.Length > 0)
            { LoadPage(Arguments[0]); }
        }

        public override void Destroy()
        {
            foreach (DownloadTask task in Tasks)
            { task.Dispose(); }

            foreach (KeyValuePair<byte, Texture2D> image in Images)
            { Texture2D.Destroy(image.Value); }

            foreach (Cache cache in Caches)
            { cache.Dispose(); }

            Tasks.Clear();
            Images.Clear();
            Caches.Clear();
        }

        void OnKeyDown(KeyCode key)
        {
            if (SelectedElementID == 0) return;
            GraphicsElementGenerator.ElementWithID selectedElement = Page.GetElementByID(SelectedElementID);
            if (selectedElement == null) return;

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                return;
            }

            if (selectedElement is GraphicsElementGenerator.ElementTextField elementTextField)
            {
                elementTextField.Manager.FeedKey(key);
            }
        }

        public override void Draw()
        {
            if (LastPageLoadResult.Result == UnityWebRequest.Result.Success)
            {
                DrawPage();
            }
            else if (LastPageLoadResult.Result == UnityWebRequest.Result.InProgress)
            { }
            else
            {
                int y = 0;
                y += VideoCard.DrawText(LastPageLoadResult.Result.ToString(), new Vector2Int(Page.PageArea.xMin, Page.PageArea.yMin + y), Mathf.RoundToInt(FontSize * 1.25f), Color.red).y;
                y += 4;

                y += VideoCard.DrawText(LastPageLoadResult.Method, new Vector2Int(Page.PageArea.xMin, Page.PageArea.yMin + y), 8, Color.white).y;
                y += VideoCard.DrawText(LastPageLoadResult.Uri?.ToString(), new Vector2Int(Page.PageArea.xMin, Page.PageArea.yMin + y), 8, Color.blue, VideoCard.TextDecorations.Underline).y;
                y += 4;

                if (LastPageLoadResult.ResponseCode != 0)
                {
                    y += VideoCard.DrawText($"HTTP {LastPageLoadResult.ResponseCode}", new Vector2Int(Page.PageArea.xMin, Page.PageArea.yMin + y), FontSize, Color.white).y;
                    y += 4;
                }

                if (!string.IsNullOrWhiteSpace(LastPageLoadResult.Error))
                {
                    RectInt remaingSpace = Page.PageArea;
                    remaingSpace.yMin += y;
                    VideoCard.DrawText(LastPageLoadResult.Error, remaingSpace, FontSize, Color.red);
                }
            }

            RectInt headerRect = new(Rect.xMin, Rect.yMin, Rect.width, Layout.LineHeight);

            VideoCard.DrawRectangle(headerRect, Color.black);

            if (HasTasks)
            {
                DownloadTask task = Tasks[0];
                if (task.Request.isDone)
                {
                    switch (task.Request.result)
                    {
                        case UnityWebRequest.Result.Success:
                            VideoCard.DrawText($"HTTP {task.Request.responseCode}", headerRect, FontSize, Color.green);
                            break;
                        case UnityWebRequest.Result.ConnectionError:
                            VideoCard.DrawText($"Conn. Error", headerRect, FontSize, Color.red);
                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            VideoCard.DrawText($"Prot. Error", headerRect, FontSize, Color.red);
                            break;
                        case UnityWebRequest.Result.DataProcessingError:
                            VideoCard.DrawText($"Data Pr. E.", headerRect, FontSize, Color.red);
                            break;
                        default:
                        case UnityWebRequest.Result.InProgress:
                            VideoCard.DrawText($"Loading...", headerRect, FontSize, Color.red);
                            break;
                    }
                }
                else
                {
                    float progress = task.DownloadProgress;
                    if (progress >= 0)
                    {
                        VideoCard.DrawRectangleOutline(headerRect, Color.white);
                        RectInt progressRect = headerRect.Padding(-1);
                        progressRect.width = Mathf.RoundToInt(progressRect.width * progress);
                        VideoCard.DrawRectangle(progressRect, Color.white);
                    }

                    VideoCard.DrawText("Loading...", headerRect, FontSize, Color.white);
                }
            }

            VideoCard.DrawLine(new Vector2Int(Rect.xMin, headerRect.yMax), new Vector2Int(Rect.xMax, headerRect.yMax), Color.white);

            bool scrolling = false;

            {
                int scrollbarHandleSize = 16;
                RectInt scrollbarRect = new(Rect.xMax - ScrollbarWidth, Rect.yMin, ScrollbarWidth, Rect.height - ScrollbarWidth);
                VideoCard.DrawRectangle(scrollbarRect, new Color(.2f, .2f, .2f));

                float scrollProgress = (float)(-CurrentScroll.y) / (float)Page.PageHeight;

                float scrollbarHandleTop = scrollProgress * (scrollbarRect.height - scrollbarHandleSize);
                // float scrollbarHandleBottom = scrollbarHandleTop + scrollbarHandleSize;

                RectInt scrollbarHandleRect = new(scrollbarRect.xMin, Mathf.RoundToInt(scrollbarRect.yMin + scrollbarHandleTop), ScrollbarWidth, scrollbarHandleSize);

                Color scrollbarHandleColor = Color.gray;

                if (!scrolling && Computer.Mouse.IsButtonHold(MouseButton.Left) && scrollbarRect.Contains(Computer.TransformPoint(Computer.Mouse.PressedAt(MouseButton.Left))))
                {
                    float position = Mathf.Clamp01(((float)Computer.TransformedMousePosition.y - (float)scrollbarRect.y) / (float)scrollbarRect.height);

                    CurrentScroll.y = -Mathf.RoundToInt(position * Page.PageHeight);
                    scrollbarHandleColor = Color.white;
                    scrolling = true;
                    SelectedElementID = 0;
                }

                VideoCard.DrawRectangle(scrollbarHandleRect, scrollbarHandleColor);
            }

            {
                int scrollbarHandleSize = 16;
                RectInt scrollbarRect = new(Rect.xMin, Rect.yMax - ScrollbarWidth, Rect.width - ScrollbarWidth, ScrollbarWidth);
                VideoCard.DrawRectangle(scrollbarRect, new Color(.2f, .2f, .2f));

                float scrollProgress = (float)(-CurrentScroll.x) / (float)Page.PageWidth;

                float scrollbarHandleTop = scrollProgress * (scrollbarRect.width - scrollbarHandleSize);
                // float scrollbarHandleBottom = scrollbarHandleTop + scrollbarHandleSize;

                RectInt scrollbarHandleRect = new(Mathf.RoundToInt(scrollbarRect.xMin + scrollbarHandleTop), scrollbarRect.yMin, scrollbarHandleSize, ScrollbarWidth);

                Color scrollbarHandleColor = Color.gray;

                if (!scrolling && Computer.Mouse.IsButtonHold(MouseButton.Left) && scrollbarRect.Contains(Computer.TransformPoint(Computer.Mouse.PressedAt(MouseButton.Left))))
                {
                    float position = Mathf.Clamp01(((float)Computer.TransformedMousePosition.x - (float)scrollbarRect.x) / (float)scrollbarRect.width);

                    CurrentScroll.x = -Mathf.RoundToInt(position * Page.PageWidth);
                    scrollbarHandleColor = Color.white;
                    scrolling = true;
                    SelectedElementID = 0;
                }

                VideoCard.DrawRectangle(scrollbarHandleRect, scrollbarHandleColor);
            }
        }

        public override void Tick()
        {
            if (HasTasks)
            {
                DownloadTask task = Tasks[0];

                task.Tick();

                if (task.IsDone)
                {
                    task.Dispose();
                    Tasks.RemoveAt(0);
                }
                else if (!task.RequestSent)
                {
                    task.SendWebRequest();
                }
                else if (task.Request != null && (
                    task.Request.result == UnityWebRequest.Result.ConnectionError ||
                    task.Request.result == UnityWebRequest.Result.DataProcessingError ||
                    task.Request.result == UnityWebRequest.Result.ProtocolError
                    ))
                {
                    LastPageLoadResult = new PageLoadResult(task.Request);
                    Page.PageArea = GetPageArea();
                    task.Dispose();
                    Tasks.RemoveAt(0);
                }
            }

            foreach (var form in Page.Forms)
            {
                if (form.Submitted) continue;
                if (!form.ShouldSubmit) continue;
                SubmitForm(form);
                break;
            }

            if (Computer.Hardware.IsControlling)
            {
                CurrentScroll += Vector2Int.CeilToInt(Computer.Mouse.ScrollDelta * 4);
                CurrentScroll.x = Mathf.Min(CurrentScroll.x, 0);
                CurrentScroll.y = Mathf.Min(CurrentScroll.y, 0);
            }

            Blinker.Tick();
        }

        void DrawPage()
        {
            bool anyLinkHovered = false;
            foreach (GraphicsElementGenerator.Element element in Page.Elements)
            {
                CachedDimensions dimensions = element.Dimensions;
                dimensions += Page.PageArea.position;
                dimensions += CurrentScroll;

                RectInt borderRect = dimensions.BorderRect;

                if (borderRect.xMax <= Rect.xMin || borderRect.yMax <= Rect.yMin || borderRect.xMin >= Rect.xMax)
                { continue; }
                if (borderRect.yMin >= Rect.yMax)
                { break; }

                bool canHandleMouse = Computer.IsPointerOnRect(Page.PageArea);
                bool isHovered = false;

                if (canHandleMouse && element is GraphicsElementGenerator.ElementFocusable elementFocusable)
                {
                    bool pointerOver = Computer.IsPointerOnRect(borderRect);
                    if (pointerOver)
                    {
                        if (Computer.Mouse.IsButtonUp(MouseButton.Left) && borderRect.Contains(Computer.TransformPoint(Computer.Mouse.PressedAt(MouseButton.Left))))
                        {
                            SelectedElementID = elementFocusable.ID;
                        }
                    }
                }

                if (canHandleMouse)
                {
                    isHovered = Computer.IsPointerOnRect(borderRect);
                }

                bool isSelected = false;

                if (element is GraphicsElementGenerator.ElementWithID elementWithID)
                {
                    isSelected = SelectedElementID == elementWithID.ID;
                }

                RectInt contentRect = dimensions.Content;

                if (element is GraphicsElementGenerator.ElementLabel elementLabel)
                {
                    if (string.IsNullOrWhiteSpace(elementLabel.Link))
                    {
                        VideoCard.DrawText(elementLabel.Text, contentRect, FontSize, elementLabel.Color);
                    }
                    else
                    {
                        Color color = elementLabel.Color;

                        if (isHovered || elementLabel.LinkID == HoveringLinkID)
                        {
                            HoveringLinkID = elementLabel.LinkID;
                            color = Color.white;

                            if (Computer.Mouse.IsButtonUp(MouseButton.Left) && borderRect.Contains(Computer.TransformPoint(Computer.Mouse.PressedAt(MouseButton.Left))))
                            {
                                LoadPage(elementLabel.Link);
                            }
                        }

                        anyLinkHovered = anyLinkHovered || isHovered;

                        VideoCard.DrawText(elementLabel.Text, contentRect, FontSize, color, VideoCard.TextDecorations.Underline);
                    }
                }
                else if (element is GraphicsElementGenerator.ElementImage elementImage)
                {
                    if (Images.TryGetValue(elementImage.ImageID, out Texture2D image))
                    {
                        VideoCard.DrawTexture(contentRect, image, Color.white);
                    }
                    else
                    {
                        VideoCard.DrawRectangleOutline(contentRect, Color.gray);
                    }
                }
                else if (element is GraphicsElementGenerator.ElementButton elementButton)
                {
                    bool clicked = VideoCard.DrawButton(borderRect, elementButton.Text, contentRect, FontSize);

                    if (clicked && canHandleMouse)
                    {
                        if (elementButton.Form != null && !elementButton.Form.Submitted)
                        { elementButton.Form.ShouldSubmit = true; }
                    }
                }
                else if (element is GraphicsElementGenerator.ElementTextField elementTextField)
                {
                    RectInt textRect = VideoCard.AlignText(contentRect, elementTextField.Manager.Buffer, FontSize, VideoCard.Align.Start, VideoCard.Align.Middle);
                    VideoCard.DrawText(elementTextField.Manager.Buffer, textRect, FontSize, Color.white);

                    if (isSelected)
                    {
                        VideoCard.DrawRectangleOutline(borderRect, Color.blue);
                        if (Blinker.IsOn)
                        {
                            var characterSize = VideoCard.SelectedFont.CharacterSize(FontSize);
                            int x = contentRect.x + (characterSize.x * elementTextField.Manager.Buffer.Length);
                            VideoCard.DrawLine(new Vector2Int(x, textRect.yMin), new Vector2Int(x, textRect.yMin + characterSize.y), Color.white);
                        }
                    }
                    else
                    {
                        bool pointerOver = Computer.IsPointerOnRect(borderRect);
                        VideoCard.DrawRectangleOutline(borderRect, (pointerOver && canHandleMouse) ? Color.white : Color.gray);
                    }
                }
                else if (element is GraphicsElementGenerator.ElementSelect elementSelect)
                {
                    VideoCard.DrawText(elementSelect.Label, VideoCard.AlignText(contentRect, elementSelect.Label, FontSize, VideoCard.Align.Middle, VideoCard.Align.Middle), FontSize, Color.white);
                    VideoCard.DrawRectangleOutline(borderRect, Color.white);
                }
                else if (element is GraphicsElementGenerator.ElementForm)
                {

                }
                else
                {
                    VideoCard.DrawRectangle(borderRect, Color.magenta);
                }
            }

            if (!anyLinkHovered)
            {
                HoveringLinkID = 0;
            }
        }

        void SubmitForm(GraphicsElementGenerator.ElementForm form)
        {
            if (form.Submitted) return;
            string target = form.Target;

            if (!Uri.TryCreate(CurrentURI, target, out Uri uri)) return;

            string method = form.Method;

            GraphicsElementGenerator.Element[] inputs = Page.GetFormElements(form);

            Dictionary<string, string> fields = new();

            foreach (GraphicsElementGenerator.Element input in inputs)
            {
                if (input is GraphicsElementGenerator.ElementTextField textField)
                {
                    fields[textField.Name] = textField.Manager.Buffer;
                }
            }

            switch (method.ToUpperInvariant())
            {
                case "GET":
                    Tasks.Add(DownloadTask.Get(uri, OnPageDownloaded));
                    break;
                case "POST":
                    Tasks.Add(DownloadTask.Post(uri, fields, OnPageDownloaded));
                    break;
                default: return;
            }

            form.Submitted = true;
        }

        void LoadPage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return;

            CurrentURI = uri;
            LastPageLoadResult = PageLoadResult.Default;
            Download(CurrentURI, OnPageDownloaded, true);
        }

        bool TryGetImageSize(string url, out Vector2Int imageSize)
        {
            imageSize = default;
            if (string.IsNullOrWhiteSpace(url))
            { return false; }

            if (!Uri.TryCreate(CurrentURI, url, out Uri uri))
            { return false; }

            foreach (var image in Page.NeedTheseImages)
            {
                if (image.Url != url) continue;

                if (image.DownloadedSize == Vector2Int.zero)
                { return false; }

                imageSize = image.DownloadedSize;
                return true;
            }

            return false;
        }

        void OnPageDownloaded(DownloadTask sender, string data)
        {
            LastPageLoadResult = new PageLoadResult(sender.Request);

            Images.Clear();

            CurrentScroll = Vector2Int.zero;
            SelectedElementID = 0;
            Document = new()
            {
                OptionFixNestedTags = true
            };
            Document.LoadHtml(data ?? "");

            Page.Reset();
            Page.Stylesheets.Add(DefaultStylesheet);
            Page.PageArea = GetPageArea();

            HtmlNodeCollection styles = Document.DocumentNode.SelectNodes("style");

            Page.GenerateLayout(Document, VideoCard.TextSize, TryGetImageSize);

            List<string> cssLinks = new();

            var html = Document.DocumentNode.Element("html");

            if (html != null)
            {
                var head = html.Element("head");
                if (head != null)
                {
                    var links = head.Elements("link");
                    foreach (var link in links)
                    {
                        if (link.GetAttributeValue("rel", "none") != "stylesheet") continue;
                        string href = link.GetAttributeValue("href", null);
                        if (string.IsNullOrWhiteSpace(href)) continue;
                        cssLinks.Add(href);
                    }
                }
            }

            foreach (string cssLink in cssLinks)
            {
                if (Uri.TryCreate(CurrentURI, cssLink, out Uri uri))
                { Download(uri, OnStylesheetDownloaded, false); }
            }

            foreach (GraphicsElementGenerator.Generator.NeedThisImage image in Page.NeedTheseImages)
            {
                if (Uri.TryCreate(CurrentURI, image.Url, out Uri uri))
                { Download(uri, OnImageDownloaded, false); }
            }
        }

        void OnImageDownloaded(DownloadTask sender, Texture2D data)
        {
            if (data == null) return;

            Caches.Add(new Cache(sender.Uri, data));

            foreach (var image in Page.NeedTheseImages)
            {
                if (Uri.TryCreate(CurrentURI, image.Url, out Uri uri) && uri == sender.Uri)
                {
                    Images[image.ID] = data;
                    image.DownloadedSize = new Vector2Int(data.width, data.height);
                    break;
                }
            }

            Page.GenerateLayout(Document, VideoCard.TextSize, TryGetImageSize);
        }

        void OnStylesheetDownloaded(DownloadTask sender, string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;

            Parser parser = new();
            Stylesheet stylesheet = parser.Parse(data);
            Page.Stylesheets.Add(stylesheet);

            Page.GenerateLayout(Document, VideoCard.TextSize, TryGetImageSize);
        }

        void Download(Uri uri, DownloadTask.CompletedEvent<string> callback, bool autoStart = true)
        {
            foreach (Cache cache in Caches)
            {
                if (cache.Uri == uri)
                {
                    callback?.Invoke(null, cache.Text);
                    return;
                }
            }

            Tasks.Add(DownloadTask.Get(uri, callback, autoStart));
        }

        void Download(Uri uri, DownloadTask.CompletedEvent<byte[]> callback, bool autoStart = true)
        {
            foreach (Cache cache in Caches)
            {
                if (cache.Uri == uri)
                {
                    callback?.Invoke(null, cache.Binary);
                    return;
                }
            }

            Tasks.Add(DownloadTask.Get(uri, callback, autoStart));
        }

        void Download(Uri uri, DownloadTask.CompletedEvent<Texture2D> callback, bool autoStart = true)
        {
            foreach (Cache cache in Caches)
            {
                if (cache.Uri == uri)
                {
                    callback?.Invoke(null, cache.Image);
                    return;
                }
            }

            Tasks.Add(DownloadTask.Get(uri, callback, autoStart));
        }
    }
}
