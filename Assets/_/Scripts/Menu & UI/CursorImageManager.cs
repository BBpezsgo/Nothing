using System.Collections.Generic;

using UnityEngine;

namespace Game.UI
{
    internal interface ICanChangeCursorImage
    {
        public int CursorPriority { get; }
        public bool YouCanChangeCursor();
    }
}

namespace Game.Managers
{
    using Game.UI;

    internal class CursorImagePriorityComparer : IComparer<ICanChangeCursorImage>
    {
        public int Compare(ICanChangeCursorImage a, ICanChangeCursorImage b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);
    }

    internal class CursorImageManager : SingleInstance<CursorImageManager>
    {
        readonly List<ICanChangeCursorImage> CursorManagers = new();

        static readonly CursorImagePriorityComparer PriorityComparer = new();

        bool NeedResort = false;
        float ResortIn = 0f;

        void FixedUpdate()
        {
            if (!MouseManager.MouseOnWindow) return;

            Do();

            ManageManagers();
        }

        void Do()
        {
            if (!MenuManager.AnyMenuVisible)
            {
                for (int i = 0; i < CursorManagers.Count; i++)
                { if (CursorManagers[i].YouCanChangeCursor()) return; }
            }
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        void ManageManagers()
        {
            if (!NeedResort)
            { return; }

            if (ResortIn > 0f)
            {
                ResortIn -= Time.fixedDeltaTime;
                return;
            }

            CursorManagers.Sort(PriorityComparer);

            NeedResort = false;
        }

        public void Register(ICanChangeCursorImage obj)
        {
            CursorManagers.Add(obj);
            NeedResort = true;
            if (ResortIn < 0f) ResortIn = 1f;
        }

        public void Deregister(ICanChangeCursorImage obj)
        {
            CursorManagers.Remove(obj);
        }

        bool MouseCondition() =>
            !TakeControlManager.Instance.IsControlling &&
            !BuildingManager.Instance.IsBuilding &&
            (SelectionManager.Instance.Selected == null || SelectionManager.Instance.Selected.Length == 0);
    }
}
