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
        // MeshCollider SelectionMeshCollider;

        [Header("Cursors")]
        [SerializeField] CursorConfig CursorSelect;
        [SerializeField] CursorConfig CursorGoTo;

        [SerializeField, ReadOnly] UnitCommandManager CommandManager;

        bool selectionChanged = false;
        internal event SelectionChangedEvent OnSelectionChanged;

        AdvancedMouse MouseLeftButton;
        AdvancedMouse MouseRightButton;

        public int CursorPriority => 4;

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

            if (!MouseCondition()) return;

            if (AlmostSelectTimer <= 0f)
            {
                AlmostSelectTimer = .5f;
                if (MouseLeftButton.IsDragging)
                {
                    // if (SelectionMeshCollider == null)
                    OnDrag(MouseLeftButton.DragStart, Input.mousePosition);
                }
                else
                {
                    OnMouseMove();
                }
            }
            else
            {
                AlmostSelectTimer -= Time.deltaTime;
            }
        }

        void OnMouseMove()
        {
            ClearAlmostSelection();
            Vector3 mousePosition = Input.mousePosition;

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

        void OnLeftClicked(AdvancedMouse sender)
        {
            if (sender.HoldTime >= QuickCommandManager.HOLD_TIME_REQUIREMENT) return;
            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(AdvancedMouse.Position, Utilities.DefaultLayerMasks.Solids, out RaycastHit[] hits);
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
            /*
            Vector3[] worldCorners = new Vector3[4];
            Vector3[] vectors = new Vector3[4];
            Vector2[] screenCorners = GetSelectionBoundingBox(positionA, positionB);

            for (int i = 0; i < screenCorners.Length; i++)
            {
                Vector2 corner = screenCorners[i];
                Ray ray = Camera.main.ScreenPointToRay(corner);
                Vector3 rayHit = Camera.main.ScreenToWorldPosition(corner);
                rayHit.y = 0f;

                Debug.DrawLine(ray.origin, rayHit, Color.red, .5f);

                worldCorners[i] = rayHit;
                vectors[i] = ray.origin - rayHit;
            }

            DragFinished = false;
            ClearAlmostSelection();

            Mesh SelectionMesh = GenerateSelectionMesh(worldCorners, vectors);

            Utilities.Debug.DrawMesh(SelectionMesh, Color.blue, .5f);

            SelectionMeshCollider = gameObject.AddComponent<MeshCollider>();
            SelectionMeshCollider.sharedMesh = SelectionMesh;
            SelectionMeshCollider.convex = true;
            SelectionMeshCollider.isTrigger = true;

            Destroy(SelectionMeshCollider, 0.02f);
            */
        }

        void OnLeftDragged(Vector2 positionA, Vector2 positionB)
        {
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
            /*
            Vector3[] worldCorners = new Vector3[4];
            Vector3[] vectors = new Vector3[4];
            Vector2[] screenCorners = GetSelectionBoundingBox(positionA, positionB);

            for (int i = 0; i < screenCorners.Length; i++)
            {
                Vector2 corner = screenCorners[i];
                Ray ray = Camera.main.ScreenPointToRay(corner);
                Vector3 rayHit = Camera.main.ScreenToWorldPosition(corner);
                rayHit.y = 0f;

                Debug.DrawLine(ray.origin, rayHit, Color.red, 2f);

                worldCorners[i] = rayHit;
                vectors[i] = ray.origin - rayHit;
            }

            DragFinished = true;
            if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();

            Mesh SelectionMesh = GenerateSelectionMesh(worldCorners, vectors);

            Utilities.Debug.DrawMesh(SelectionMesh, Color.blue, 2f);

            SelectionMeshCollider = gameObject.AddComponent<MeshCollider>();
            SelectionMeshCollider.sharedMesh = SelectionMesh;
            SelectionMeshCollider.convex = true;
            SelectionMeshCollider.isTrigger = true;

            Destroy(SelectionMeshCollider, 0.02f);
            */
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out Selectable unit)) return;

            //if (DragFinished)
            //{
            //    AddSelection(unit);
            //}
            //else
            //{
            AddAlmostSelection(unit);
            //}
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

            Rect rect = Utilities.UnityUtils.GetScreenRect(MouseLeftButton.DragStart, Input.mousePosition);
            Utilities.UnityUtils.DrawScreenRect(rect, SelectionBoxColor);
            Utilities.UnityUtils.DrawScreenRectBorder(rect, SelectionBoxBorderWidth, SelectionBoxBorderColor);
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/global_selection.cs"/><br/>
        /// Generates and normalizes the screen rectangle from <paramref name="pointA"/> and <paramref name="pointB"/>.
        /// </summary>
        /// <returns>
        /// The rectangle's corners as an array with length of 4.
        /// </returns>
        Vector2[] GetSelectionBoundingBox(Vector2 pointA, Vector2 pointB)
        {
            Vector2 point1;
            Vector2 point2;
            Vector2 point3;
            Vector2 point4;

            if (pointA.x < pointB.x)
            {
                if (pointA.y > pointB.y)
                {
                    point1 = pointA;
                    point2 = new Vector2(pointB.x, pointA.y);
                    point3 = new Vector2(pointA.x, pointB.y);
                    point4 = pointB;
                }
                else
                {
                    point1 = new Vector2(pointA.x, pointB.y);
                    point2 = pointB;
                    point3 = pointA;
                    point4 = new Vector2(pointB.x, pointA.y);
                }
            }
            else
            {
                if (pointA.y > pointB.y)
                {
                    point1 = new Vector2(pointB.x, pointA.y);
                    point2 = pointA;
                    point3 = pointB;
                    point4 = new Vector2(pointA.x, pointB.y);
                }
                else
                {
                    point1 = pointB;
                    point2 = new Vector2(pointA.x, pointB.y);
                    point3 = new Vector2(pointB.x, pointA.y);
                    point4 = pointA;
                }
            }

            return new Vector2[4] { point1, point2, point3, point4, };
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/global_selection.cs"/><br/>
        /// Generates the selection mesh.
        /// </summary>
        /// <param name="corners">
        /// The corners on the ground. These will be the <b>bottom part</b> of the bounding box.
        /// </param>
        /// <param name="vectors">
        /// The vectors of the box. These will be the <b>top part</b> of the bounding box.
        /// </param>
        Mesh GenerateSelectionMesh(Vector3[] corners, Vector3[] vectors)
        {
            Vector3[] vertices = new Vector3[8];
            int[] triangles = {
                0, 1, 2,
                2, 1, 3,
                4, 6, 0,
                0, 6, 2,
                6, 7, 2,
                2, 7, 3,
                7, 5, 3,
                3, 5, 1,
                5, 0, 1,
                1, 4, 0,
                4, 5, 6,
                6, 5, 7,
            };

            for (int i = 0; i < 4; i++)
            {
                vertices[i] = corners[i];
            }

            for (int i = 4; i < 8; i++)
            {
                vertices[i] = corners[i - 4] + vectors[i - 4];
            }

            return new Mesh()
            {
                vertices = vertices,
                triangles = triangles,
            };
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

            Vector3 mousePosition = Input.mousePosition;

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
