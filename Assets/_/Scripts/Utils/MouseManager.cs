using System.Collections.Generic;
using InputUtils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Managers
{
    public class MouseManager : MonoBehaviour
    {
        static MouseManager Instance;

        List<AdvancedMouse> Mouses;
        List<AdvancedTouch> Touches;

        [SerializeField, ReadOnly] float TimeToNextUICollecting = 1f;
        [SerializeField, ReadOnly, NonReorderable] UIDocument[] UIDocuments;

        [SerializeField] bool ShowDebugStuff;

        public static bool MouseOnWindow
        {
            get
            {
                if (MainCamera.Camera == null)
                { return false; }

                Vector3 mousePosition = Input.mousePosition;

                if (mousePosition.x < 0 ||
                    mousePosition.y < 0 ||
                    mousePosition.x > Screen.width ||
                    mousePosition.y > Screen.height)
                { return false; }

                Vector3 view = MainCamera.Camera.ScreenToViewportPoint(mousePosition);
                return !(view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1);
            }
        }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(MouseManager)}]: Instance already registered, destroying self");
                Object.Destroy(this);
                return;
            }
            Instance = this;

            Mouses = new List<AdvancedMouse>();
            Touches = new List<AdvancedTouch>();

            Input.simulateMouseWithTouches = false;
        }

        void Update()
        {
            if (MenuManager.AnyMenuVisible) return;

            for (int i = 0; i < Mouses.Count; i++)
            { Mouses[i].Update(); }

            for (int i = 0; i < Touches.Count; i++)
            { Touches[i].Update(); }
        }

        void FixedUpdate()
        {
            TimeToNextUICollecting -= Time.fixedDeltaTime;
            if (TimeToNextUICollecting <= 0f)
            {
                TimeToNextUICollecting = 10f;
                UIDocuments = GameObject.FindObjectsOfType<UIDocument>(true);
            }
        }

        bool IsPointerOverUI_(Vector2 screenPosition)
        {
            Vector2 pointerUiPos = new(screenPosition.x, Screen.height - screenPosition.y);
            for (int i = 0; i < UIDocuments.Length; i++)
            {
                if (UIDocuments[i] == null) continue;
                if (!UIDocuments[i].gameObject.activeSelf) continue;
                if (!UIDocuments[i].isActiveAndEnabled) continue;
                if (UIDocuments[i].rootVisualElement == null) continue;

                List<VisualElement> picked = new();
                UIDocuments[i].rootVisualElement.panel.PickAll(pointerUiPos, picked);
                for (int j = 0; j < picked.Count; j++)
                {
                    if (picked[j] == null) continue;

                    Color32 color = picked[j].resolvedStyle.backgroundColor;
                    if (picked[j].enabledInHierarchy && color.a != 0)
                    { return true; }
                }
            }

            if (GUIUtility.hotControl != 0)
            {
                return true;
            }

            return false;
        }

        internal static bool IsOverUI(Vector2 screenPosition)
            => Instance.IsPointerOverUI_(screenPosition);

        internal static bool IsPointerOverUI()
            => Instance.IsPointerOverUI_(Input.mousePosition);

        internal static void RegisterInput(AdvancedMouse mouse)
        {
            Instance.Mouses.Add(mouse);
            Instance.Mouses.Sort();
        }

        internal static void RegisterInput(AdvancedTouch mouse)
        {
            Instance.Touches.Add(mouse);
            Instance.Touches.Sort();
        }

        internal static bool IsTouchCaptured(int touchID)
        {
            AdvancedTouch[] touches = Instance.Touches.ToArray();
            for (int i = 0; i < touches.Length; i++)
            {
                if (touches[i].FingerID == touchID && touches[i].IsCaptured)
                { return true; }
            }
            return false;
        }

        internal static bool IsTouchCaptured(int touchID, AdvancedTouch sender)
        {
            AdvancedTouch[] touches = Instance.Touches.ToArray();
            for (int i = 0; i < touches.Length; i++)
            {
                if (AdvancedTouch.ReferenceEquals(touches[i], sender)) continue;

                if (touches[i].FingerID == touchID && touches[i].IsCaptured)
                { return true; }
            }
            return false;
        }

        void OnGUI()
        {
            if (!ShowDebugStuff) return;

            GL.PushMatrix();
            if (GLUtils.SolidMaterial.SetPass(0))
            {
                for (int i = 0; i < Mouses.Count; i++)
                {
                    if (!Mouses[i].Enabled) continue;
                    Mouses[i].DebugDraw();
                    break;
                }

                for (int i = 0; i < Touches.Count; i++)
                {
                    Touches[i].DebugDraw();
                }
            }
            GL.PopMatrix();
        }
    }
}
