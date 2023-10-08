using System.Collections.Generic;
using System.Linq;
using Game.UI;
using InputUtils;
using UnityEngine;

namespace Game.Managers
{
    public class WorldCursorManager : SingleInstance<WorldCursorManager>, ICanChangeCursor
    {
        readonly List<INeedWorldCursor> NeedWorldCursor = new();
        readonly List<INeedDirectWorldCursor> NeedDirectWorldCursor = new();

        [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedWorldCursor = new MonoBehaviour[0];
        [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _NeedDirectWorldCursor = new MonoBehaviour[0];

        AdvancedMouse LeftMouseButton;

        static readonly CursorPriorityComparer CursorPriorityComparer = new();

        bool NeedResort = false;
        float ResortIn = 0f;

        public int CursorPriority => 5;

        readonly RaycastHit[] RayHits = new RaycastHit[10];

        void Start()
        {
            LeftMouseButton = new AdvancedMouse(Mouse.Left, 9, MouseCondition);
            LeftMouseButton.OnClick += OnLeftMouseButtonClick;

            CursorManager.Instance.Register(this);
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

            _NeedDirectWorldCursor = NeedDirectWorldCursor.Select(v => (MonoBehaviour)v).ToArray();
            _NeedWorldCursor = NeedWorldCursor.Select(v => (MonoBehaviour)v).ToArray();

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
            _NeedWorldCursor = NeedWorldCursor.Select(v => (MonoBehaviour)v).ToArray();

        }
        public void Deregister(INeedDirectWorldCursor obj)
        {
            NeedDirectWorldCursor.Remove(obj);
            _NeedDirectWorldCursor = NeedDirectWorldCursor.Select(v => (MonoBehaviour)v).ToArray();
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

            int hitCount = Physics.RaycastNonAlloc(ray, RayHits, maxDistance, Utilities.DefaultLayerMasks.Solids, QueryTriggerInteraction.Collide);

            if (hitCount == 0) return;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit = RayHits[j];

                for (int i = NeedDirectWorldCursor.Count - 1; i >= 0; i--)
                {
                    if (NeedDirectWorldCursor[i].Object() == null) continue;
                    if (NeedDirectWorldCursor[i].gameObject == null) continue;
                    if (!NeedDirectWorldCursor[i].gameObject.activeSelf) continue;

                    if (hit.collider != null)
                    {
                        var v = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>(false);
                        foreach (MonoBehaviour v1 in v)
                        {
                            if (v1.gameObject == NeedDirectWorldCursor[i].gameObject)
                            {
                                if (NeedDirectWorldCursor[i].OnWorldCursor(hit.point))
                                { return; }
                            }
                        }
                    }
                }
            }

            for (int i = NeedWorldCursor.Count - 1; i >= 0; i--)
            {
                if (NeedWorldCursor[i].OnWorldCursor(RayHits[0].point))
                { return; }
            }
        }

        public bool HandleCursor()
        {
            if (!MouseManager.MouseOnWindow) return false;
            if (!MouseCondition()) return false;

            Ray ray = MainCamera.Camera.ScreenPointToRay(AdvancedMouse.Position);
            float maxDistance = 500f;

            int hitCount = Physics.RaycastNonAlloc(ray, RayHits, maxDistance, Utilities.DefaultLayerMasks.Solids, QueryTriggerInteraction.Collide);

            if (hitCount == 0) return false;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit = RayHits[j];
                if (hit.collider.gameObject.HasComponent<INeedDirectWorldCursor>() ||
                    hit.collider.gameObject.HasComponentInChildren<INeedDirectWorldCursor>() ||
                    hit.collider.gameObject.HasComponentInParent<INeedDirectWorldCursor>())
                {
                    CursorManager.SetDefaultCursor(DefaultCursors.Hand);
                    return true;
                }
            }

            return false;
        }

        public bool HandleCursorLock(out CursorLockMode locked)
        {
            locked = default;
            return false;
        }
    }
}

namespace Game
{
    public class CursorPriorityComparer : IComparer<INeedWorldCursor>, IComparer<INeedDirectWorldCursor>
    {
        public int Compare(INeedWorldCursor a, INeedWorldCursor b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);

        public int Compare(INeedDirectWorldCursor a, INeedDirectWorldCursor b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);
    }

    public interface INeedWorldCursor
    {
        public int CursorPriority { get; }
        public bool OnWorldCursor(Vector3 worldPosition);
    }

    public interface INeedDirectWorldCursor : IComponent
    {
        public GameObject gameObject { get; }
        public int CursorPriority { get; }
        public bool OnWorldCursor(Vector3 worldPosition);
    }
}
