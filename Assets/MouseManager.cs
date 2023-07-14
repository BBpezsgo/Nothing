using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

public class MouseManager : MonoBehaviour
{
    static MouseManager Instance;

    List<Utilities.AdvancedPriorityMouse> Mouses;
    static readonly Utilities.AdvancedPriorityMouseComparer Comparer = new();

    [SerializeField, ReadOnly] float TimeToNextUICollecting = 1f;
    [SerializeField, ReadOnly, NonReorderable] UIDocument[] UIDocuments;

    public static bool MouseOnWindow
    {
        get
        {
            if (MainCamera.Camera == null) return false;
            Vector3 view = MainCamera.Camera.ScreenToViewportPoint(Input.mousePosition);
            return !(view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1);
        }
    }

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"[{nameof(MouseManager)}]: Instance already registered");
            Object.Destroy(this);
            return;
        }
        Instance = this;
        Mouses = new List<Utilities.AdvancedPriorityMouse>();
    }

    void Update()
    {
        if (MenuManager.AnyMenuVisible) return;

        for (int i = 0; i < Mouses.Count; i++)
        { Mouses[i].Update(); }
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

    void RegisterMouse_(Utilities.AdvancedPriorityMouse mouse)
    {
        Mouses.Add(mouse);
        Mouses.Sort(Comparer);
    }

    void DeregisterMouse_(Utilities.AdvancedPriorityMouse mouse)
    {
        Mouses.Remove(mouse);
        Mouses.Sort(Comparer);
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

    internal static bool IsPointerOverUI(Vector2 screenPosition)
        => Instance.IsPointerOverUI_(screenPosition);

    internal static bool IsPointerOverUI()
        => Instance.IsPointerOverUI_(Input.mousePosition);

    internal static void RegisterMouse(Utilities.AdvancedPriorityMouse mouse)
        => Instance.RegisterMouse_(mouse);
    internal static void DeregisterMouse(Utilities.AdvancedPriorityMouse mouse)
        => Instance.DeregisterMouse_(mouse);
}
