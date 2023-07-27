using Authentication;

using Game.Components;
using Game.Managers;
using Game.UI;
using Game.UI.Components;

using System.Linq;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Managers
{
    public class TakeControlManager : NetworkBehaviour, ICanChangeCursorImage
    {
        static TakeControlManager instance;

        internal static TakeControlManager Instance => instance;

        internal bool IsControlling =>
            ((Object)ControllingObject) != null &&
            ControllingObject.IAmControllingThis();

        public int CursorPriority => 5;

        [SerializeField, ReadOnly] CameraController CameraController;
        [SerializeField] ICanTakeControl ControllingObject;
        [SerializeField] Transform IngameCursorBlue;
        [SerializeField] Transform IngameCursorRed;
        [SerializeField] float IngameCursorScale;

        [SerializeField] Transform[] CursorPriorities = new Transform[0];

        [SerializeField] LayerMask CursorHitLayer;

        InputUtils.AdvancedMouse LeftMouse;

        ICanTakeControl[] units = new ICanTakeControl[0];

        [SerializeField, ReadOnly] float EnableMouseCooldown = 0f;
        [SerializeField, ReadOnly] internal bool IsScoping;

        [SerializeField, ReadOnly] float TimeToNextUnitCollecting = .5f;

        [SerializeField] CursorConfig CursorConfig;

        NetworkList<ulong> ControllingObjects;

        [Header("UI")]
        [SerializeField] Projectiles Projectiles;
        [SerializeField, ReadOnly] UIDocument UI;
        [SerializeField] VisualTreeAsset ProjectileButton;

        InputUtils.PriorityKey KeyEsc;

        ProgressBar BarHealth;

        void OnEnable()
        {
            CameraController.OnCameraModeChanged += OnCameraModeChanged;
        }

        void OnDisable()
        {
            CameraController.OnCameraModeChanged -= OnCameraModeChanged;
        }

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Instance already registered, destroying self");
                Object.Destroy(this);
                return;
            }
            instance = this;
        }

        void Start()
        {
            CameraController = FindObjectOfType<CameraController>();
            if (CameraController == null)
            { Debug.LogError($"[{nameof(TakeControlManager)}]: CameraController is null", this); }

            {
                TakeControlUI takeControlUiObject = FindObjectOfType<TakeControlUI>(true);
                if (takeControlUiObject == null)
                { Debug.LogError($"[{nameof(TakeControlManager)}]: UI not found", this); }
                else if (!takeControlUiObject.TryGetComponent(out UI))
                { Debug.LogError($"[{nameof(TakeControlManager)}]: UI does not have an UIDocument", takeControlUiObject); }
            }

            HideCursor(IngameCursorBlue);
            HideCursor(IngameCursorRed);

            LeftMouse = new InputUtils.AdvancedMouse(MouseButton.Left, 11, InputCondition);
            LeftMouse.OnDown += OnLeftMouseDown;

            CursorImageManager.Instance.Register(this);

            KeyEsc = new InputUtils.PriorityKey(KeyCode.Escape, 1, EscKeyCondition);
            KeyEsc.OnDown += OnKeyEsc;

            ControllingObjects = new NetworkList<ulong>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            ControllingObjects.OnListChanged += ControllingObjectsChanged;
            ControllingObjects.Initialize(this);
        }

        private void ControllingObjectsChanged(NetworkListEvent<ulong> changeEvent)
        {
            if (!NetcodeUtils.IsClient)
            { return; }

            Debug.Log($"[{nameof(TakeControlManager)}]: Server sent a RefreshControlling command, so ...", this);

            Debug.Log($"[{nameof(TakeControlManager)}]: {nameof(ControllingObjects)} = {ControllingObjects}");

            ulong controllingThis = ControllingObjects.Get((int)NetworkManager.LocalClientId, ulong.MaxValue);
            if (controllingThis == ulong.MaxValue)
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Not controlling anything");
                LoseControlClient();
                return;
            }

            if (!NetcodeUtils.FindGameObject(controllingThis, out var controlling))
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Object {controlling} not found", this);
                LoseControlClient();
                return;
            }

            if (!controlling.TryGetComponent<ICanTakeControl>(out var unit))
            {
                Debug.LogError($"[{nameof(TakeControlManager)}]: Object {controlling} does not have a {nameof(ICanTakeControl)} component", controlling);
                LoseControlClient();
                return;
            }

            TakeControlClient(unit);
            Debug.Log($"[{nameof(TakeControlManager)}]: Controlling object {unit}", unit.GetObject().gameObject);
        }

        void OnKeyEsc()
        {
            EnableMouseCooldown = 1f;
            LoseControl();
        }

        bool EscKeyCondition() => IsControlling;

        bool InputCondition() =>
            !MenuManager.AnyMenuVisible &&
            !BuildingManager.Instance.IsBuilding;

        void Update()
        {
            if (!InputCondition())
            { return; }

            if (!IsControlling)
            { return; }

            if ((Object)ControllingObject == null ||
                !ControllingObject.IAmControllingThis())
            {
                LoseControl();
                return;
            }

            if (EnableMouseCooldown <= 0f &&
                !MouseManager.IsPointerOverUI())
            { ControllingObject.DoFrequentInput(); }
        }

        void FixedUpdate()
        {
            if (TimeToNextUnitCollecting > 0f)
            {
                TimeToNextUnitCollecting -= Time.fixedDeltaTime;
            }
            else
            {
                TimeToNextUnitCollecting = 5f;
                units = GameObject.FindObjectsOfType<MonoBehaviour>(false).OfType<ICanTakeControl>().ToArray();
            }

            if (!InputCondition())
            { return; }

            if (EnableMouseCooldown <= 0f)
            {
                if (IsControlling &&
                    !MouseManager.IsPointerOverUI())
                {
                    IsScoping =
                        Input.GetKey(KeyCode.LeftShift) &&
                        CameraController.cameraMode == CameraMode.Normal;

                    if (MouseManager.MouseOnWindow)
                    { ControllingObject.DoInput(); }
                }
                else
                { IsScoping = false; }
            }
            else
            {
                EnableMouseCooldown -= Time.fixedUnscaledDeltaTime;
                IsScoping = false;
            }

            if (IsControlling)
            {
                if (ControllingObject is ICanTakeControlAndHasTurret hasTurret)
                {
                    if (hasTurret.Turret != null) SetCursor(IngameCursorRed, hasTurret.Turret.PredictImpact() ?? hasTurret.Turret.TargetPosition);
                    else HideCursor(IngameCursorRed);
                }

                if (MouseManager.MouseOnWindow)
                {
                    SetCursor(IngameCursorBlue, MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition, CursorHitLayer));
                }

                if (ControllingObject is Unit controllingUnit)
                {
                    BarHealth.value = controllingUnit.NormalizedHP;
                }
                else if (ControllingObject is Building controllingBuilding)
                {
                    BarHealth.value = controllingBuilding.NormalizedHP;
                }
            }

            if (IsScoping)
            {
                Turret turret = null;
                if (ControllingObject is BuildingAttacker obj1)
                { turret = obj1.Turret; }
                else if (ControllingObject is Unit obj2)
                { turret = obj2.Turret; }

                if (turret != null)
                { CameraController.CurrentScope = turret.Scope; }
                else
                { CameraController.CurrentScope = null; }
            }
            else
            { CameraController.CurrentScope = null; }
        }

        ICanTakeControl GetControllableAt(Vector3 worldPosition)
        {
            System.ValueTuple<int, float> closest = units.ClosestI(worldPosition);

            if (closest.Item2 < 5f)
            {
                if (closest.Item1 < 0 || closest.Item1 >= units.Length) return null;
                return units[closest.Item1];
            }

            return null;
        }

        void OnLeftMouseDown(Vector2 position, float holdTime)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return;
            if (!MouseManager.MouseOnWindow) return;

            var worldPosition = MainCamera.Camera.ScreenToWorldPosition(position, CursorHitLayer);

            var controllable = GetControllableAt(worldPosition);

            if (controllable.GetObject() != null)
            {
                EnableMouseCooldown = 1f;
                TakeControl(controllable);
            }
        }

        void OnCameraModeChanged(CameraMode mode)
        {
            SetWindowCursor();
        }

        void SetCursor(Transform cursor, Vector3 position)
        {
            for (int i = 0; i < CursorPriorities.Length; i++)
            {
                if (cursor == CursorPriorities[i])
                { break; }
                if (Vector3.Distance(CursorPriorities[i].position, position) < 1f)
                {
                    HideCursor(cursor);
                    return;
                }
            }
            if (!cursor.gameObject.activeSelf) cursor.gameObject.SetActive(true);
            cursor.position = position;
            cursor.localScale = IngameCursorScale * Mathf.Clamp(Vector3.Distance(CameraController.CameraPosition, position) * .1f, .1f, 20f) * Vector3.one;
        }

        void HideCursor(Transform cursor)
        {
            cursor.position = new Vector3(0f, -50f, 0f);
            cursor.localScale = Vector3.one * .5f;
        }

        void SetWindowCursor()
        {
            if (CameraController.cameraMode == CameraMode.Normal)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            }
        }

        void TakeControl(ICanTakeControl unit)
        {
            if (unit.SomebodyElseControllingThis())
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Somebody else controlling this", unit.GetObject());
                return;
            }

            if (IsClient)
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Send take control request to server (trying to take control over object {unit.GetObject().GetComponent<NetworkObject>().NetworkObjectId})");
                WantToTakeControl_ServerRpc(unit.GetObject().GetComponent<NetworkObject>().NetworkObjectId);
                return;
            }

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            if ((Object)ControllingObject == (Object)unit) return;
            LoseControl();
            SetWindowCursor();

            CameraController.FollowObject = ((Component)unit).transform;
            CameraController.JustFollow = false;
            ControllingObject = unit;

            if (ControllingObject.GetObject() != null &&
                ControllingObject.GetObject().TryGetComponent(out HoveringLabel label) &&
                AuthManager.RemoteAccountProvider is IRemoteAccountProviderWithCustomID<ulong> remoteAccounts)
            {
                label.Show(remoteAccounts.Get(NetworkManager.LocalClientId)?.DisplayName);
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.IsListening)
            {
                ControllingObjects.Set((int)NetworkManager.LocalClientId, unit.GetObject().GetComponent<NetworkObject>().NetworkObjectId, ulong.MaxValue);
            }

            UI.gameObject.SetActive(true);
            BarHealth = UI.rootVisualElement.Q<ProgressBar>("bar-health");
            UI.rootVisualElement.Q<VisualElement>(null, "unity-progress-bar__progress").style.backgroundColor = new StyleColor(Color.green);
            UI.rootVisualElement.Q<VisualElement>("container-ammo").Clear();

            {
                Turret turret = ((Component)unit).GetComponentInChildren<Turret>();
                if (turret != null)
                {
                    if (turret.projectile != null)
                    {
                        GameObject projectile = turret.projectile;

                        TemplateContainer newButton = ProjectileButton.Instantiate();

                        if (TryGetProjectile(projectile, out var projectileInfo))
                        {
                            newButton.Q<Label>("label-name").text = projectileInfo.Name;
                            newButton.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(projectileInfo.Image);
                        }
                        else
                        {
                            newButton.Q<Label>("label-name").text = projectile.name;
                        }

                        if (!newButton.ClassListContains("ammo-selected"))
                        { newButton.AddToClassList("ammo-selected"); }

                        newButton.name = $"ammo-{0}";

                        UI.rootVisualElement.Q<VisualElement>("container-ammo").Add(newButton);
                    }
                    else
                    {
                        for (int i = 0; i < turret.Projectiles.Length; i++)
                        {
                            GameObject projectile = turret.Projectiles[i];

                            TemplateContainer newButton = ProjectileButton.Instantiate();

                            if (TryGetProjectile(projectile, out var projectileInfo))
                            {
                                newButton.Q<Label>("label-name").text = projectileInfo.Name;
                                newButton.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(projectileInfo.Image);
                            }
                            else
                            {
                                newButton.Q<Label>("label-name").text = projectile.name;
                            }

                            if (turret.SelectedProjectile == i &&
                                !newButton.ClassListContains("ammo-selected"))
                            { newButton.AddToClassList("ammo-selected"); }

                            newButton.name = $"ammo-{i}";

                            UI.rootVisualElement.Q<VisualElement>("container-ammo").Add(newButton);
                        }
                    }
                }
            }
        }

        void TakeControlClient(ICanTakeControl unit)
        {
            if (unit.SomebodyElseControllingThis())
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Somebody else controlling this", unit.GetObject());
                return;
            }

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            if ((Object)ControllingObject == (Object)unit) return;
            LoseControl();
            SetWindowCursor();

            CameraController.FollowObject = ((Component)unit).transform;
            CameraController.JustFollow = false;
            ControllingObject = unit;

            UI.gameObject.SetActive(true);
            BarHealth = UI.rootVisualElement.Q<ProgressBar>("bar-health");
            UI.rootVisualElement.Q<VisualElement>(null, "unity-progress-bar__progress").style.backgroundColor = new StyleColor(Color.green);
            UI.rootVisualElement.Q<VisualElement>("container-ammo").Clear();

            {
                Turret turret = ((Component)unit).GetComponentInChildren<Turret>();
                if (turret != null)
                {
                    if (turret.projectile != null)
                    {
                        GameObject projectile = turret.projectile;

                        TemplateContainer newButton = ProjectileButton.Instantiate();

                        if (TryGetProjectile(projectile, out var projectileInfo))
                        {
                            newButton.Q<Label>("label-name").text = projectileInfo.Name;
                            newButton.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(projectileInfo.Image);
                        }
                        else
                        {
                            newButton.Q<Label>("label-name").text = projectile.name;
                        }

                        if (!newButton.ClassListContains("ammo-selected"))
                        { newButton.AddToClassList("ammo-selected"); }

                        newButton.name = $"ammo-{0}";

                        UI.rootVisualElement.Q<VisualElement>("container-ammo").Add(newButton);
                    }
                    else
                    {
                        for (int i = 0; i < turret.Projectiles.Length; i++)
                        {
                            GameObject projectile = turret.Projectiles[i];

                            TemplateContainer newButton = ProjectileButton.Instantiate();

                            if (TryGetProjectile(projectile, out var projectileInfo))
                            {
                                newButton.Q<Label>("label-name").text = projectileInfo.Name;
                                newButton.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(projectileInfo.Image);
                            }
                            else
                            {
                                newButton.Q<Label>("label-name").text = projectile.name;
                            }

                            if (turret.SelectedProjectile == i &&
                                !newButton.ClassListContains("ammo-selected"))
                            { newButton.AddToClassList("ammo-selected"); }

                            newButton.name = $"ammo-{i}";

                            UI.rootVisualElement.Q<VisualElement>("container-ammo").Add(newButton);
                        }
                    }
                }
            }
        }

        internal void UpdateSelectedProjectile(int selected)
        {
            var container = UI.rootVisualElement.Q<VisualElement>("container-ammo");
            var buttons = container.Children();
            foreach (var button in buttons)
            {
                int i = int.Parse(button.name.Split('-')[1]);
                if (selected == i)
                {
                    if (!button.ClassListContains("ammo-selected"))
                    { button.AddToClassList("ammo-selected"); }
                    if (button.ClassListContains("ammo-unselected"))
                    { button.RemoveFromClassList("ammo-unselected"); }
                }
                else
                {
                    if (button.ClassListContains("ammo-selected"))
                    { button.RemoveFromClassList("ammo-selected"); }
                    if (!button.ClassListContains("ammo-unselected"))
                    { button.AddToClassList("ammo-unselected"); }
                }
            }
        }

        bool TryGetProjectile(GameObject prefab, out Projectiles.Projectile projectile)
        {
            for (int i = 0; i < Projectiles.Length; i++)
            {
                if (Projectiles[i].Prefab == prefab)
                {
                    projectile = Projectiles[i];
                    return true;
                }
            }
            projectile = default;
            return false;
        }

        void LoseControl()
        {
            if (IsClient)
            {
                if (ControllingObject.GetObject() != null)
                {
                    Debug.Log($"[{nameof(TakeControlManager)}]: Send lose control request to server (trying to lose control over object {ControllingObject.GetObject().GetComponent<NetworkObject>().NetworkObjectId})");
                    LoseControl_ServerRpc();
                }
                return;
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.IsListening &&
                NetworkManager.IsServer)
            {
                ControllingObjects.Set((int)NetworkManager.LocalClientId, ulong.MaxValue, ulong.MaxValue);
            }

            UI.gameObject.SetActive(false);

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            HideCursor(IngameCursorBlue);
            HideCursor(IngameCursorRed);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            if (ControllingObject.GetObject() == null) return;

            if (ControllingObject.GetObject().TryGetComponent(out HoveringLabel label))
            {
                label.Hide();
            }

            if (ControllingObject is ICanTakeControlAndHasTurret hasTurret &&
                hasTurret.Turret != null)
            { hasTurret.Turret.SetTarget(Vector3.zero); }

            CameraController.FollowObject = null;
            ControllingObject = null;
        }

        void LoseControlClient()
        {
            UI.gameObject.SetActive(false);

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            HideCursor(IngameCursorBlue);
            HideCursor(IngameCursorRed);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            if (ControllingObject.GetObject() == null) return;

            CameraController.FollowObject = null;
            ControllingObject = null;
        }

        internal void ControllableDestroyed()
        {
            if (!gameObject.scene.isLoaded) return;
            LoseControl();
        }

        public bool YouCanChangeCursor()
        {
            if (!InputCondition()) return false;
            if (IsControlling) return false;
            if (!Input.GetKey(KeyCode.LeftControl)) return false;

            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition, CursorHitLayer);

            ICanTakeControl controllable = GetControllableAt(worldPosition);

            if ((Object)controllable == null) return false;

            CursorConfig.SetCursor();
            return true;
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void WantToTakeControl_ServerRpc(ulong objectID, ServerRpcParams serverRpcParams = default)
        {
            Debug.Log($"[{nameof(TakeControlManager)}]: Client {serverRpcParams.Receive.SenderClientId} wants to take control of object {objectID} ...");

            if (ControllingObjects.Get((int)serverRpcParams.Receive.SenderClientId, ulong.MaxValue) == ulong.MaxValue)
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Object {objectID} is now controlled by {(int)serverRpcParams.Receive.SenderClientId}");

                if (NetcodeUtils.FindGameObject(objectID, out GameObject unitObject2) &&
                    unitObject2.TryGetComponent(out HoveringLabel label) &&
                    AuthManager.RemoteAccountProvider is IRemoteAccountProviderWithCustomID<ulong> remoteAccounts)
                {
                    label.Show(remoteAccounts.Get(serverRpcParams.Receive.SenderClientId)?.DisplayName);
                }

                ControllingObjects.Set((int)serverRpcParams.Receive.SenderClientId, objectID, ulong.MaxValue);

                return;
            }

            if (NetcodeUtils.FindGameObject(objectID, out GameObject unitObject) &&
                unitObject.TryGetComponent(out ICanTakeControl unit) &&
                AnybodyControlling(unit, out ulong controlledBy) &&
                controlledBy == serverRpcParams.Receive.SenderClientId)
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Object {objectID} is now controlled by {(int)serverRpcParams.Receive.SenderClientId}");

                if (unitObject.TryGetComponent(out HoveringLabel label) &&
                    AuthManager.RemoteAccountProvider is IRemoteAccountProviderWithCustomID<ulong> remoteAccounts)
                {
                    label.Show(remoteAccounts.Get(serverRpcParams.Receive.SenderClientId)?.DisplayName);
                }

                ControllingObjects.Set((int)serverRpcParams.Receive.SenderClientId, objectID, ulong.MaxValue);

                return;
            }

            Debug.LogWarning($"[{nameof(TakeControlManager)}]: Bruh case", this);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void LoseControl_ServerRpc(ServerRpcParams serverRpcParams = default)
        {
            Debug.Log($"[{nameof(TakeControlManager)}]: Client {serverRpcParams.Receive.SenderClientId} lost control of an object ...");

            ulong lostControlObject = ControllingObjects.Get((int)serverRpcParams.Receive.SenderClientId, ulong.MaxValue);

            if (NetcodeUtils.FindGameObject(lostControlObject, out GameObject unitObject) &&
                unitObject.TryGetComponent(out HoveringLabel label))
            {
                label.Hide();
            }

            ControllingObjects.Set((int)serverRpcParams.Receive.SenderClientId, ulong.MaxValue, ulong.MaxValue);

            Debug.Log($"[{nameof(TakeControlManager)}]: Object {lostControlObject} is now controlled by nobody", this);
        }

        internal bool IAmControlling(ICanTakeControl @this)
        {
            if (NetcodeUtils.IsOffline)
            { return ControllingObject.GetObject() == @this.GetObject(); }

            if ((int)NetworkManager.LocalClientId >= ControllingObjects.Count ||
                (int)NetworkManager.LocalClientId < 0)
            { return false; }

            if (!@this.GetObject().TryGetComponent(out NetworkObject networkObject))
            {
                Debug.LogWarning($"Object {@this} does not have a {nameof(NetworkObject)} component", @this.GetObject());
                return false;
            }

            return networkObject.NetworkObjectId == ControllingObjects[(int)NetworkManager.LocalClientId];
        }

        internal bool AnybodyControlling(ICanTakeControl @this)
            => AnybodyControlling(@this, out _);
        internal bool AnybodyControlling(ICanTakeControl @this, out ulong clientID)
        {
            clientID = default;

            try
            {
                if (NetcodeUtils.IsOffline)
                { return ControllingObject.GetObject() == @this.GetObject(); }
            }
            catch (System.NullReferenceException)
            { return false; }

            if (!@this.GetObject().TryGetComponent(out NetworkObject networkObject))
            {
                Debug.LogWarning($"Object {@this} does not have a {nameof(NetworkObject)} component", @this.GetObject());
                return false;
            }

            for (int i = 0; i < ControllingObjects.Count; i++)
            {
                if (ControllingObjects[i] == networkObject.NetworkObjectId)
                {
                    clientID = (ulong)i;
                    return true;
                }
            }
            return false;
        }
    }
}

namespace Game.Components
{
    public interface ICanTakeControl : IAmObject
    {
        void DoInput();
        void DoFrequentInput();

        public void OnDestroy();
    }

    public interface ICanTakeControlAndHasTurret : ICanTakeControl
    {
        public Turret Turret { get; }
    }

    public interface IAmObject
    {

    }
}

public static class ICanTakeControlExtensions
{
    public static bool IAmControllingThis(this ICanTakeControl self)
    {
        if (!self.AnybodyControllingThis()) return false;
        if (NetcodeUtils.IsOffline)
        { return true; }

        if (TakeControlManager.Instance == null)
        { return false; }

        return TakeControlManager.Instance.IAmControlling(self);
    }

    public static bool SomebodyElseControllingThis(this ICanTakeControl self)
    {
        if (!self.AnybodyControllingThis()) return false;
        if (NetcodeUtils.IsOffline)
        { return false; }

        if (TakeControlManager.Instance == null)
        { return false; }

        return !TakeControlManager.Instance.IAmControlling(self);
    }

    public static bool AnybodyControllingThis(this ICanTakeControl self)
    {
        if (TakeControlManager.Instance == null)
        { return false; }
        return TakeControlManager.Instance.AnybodyControlling(self);
    }

    public static string ControlledBy(this ICanTakeControl self)
    {
        if (TakeControlManager.Instance == null)
        { return "nobody"; }
        return TakeControlManager.Instance.AnybodyControlling(self, out ulong controllingBy) ? $"client {controllingBy}" : "nobody";
    }
}

public static class IAmObjectExtensions
{
    public static Component GetObject(this Game.Components.IAmObject self) => (Component)self;
}
