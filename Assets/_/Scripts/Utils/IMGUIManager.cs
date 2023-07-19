using System;
using System.Collections.Generic;

using UnityEngine;

namespace UI
{
    public class IMGUIManager : MonoBehaviour
    {
        public static IMGUIManager Instance;
        [SerializeField, ReadOnly, NonReorderable] List<ImguiWindow> windows = new();
        [SerializeField, ReadOnly, NonReorderable] int windowIdCounter;
        [SerializeField] internal GUISkin Skin;

        [ReadOnly] public bool BlockedByImgui = false;

        public readonly struct TempSkin : IDisposable
        {
            readonly GUISkin savedSkin;

            public TempSkin(GUISkin skin)
            {
                this.savedSkin = GUI.skin;
                GUI.skin = skin;
            }

            public void Dispose()
            {
                GUI.skin = this.savedSkin;
            }
        }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.Log($"{nameof(IMGUIManager)} instance already registered");
                GameObject.Destroy(gameObject);
                return;
            }
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public ImguiWindow CreateWindow()
             => CreateWindow(new Rect(10f, 10f, 20f, 40f));

        public ImguiWindow CreateWindow(Rect rect)
        {
            ImguiWindow newWindow = new(++windowIdCounter, rect);
            windows.Add(newWindow);
            return newWindow;
        }

        void OnGUI()
        {
            BlockedByImgui = GUIUtility.hotControl != 0;
            var skin = GUI.skin;
            GUI.skin = Skin;
            for (int i = 0; i < windows.Count; i++)
            { windows[i].OnGUI(); }
            GUI.skin = skin;
        }
    }

    [Serializable]
    public class ImguiWindow
    {
        public delegate void Draw();

        public bool Visible;
        [ReadOnly] public GUISkin Skin;
        [ReadOnly] public Draw DrawContent;
        [ReadOnly] public string Title;

        [SerializeField, ReadOnly] Rect Rect;

        readonly int id;

        public ImguiWindow(int id, Rect rect)
        {
            this.id = id;
            this.Rect = rect;
        }

        void OnDrawContent(int _) => DrawContent?.Invoke();

        public void OnGUI()
        {
            if (!Visible) return;
            if (DrawContent == null) return;

            GUISkin prevSkin = null;
            if (Skin != null)
            {
                prevSkin = GUI.skin;
                GUI.skin = Skin;
            }

            Rect = GUILayout.Window(id, Rect, OnDrawContent, Title);

            if (Rect.y < 0) Rect.y = 0;
            if (Rect.x < 0) Rect.x = 0;
            if (Rect.x + Rect.width > Screen.width) Rect.x = Screen.width - Rect.width;
            if (Rect.y + Rect.height > Screen.height) Rect.y = Screen.height - Rect.height;

            if (Skin != null) GUI.skin = prevSkin;
        }
    }
}
