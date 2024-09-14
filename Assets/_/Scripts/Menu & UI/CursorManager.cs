using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    internal interface ICanChangeCursor
    {
        public int CursorPriority { get; }
        public bool HandleCursor();
        public bool HandleCursorLock(out CursorLockMode locked);
    }
}

namespace Game.Managers
{
    using System;
    using Game.UI;

    internal static class Comparers
    {
        public static int Compare(ICanChangeCursor a, ICanChangeCursor b)
            => System.Collections.Comparer.Default.Compare(b.CursorPriority, a.CursorPriority);
    }

    internal enum DefaultCursors
    {
        Hand,
    }

    internal class CursorManager : SingleInstance<CursorManager>
    {
        readonly List<ICanChangeCursor> CursorManagers = new();

        [SerializeField] CursorConfig HandCursor;
        [SerializeField, ReadOnly] Texture2D CurrentCursor = null;

        bool NeedResort = false;
        float ResortIn = 0f;

        const float Infrequency = 2f;
        float InfrequencyTimer = Infrequency;

        void Update()
        {
            if (!MouseManager.MouseOnWindow) return;

            FrequentUpdate();

            InfrequencyTimer -= Time.deltaTime;
            if (InfrequencyTimer <= 0f)
            {
                InfrequencyTimer = Infrequency;
                InfrequentUpdate();
            }

            ManageSorting();
        }

        void InfrequentUpdate()
        {
            InfrequencyTimer = Infrequency;

            if (!MenuManager.AnyMenuVisible)
            {
                for (int i = 0; i < CursorManagers.Count; i++)
                {
                    if (CursorManagers[i].HandleCursorLock(out CursorLockMode locked))
                    {
                        Cursor.lockState = locked;
                        return;
                    }
                }
            }

            Cursor.lockState = CursorLockMode.None;
        }

        void FrequentUpdate()
        {
            if (!MenuManager.AnyMenuVisible && MouseManager.MouseOnWindow)
            {
                for (int i = 0; i < CursorManagers.Count; i++)
                {
                    if (CursorManagers[i].HandleCursor())
                    { return; }
                }
            }

            SetCursor();
        }

        void ManageSorting()
        {
            if (!NeedResort)
            { return; }

            if (ResortIn > 0f)
            {
                ResortIn -= Time.deltaTime;
                return;
            }

            CursorManagers.Sort(Comparers.Compare);

            NeedResort = false;
        }

        public void Register(ICanChangeCursor obj)
        {
            CursorManagers.Add(obj);
            NeedResort = true;
            if (ResortIn < 0f) ResortIn = 1f;
        }

        public void Deregister(ICanChangeCursor obj)
        {
            CursorManagers.Remove(obj);
        }

        public void ForceUpdate()
        {
            FrequentUpdate();
            InfrequentUpdate();
        }

        public static void SetDefaultCursor(DefaultCursors cursor)
        {
            switch (cursor)
            {
                case DefaultCursors.Hand:
                    instance.HandCursor.Set();
                    break;
                default:
                    break;
            }
        }

        bool MouseCondition() =>
            !TakeControlManager.Instance.IsControlling &&
            !BuildingManager.Instance.IsBuilding &&
            (SelectionManager.Instance.Selected == null || SelectionManager.Instance.Selected.Length == 0);

        public static void SetCursor(Texture2D texture, Vector2 hotspot)
        {
            if (instance == null || instance.CurrentCursor == texture) return;

            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            instance.CurrentCursor = texture;
        }

        public static void SetCursor()
        {
            Cursor.visible = true;

            if (instance == null || instance.CurrentCursor == null) return;

            Cursor.SetCursor(null, default, CursorMode.Auto);
            instance.CurrentCursor = null;
        }
    }
}
