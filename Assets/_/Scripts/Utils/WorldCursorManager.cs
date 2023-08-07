using System.Collections.Generic;
using InputUtils;
using UnityEngine;

namespace Game.Managers
{
    internal class WorldCursorManager : SingleInstance<WorldCursorManager>
    {
        readonly List<INeedWorldCursor> NeedWorldCursor = new();
        readonly List<INeedDirectWorldCursor> NeedDirectWorldCursor = new();

        // [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedWorldCursor = new MonoBehaviour[0];
        // [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedDirectWorldCursor = new MonoBehaviour[0];

        InputUtils.AdvancedMouse LeftMouseButton;

        static readonly CursorPriorityComparer CursorPriorityComparer = new();

        bool NeedResort = false;
        float ResortIn = 0f;

        void Start()
        {
            LeftMouseButton = new InputUtils.AdvancedMouse(MouseButton.Left, 9, MouseCondition);
            LeftMouseButton.OnClick += OnLeftMouseButtonClick;
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
            (TakeControlManager.Instance == null || !TakeControlManager.Instance.IsControlling) &&
            !BuildingManager.Instance.IsBuilding &&
            (SelectionManager.Instance.Selected == null || SelectionManager.Instance.Selected.Length == 0);

        void OnLeftMouseButtonClick(AdvancedMouse sender)
        {
            if (!MouseManager.MouseOnWindow) return;

            Ray ray = MainCamera.Camera.ScreenPointToRay(AdvancedMouse.Position);
            float maxDistance = 500f;
            if (!Physics.Raycast(ray, out var hit, maxDistance))
            { return; }

            Vector3 point = hit.point;

            for (int i = NeedDirectWorldCursor.Count - 1; i >= 0; i--)
            {
                if (NeedDirectWorldCursor[i].gameObject == null) continue;
                if (!NeedDirectWorldCursor[i].gameObject.activeSelf) continue;

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
}

namespace Game
{
    internal class CursorPriorityComparer : IComparer<INeedWorldCursor>, IComparer<INeedDirectWorldCursor>
    {
        public int Compare(INeedWorldCursor a, INeedWorldCursor b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);

        public int Compare(INeedDirectWorldCursor a, INeedDirectWorldCursor b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);
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
}
