using Game.Components;
using Game.UI;
using InputUtils;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Managers
{
    public class SelectionManager : SingleInstance<SelectionManager>, ICanChangeCursor
    {
        internal delegate void SelectionChangedEvent();

        [SerializeField] bool IsEnabled = true;

        [SerializeField, ReadOnly, NonReorderable] internal Selectable[] Selected = new Selectable[0];
        [SerializeField, ReadOnly, NonReorderable] Selectable[] AlmostSelected = new Selectable[0];

        // bool DragFinished = false;
        float AlmostSelectTimer = 0f;

        [Header("UI - Selection Box")]
        [SerializeField] internal Color SelectionBoxColor = new(.8f, .8f, .95f, .25f);
        [SerializeField] internal Color SelectionBoxBorderColor = new(.8f, .8f, .95f);
        [SerializeField, Min(0f)] internal float SelectionBoxBorderWidth = 2f;

        [Header("UI - Game")]
        [SerializeField] internal Color SelectedColor;
        [SerializeField] internal Color AlmostSelectedColor;

        [Header("Cursors")]
        [SerializeField] CursorConfig CursorSelect;
        [SerializeField] CursorConfig CursorGoTo;

        [SerializeField, ReadOnly] UnitCommandManager CommandManager;

        bool selectionChanged = false;
        internal event SelectionChangedEvent OnSelectionChanged;

        AdvancedMouse MouseLeftButton;
        AdvancedMouse MouseRightButton;

        public int CursorPriority => 4;

        Vector3 DragStartWorldPosition;

        protected override void Awake()
        {
            base.Awake();
            CommandManager = FindFirstObjectByType<UnitCommandManager>();
            if (CommandManager == null)
            { Debug.LogWarning($"[{nameof(SelectionManager)}]: {nameof(CommandManager)} is null"); }
        }

        void Start()
        {
            MouseLeftButton = new AdvancedMouse(Mouse.Left, 10, MouseCondition);
            MouseLeftButton.OnDragged += OnLeftDragged;
            MouseLeftButton.OnClick += OnLeftClicked;
            MouseLeftButton.OnDown += OnLeftDown;

            MouseRightButton = new AdvancedMouse(Mouse.Right, 10, MouseCondition);
            MouseRightButton.OnClick += OnRightClicked;

            CursorManager.Instance.Register(this);
        }

        void OnRightClicked(AdvancedMouse sender)
        {
            ClearSelection();
        }

        bool MouseCondition() =>
            IsEnabled &&
            CameraController.Instance != null &&
            (!CameraController.Instance.IsFollowing || CameraController.Instance.JustFollow) &&
            !TakeControlManager.Instance.IsControlling &&
            !BuildingManager.Instance.IsBuilding &&
            !QuickCommandManager.Instance.IsShown &&
            !MenuManager.AnyMenuVisible;

        void Update()
        {
            if (selectionChanged)
            {
                selectionChanged = false;
                OnSelectionChanged?.Invoke();
            }

            if (!MouseCondition())
            {
                DragStartWorldPosition = default;
                return;
            }

            if (AlmostSelectTimer > 0f)
            {
                AlmostSelectTimer -= Time.deltaTime;
                return;
            }

            AlmostSelectTimer = .5f;
            if (MouseLeftButton.IsDragging)
            {
                if (DragStartWorldPosition == default)
                {
                    DragStartWorldPosition = MainCamera.Camera.ScreenToWorldPosition(MouseLeftButton.DragStart);
                }
                OnDrag(MainCamera.Camera.WorldToScreenPoint(DragStartWorldPosition), Mouse.Position);
            }
            else
            {
                OnMouseMove();
            }
        }

        void OnMouseMove()
        {
            ClearAlmostSelection();
            Vector3 mousePosition = Mouse.Position;

            if (mousePosition.x < 0f ||
                mousePosition.y < 0f ||
                !mousePosition.IsOk())
            { return; }

            Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
            float maxDistance = 500f;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.isTrigger) continue;
                if (hits[i].transform.gameObject.TryGetComponent(out Selectable unit))
                {
                    AddAlmostSelection(unit);
                    return;
                }
            }
        }

        void OnLeftDown(AdvancedMouse sender)
        {
            DragStartWorldPosition = MainCamera.Camera.ScreenToWorldPosition(Mouse.Position);
        }

        void OnLeftClicked(AdvancedMouse sender)
        {
            if (sender.HoldTime >= QuickCommandManager.HOLD_TIME_REQUIREMENT) return;
            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(Mouse.Position, Utilities.DefaultLayerMasks.Solids, out RaycastHit[] hits);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject.TryGetComponent(out Selectable unit))
                {
                    if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
                    AddSelection(unit);
                    return;
                }
            }

            if (CommandManager != null)
            { CommandManager.OnClick(worldPosition); }
        }

        void OnDrag(Vector2 positionA, Vector2 positionB)
        {
            ClearAlmostSelection();
            Vector2 min = Vector2.Min(positionA, positionB);
            Vector2 max = Vector2.Max(positionA, positionB);
            Selectable[] allUnits = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allUnits.Length; i++)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(allUnits[i].transform.position);
                if (screenPoint.x < min.x) continue;
                if (screenPoint.y < min.y) continue;
                if (screenPoint.x > max.x) continue;
                if (screenPoint.y > max.y) continue;
                AddAlmostSelection(allUnits[i]);
            }
        }

        void OnLeftDragged(Vector2 positionA, Vector2 positionB)
        {
            positionA = MainCamera.Camera.WorldToScreenPoint(DragStartWorldPosition);
            DragStartWorldPosition = default;
            if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
            Vector2 min = Vector2.Min(positionA, positionB);
            Vector2 max = Vector2.Max(positionA, positionB);
            Selectable[] allUnits = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allUnits.Length; i++)
            {
                Vector3 screenPoint = MainCamera.Camera.WorldToScreenPoint(allUnits[i].transform.position);
                if (screenPoint.x < min.x) continue;
                if (screenPoint.y < min.y) continue;
                if (screenPoint.x > max.x) continue;
                if (screenPoint.y > max.y) continue;
                AddSelection(allUnits[i]);
            }
        }

        internal void ClearAlmostSelection()
        {
            for (int i = 0; i < AlmostSelected.Length; i++)
            {
                if (AlmostSelected[i] == null) continue;
                if (Selected.Contains(AlmostSelected[i])) continue;
                AlmostSelected[i].SelectableState = Selectable.State.None;
            }
            AlmostSelected = new Selectable[0];
        }

        internal void ClearSelection()
        {
            for (int i = 0; i < Selected.Length; i++)
            {
                if (Selected[i] == null) continue;
                Selected[i].SelectableState = Selectable.State.None;
            }
            selectionChanged = true;
            Selected = new Selectable[0];
            ClearAlmostSelection();
        }

        internal void AddAlmostSelection(Selectable obj)
        {
            if (Selected.Contains(obj)) return;
            if (AlmostSelected.Contains(obj)) return;
            AlmostSelected = (new List<Selectable>(AlmostSelected) { obj }).ToArray();
            obj.SelectableState = (Selectable.State)System.Math.Max((int)obj.SelectableState, (int)Selectable.State.Almost);
        }

        internal void AddSelection(Selectable obj)
        {
            if (Selected.Contains(obj)) return;
            selectionChanged = true;
            Selected = (new List<Selectable>(Selected) { obj }).ToArray();
            obj.SelectableState = Selectable.State.Selected;
        }

        void OnGUI()
        {
            if (!MouseLeftButton.IsDragging || !MouseLeftButton.IsActive)
            { return; }

            Rect rect = Utilities.UnityUtils.GetScreenRect(MainCamera.Camera.WorldToScreenPoint(DragStartWorldPosition), Mouse.Position);
            Utilities.UnityUtils.DrawScreenRect(rect, SelectionBoxColor);
            Utilities.UnityUtils.DrawScreenRectBorder(rect, SelectionBoxBorderWidth, SelectionBoxBorderColor);
        }

        public bool HandleCursorLock(out CursorLockMode locked)
        {
            locked = CursorLockMode.None;
            return false;
        }
        public bool HandleCursor()
        {
            if (!MouseCondition()) return false;

            if (MouseLeftButton.IsDragging)
            {
                CursorManager.SetCursor();
                return true;
            }

            Vector3 mousePosition = Mouse.Position;

            if (mousePosition.x == int.MinValue ||
                mousePosition.y == int.MinValue ||
                !mousePosition.IsOk())
            { return false; }

            Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
            float maxDistance = 500f;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.isTrigger) continue;
                if (hits[i].transform.gameObject.HasComponent<Selectable>())
                {
                    CursorSelect.Set();
                    return true;
                }
            }
            if (Selected != null && Selected.Length > 0)
            {
                for (int i = 0; i < Selected.Length; i++)
                {
                    if (Selected[i] == null) continue;

                    if (Selected[i].HasComponent<VehicleEngine>())
                    {
                        CursorGoTo.Set();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
