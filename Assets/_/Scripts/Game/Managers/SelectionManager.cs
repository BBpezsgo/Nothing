using Game.Components;
using Game.UI;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Game.Managers
{
    public class SelectionManager : SingleInstance<SelectionManager>, ICanChangeCursorImage
    {
        internal delegate void SelectionChangedEvent();

        [SerializeField, ReadOnly, NonReorderable] internal ISelectable[] Selected = new ISelectable[0];
        [SerializeField, ReadOnly, NonReorderable] ISelectable[] AlmostSelected = new ISelectable[0];

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

        InputUtils.AdvancedMouse MouseLeftButton;
        InputUtils.AdvancedMouse MouseRightButton;

        public int CursorPriority => 4;

        void Start()
        {
            CommandManager = FindObjectOfType<UnitCommandManager>();
            if (CommandManager == null)
            { Debug.LogWarning($"[{nameof(SelectionManager)}]: {nameof(CommandManager)} is null"); }

            MouseLeftButton = new InputUtils.AdvancedMouse(MouseButton.Left, 10, MouseCondition);
            MouseLeftButton.OnDragged += OnLeftDragged;
            MouseLeftButton.OnClick += OnLeftClicked;

            MouseRightButton = new InputUtils.AdvancedMouse(MouseButton.Right, 10, MouseCondition);
            MouseRightButton.OnClick += OnRightClicked;

            CursorImageManager.Instance.Register(this);
        }

        void OnRightClicked(Vector2 position, float holdTime)
        {
            ClearSelection();
        }

        bool MouseCondition() =>
            CameraController.Instance != null &&
            (!CameraController.Instance.IsFollowing || CameraController.Instance.JustFollow) &&
            !TakeControlManager.Instance.IsControlling &&
            !BuildingManager.Instance.IsBuilding &&
            !QuickCommandManager.Instance.IsShown &&
            !MenuManager.AnyMenuVisible;

        void FixedUpdate()
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
                AlmostSelectTimer -= Time.fixedDeltaTime;
            }
        }

        void OnMouseMove()
        {
            ClearAlmostSelection();
            Vector3 mousePosition = Input.mousePosition;

            if (mousePosition.x == int.MinValue ||
                mousePosition.y == int.MinValue)
            { return; }

            Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
            float maxDistance = 500f;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.isTrigger) continue;
                if (hits[i].transform.gameObject.TryGetComponent<ISelectable>(out var unit))
                {
                    AddAlmostSelection(unit);
                    return;
                }
            }
        }

        void OnLeftClicked(Vector2 screenPosition, float holdTime)
        {
            if (holdTime >= QuickCommandManager.HOLD_TIME_REQUIREMENT) return;
            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(screenPosition, out RaycastHit[] hits);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject.TryGetComponent<ISelectable>(out var unit))
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
            ISelectable[] allUnits = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<ISelectable>().ToArray();
            for (int i = 0; i < allUnits.Length; i++)
            {
                var screenPoint = Camera.main.WorldToScreenPoint(((MonoBehaviour)allUnits[i]).transform.position);
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
            ISelectable[] allUnits = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<ISelectable>().ToArray();
            for (int i = 0; i < allUnits.Length; i++)
            {
                var screenPoint = MainCamera.Camera.WorldToScreenPoint(((MonoBehaviour)allUnits[i]).transform.position);
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
            if (!other.TryGetComponent(out ISelectable unit)) return;

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
                AlmostSelected[i].SelectableState = ISelectable.State.None;
            }
            AlmostSelected = new ISelectable[0];
        }

        internal void ClearSelection()
        {
            for (int i = 0; i < Selected.Length; i++)
            {
                if (Selected[i] == null) continue;
                Selected[i].SelectableState = ISelectable.State.None;
            }
            selectionChanged = true;
            Selected = new ISelectable[0];
            ClearAlmostSelection();
        }

        internal void AddAlmostSelection(ISelectable obj)
        {
            if (Selected.Contains(obj)) return;
            if (AlmostSelected.Contains(obj)) return;
            AlmostSelected = (new List<ISelectable>(AlmostSelected) { obj }).ToArray();
            obj.SelectableState = (ISelectable.State)System.Math.Max((int)obj.SelectableState, (int)ISelectable.State.Almost);
        }

        internal void AddSelection(ISelectable obj)
        {
            if (Selected.Contains(obj)) return;
            selectionChanged = true;
            Selected = (new List<ISelectable>(Selected) { obj }).ToArray();
            obj.SelectableState = ISelectable.State.Selected;
        }

        void OnGUI()
        {
            if (!MouseLeftButton.IsDragging || !MouseLeftButton.IsActive)
            { return; }

            Rect rect = Utilities.Utils.GetScreenRect(MouseLeftButton.DragStart, Input.mousePosition);
            Utilities.Utils.DrawScreenRect(rect, SelectionBoxColor);
            Utilities.Utils.DrawScreenRectBorder(rect, SelectionBoxBorderWidth, SelectionBoxBorderColor);
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

        public bool YouCanChangeCursor()
        {
            if (!MouseCondition()) return false;

            if (MouseLeftButton.IsDragging)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return true;
            }

            Vector3 mousePosition = Input.mousePosition;

            if (mousePosition.x == int.MinValue ||
                mousePosition.y == int.MinValue)
            { return false; }

            Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
            float maxDistance = 500f;
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.isTrigger) continue;
                if (hits[i].transform.gameObject.TryGetComponent<ISelectable>(out var unit))
                {
                    CursorSelect.SetCursor();
                    return true;
                }
            }
            if (Selected != null && Selected.Length > 0)
            {
                for (int i = 0; i < Selected.Length; i++)
                {
                    if (((Component)Selected[i]).HasComponent<VehicleEngine>())
                    {
                        CursorGoTo.SetCursor();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

namespace Game.Components
{
    public interface ISelectable : IComponent
    {
        public enum State : int
        {
            None = 0,
            Almost = 1,
            Selected = 2,
        }

        public State SelectableState { get; set; }
    }
}
