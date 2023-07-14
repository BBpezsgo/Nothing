using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Utilities;

internal class CursorPriorityComparer : IComparer<INeedWorldCursor>, IComparer<INeedDirectWorldCursor>
{
    public int Compare(INeedWorldCursor a, INeedWorldCursor b)
        => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);

    public int Compare(INeedDirectWorldCursor a, INeedDirectWorldCursor b)
        => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);
}

internal class WorldCursorManager : SingleInstance<WorldCursorManager>
{
    readonly List<INeedWorldCursor> NeedWorldCursor = new();
    readonly List<INeedDirectWorldCursor> NeedDirectWorldCursor = new();

    // [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedWorldCursor = new MonoBehaviour[0];
    // [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedDirectWorldCursor = new MonoBehaviour[0];

    AdvancedPriorityMouse LeftMouseButton;

    static readonly CursorPriorityComparer CursorPriorityComparer = new();

    bool NeedResort = false;
    float ResortIn = 0f;

    void Start()
    {
        LeftMouseButton = new AdvancedPriorityMouse(MouseButton.Left, 9, MouseCondition);
        LeftMouseButton.OnClick += OnLeftMouseButtonClick;
        MouseManager.RegisterMouse(LeftMouseButton);
    }

    void FixedUpdate()
    {
        if (!NeedResort)
        {
            return;
        }

        if (ResortIn > 0f)
        {
            ResortIn -= Time.fixedDeltaTime;
            return;
        }

        NeedDirectWorldCursor.Sort(CursorPriorityComparer);
        NeedWorldCursor.Sort(CursorPriorityComparer);

        // _NeedDirectWorldCursor = NeedDirectWorldCursor.Select(v => (MonoBehaviour)v).ToArray();
        // _NeedWorldCursor = NeedWorldCursor.Select(v => (MonoBehaviour)v).ToArray();

        NeedResort = false;
    }

    public void Register(INeedWorldCursor obj)
    {
        NeedWorldCursor.Add(obj);
        NeedResort = true;
        if (ResortIn < 0f) ResortIn = 1f;
    }
    public void Register(INeedDirectWorldCursor obj)
    {
        NeedDirectWorldCursor.Add(obj);
        NeedResort = true;
        if (ResortIn < 0f) ResortIn = 1f;
    }

    public void Deregister(INeedWorldCursor obj)
    {
        NeedWorldCursor.Remove(obj);
    }
    public void Deregister(INeedDirectWorldCursor obj)
    {
        NeedDirectWorldCursor.Remove(obj);
    }

    bool MouseCondition() =>
        !TakeControlManager.Instance.IsControlling &&
        !BuildingManager.Instance.IsBuilding &&
        (SelectionManager.Instance.Selected == null || SelectionManager.Instance.Selected.Length == 0);

    void OnLeftMouseButtonClick(Vector2 position)
    {
        Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
        float maxDistance = 500f;
        if (!Physics.Raycast(ray, out var hit, maxDistance))
        { return; }

        Vector3 point = hit.point;

        for (int i = NeedDirectWorldCursor.Count - 1; i >= 0; i--)
        {
            if (hit.collider != null)
            {
                var v = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>(false);
                for (int j = 0; j < v.Length; j++)
                {
                    if (v[j].gameObject == NeedDirectWorldCursor[i].gameObject)
                    {
                        if (NeedDirectWorldCursor[i].OnWorldCursor(point))
                        { return; }
                    }
                }
            }
        }

        for (int i = NeedWorldCursor.Count - 1; i >= 0; i--)
        {
            if (NeedWorldCursor[i].OnWorldCursor(point))
            { return; }
        }
    }
}

internal interface INeedWorldCursor
{
    public int CursorPriority { get; }
    public bool OnWorldCursor(Vector3 worldPosition);
}

internal interface INeedDirectWorldCursor
{
    public GameObject gameObject { get; }
    public int CursorPriority { get; }
    public bool OnWorldCursor(Vector3 worldPosition);
}