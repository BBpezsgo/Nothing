using System.Collections.Generic;
using System.Linq;
using Authentication;
using Game.Components;
using Game.Managers;
using Game.UI;
using Game.UI.Components;
using InputUtils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities;
using Utilities.Drawers;

namespace Game.Managers
{
    public class TakeControlManager : NetworkBehaviour, ICanChangeCursor
    {
        public enum ReloadIndicatorStyle
        {
            None,
            Dots,
            Circle,
        }

        public enum CrossStyle
        {
            None,
            Cross,
            DiagonalCross,
            Cross3,
        }

        static TakeControlManager instance;

        internal static TakeControlManager Instance => instance;

        internal bool IsControlling =>
            ((Object)ControllingObject) != null &&
            ControllingObject.IAmControllingThis();

        public int CursorPriority => 5;

        [SerializeField, ReadOnly] CameraController CameraController;
        [SerializeField] ICanTakeControl ControllingObject;

        [SerializeField] LayerMask CursorHitLayer;

        AdvancedMouse LeftMouse;

        public AdvancedTouch Touch;

        ICanTakeControl[] units = new ICanTakeControl[0];

        [SerializeField, ReadOnly] float EnableMouseCooldown = 0f;
        [SerializeField, ReadOnly] internal bool IsScoping;
        [SerializeField, ReadOnly] internal CameraLockable Scope;

        [SerializeField, ReadOnly] float TimeToNextUnitCollecting = .5f;

        [SerializeField] CursorConfig CursorConfig;

        NetworkList<ulong> ControllingObjects;

        readonly List<ClientObject> ShouldAlwaysControlObjects = new();

        [Header("UI")]
        [SerializeField] Projectiles Projectiles;
        [SerializeField, ReadOnly] UIDocument UI;
        [SerializeField] VisualTreeAsset ProjectileButton;
        [SerializeField] GUISkin GUISkin;

        [Header("UI - Crosshair")]
        [SerializeField] Color CrosshairColor = Color.white;
        [SerializeField] Color CrosshairColorInaccurate = Color.red;
        [SerializeField] Color CrosshairColorAccurate = Color.green;
        [SerializeField] Color CrosshairColorPrediction = Color.blue;
        [SerializeField] Color ReloadDotsColor = Color.white;
        [SerializeField, Min(0)] int ReloadDots = 16;
        [SerializeField, Min(.5f)] float ReloadDotsRadius = 18f;
        [SerializeField, Min(.5f)] float ReloadDotsSize = 4f;
        [SerializeField, Min(.01f)] float ReloadDotsFadeoutSpeed = 5f;
        [SerializeField, Min(.01f)] float TargetLockAnimationSpeed = 2f;
        [SerializeField, ReadOnly] ReloadIndicatorStyle CurrentReloadIndicatorStyle = ReloadIndicatorStyle.Circle;
        [SerializeField, ReadOnly] CrossStyle CurrentCrossStyle = CrossStyle.Cross;

        Texture2D SphereFilled;
        Rect targetRect = Rect.zero;

        static readonly (float Inner, float Outer) CrossSize = (4f, 12f);
        static readonly Color ShadowColor = new(0f, 0f, 0f, .8f);

        PriorityKey KeyEsc;

        ProgressBar BarHealth;

        [SerializeField] SimpleAnimation ReloadIndicatorFadeoutAnimation;
        [SerializeField] SimpleReversableAnimation TargetLockingAnimation;
        [SerializeField] PingPongAnimationController TargetLockingAnimationController;

        void OnEnable()
        {
            CameraController.OnCameraModeChanged += OnCameraModeChanged;
            ReloadIndicatorFadeoutAnimation = new SimpleAnimation(ReloadDotsFadeoutSpeed, AnimationFunctions.Square);

            TargetLockingAnimationController = new PingPongAnimationController();
            TargetLockingAnimation = new SimpleReversableAnimation(TargetLockAnimationSpeed, TargetLockingAnimationController, v => v);
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

            if (SphereFilled != null)
            { Texture2D.Destroy(SphereFilled); }
            SphereFilled = GUIUtils.GenerateCircleFilled(Vector2Int.one * 32);
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

            LeftMouse = new AdvancedMouse(Mouse.Left, 11, InputCondition);
            LeftMouse.OnDown += OnLeftMouseDown;

            Touch = new AdvancedTouch(2, () => InputCondition() && IsControlling);

            CursorManager.Instance.Register(this);

            KeyEsc = new PriorityKey(KeyCode.Escape, 1, EscKeyCondition);
            KeyEsc.OnDown += OnKeyEsc;

            ControllingObjects = new NetworkList<ulong>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            ControllingObjects.OnListChanged += ControllingObjectsChanged;
            ControllingObjects.Initialize(this);
        }

        public void ShouldAlwaysControl(ulong clientId, ulong objectId, bool should)
        {
            if (NetcodeUtils.IsClient)
            {
                Debug.LogError($"[{nameof(TakeControlManager)}]: This should be not called on clients");
                return;
            }

            for (int i = ShouldAlwaysControlObjects.Count - 1; i >= 0; i--)
            {
                if (ShouldAlwaysControlObjects[i].ClientId != clientId) continue;

                if (!should)
                { ShouldAlwaysControlObjects.RemoveAt(i); }
                return;
            }
            ShouldAlwaysControlObjects.Add(new ClientObject(clientId, objectId));

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject obj))
            {
                Debug.LogError($"[{nameof(TakeControlManager)}]: Network object {objectId} not found", this);
                return;
            }

            if (!obj.gameObject.TryGetComponent(out ICanTakeControl controllableObj))
            {
                Debug.LogError($"[{nameof(TakeControlManager)}]: Object {obj} does not have a component {nameof(ICanTakeControl)}", obj);
                return;
            }

            ControllingObjects.Set((int)clientId, objectId, ulong.MaxValue);

            if (clientId == NetworkManager.ServerClientId)
            {
                if (IsControlling)
                { LoseControl(); }
                TakeControl(controllableObj);
            }
        }

        public bool ShouldIAlwaysControl(ulong clientId, out ulong objectId)
        {
            for (int i = ShouldAlwaysControlObjects.Count - 1; i >= 0; i--)
            {
                if (ShouldAlwaysControlObjects[i].ClientId != clientId) continue;

                objectId = ShouldAlwaysControlObjects[i].ObjectId;
                return true;
            }

            objectId = default;
            return false;
        }

        void ControllingObjectsChanged(NetworkListEvent<ulong> changeEvent)
        {
            if (!NetcodeUtils.IsClient)
            { return; }

            Debug.Log($"[{nameof(TakeControlManager)}]: Server sent a RefreshControlling command, so ...", this);

            Debug.Log($"[{nameof(TakeControlManager)}]: {nameof(ControllingObjects)}: {ControllingObjects.ToReadableString(NetworkManager.SpawnManager.SpawnedObjects)}");

            ulong controllingThis = ControllingObjects.Get((int)NetworkManager.LocalClientId, ulong.MaxValue);
            if (controllingThis == ulong.MaxValue)
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Not controlling anything");
                LoseControlClient();
                return;
            }

            if (!NetcodeUtils.FindGameObject(controllingThis, out GameObject controlling))
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Object {controlling} not found", this);
                LoseControlClient();
                return;
            }

            if (!controlling.TryGetComponent(out ICanTakeControl unit))
            {
                Debug.LogError($"[{nameof(TakeControlManager)}]: Object {controlling} does not have a {nameof(ICanTakeControl)} component", controlling);
                LoseControlClient();
                return;
            }

            TakeControlClient(unit);
            Debug.Log($"[{nameof(TakeControlManager)}]: Controlling object {unit}", ((Component)unit).gameObject);
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
                    IsScoping = Input.GetKey(KeyCode.LeftShift);

                    if (MouseManager.MouseOnWindow)
                    { ControllingObject.DoInput(); }
                }
                else
                {
                    IsScoping = false;
                }
            }
            else
            {
                EnableMouseCooldown -= Time.fixedUnscaledDeltaTime;
                IsScoping = false;
            }

            if (IsScoping)
            {

            }
            else
            {
                Scope = null;
            }

            if (IsControlling)
            {
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

                if (turret != null && turret.Scope != null)
                { CameraController.TryOverrideLock(turret.Scope.GetComponent<CameraLockable>(), CameraLockable.Priorities.ControllableThing); }
                else
                { CameraController.TryOverrideLock(null, CameraLockable.Priorities.ControllableThing); }
            }
            else
            { CameraController.TryOverrideLock(null, CameraLockable.Priorities.ControllableThing); }
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

        void OnLeftMouseDown(AdvancedMouse sender)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return;
            if (!MouseManager.MouseOnWindow) return;

            var worldPosition = MainCamera.Camera.ScreenToWorldPosition(AdvancedMouse.Position, CursorHitLayer);

            var controllable = GetControllableAt(worldPosition);

            if ((Object)controllable != null)
            {
                EnableMouseCooldown = .3f;
                TakeControl(controllable);
            }
        }

        void OnCameraModeChanged(CameraMode mode)
        {
            CursorManager.Instance.ForceUpdate();
        }

        void UpdateUI()
        {
            Turret turret = ControllingObject.Object().GetComponentInChildren<Turret>();

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

        void TakeControl(ICanTakeControl unit)
        {
            if (unit.SomebodyElseControllingThis())
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Somebody else controlling this", (Object)unit);
                return;
            }

            if (IsClient)
            {
                ulong unitID = unit.GetNetworkIDForce();

                Debug.Log($"[{nameof(TakeControlManager)}]: Send take control request to server (trying to take control over object \"{unit.GetGameObject()}\" (networkID: {unitID}))", this);
                WantToTakeControl_ServerRpc(unitID);
                return;
            }

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            if (unit is ICanTakeControlAndHasTurret hasTurret)
            {
                CurrentCrossStyle = hasTurret.CrossStyle;
                CurrentReloadIndicatorStyle = hasTurret.ReloadIndicatorStyle;
            }
            else
            {
                CurrentCrossStyle = CrossStyle.None;
                CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;
            }

            if ((Object)ControllingObject == (Object)unit) return;
            LoseControl();

            if (unit is ICanTakeControlAndHasTurret hasTurret2)
            {
                CurrentCrossStyle = hasTurret2.CrossStyle;
                CurrentReloadIndicatorStyle = hasTurret2.ReloadIndicatorStyle;
            }
            else
            {
                CurrentCrossStyle = CrossStyle.None;
                CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;
            }

            CameraController.FollowObject = ((Component)unit).transform;
            CameraController.JustFollow = false;
            ControllingObject = unit;

            if ((Object)ControllingObject != null &&
                ((Component)ControllingObject).TryGetComponent(out HoveringLabel label) &&
                AuthManager.RemoteAccountProvider is IRemoteAccountProviderWithCustomID<ulong> remoteAccounts)
            {
                label.Show(remoteAccounts.Get(NetworkManager.LocalClientId)?.DisplayName);
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.IsListening)
            {
                ulong unitID = unit.GetNetworkIDForce();
                ControllingObjects.Set((int)NetworkManager.LocalClientId, unitID, ulong.MaxValue);
            }

            UI.gameObject.SetActive(true);
            BarHealth = UI.rootVisualElement.Q<ProgressBar>("bar-health");
            UI.rootVisualElement.Q<VisualElement>(null, "unity-progress-bar__progress").style.backgroundColor = new StyleColor(Color.green);
            UI.rootVisualElement.Q<VisualElement>("container-ammo").Clear();

            CursorManager.Instance.ForceUpdate();

            UpdateUI();
        }

        void TakeControlClient(ICanTakeControl unit)
        {
            if (unit.SomebodyElseControllingThis())
            {
                Debug.Log($"[{nameof(TakeControlManager)}]: Somebody else controlling this", (Object)unit);
                return;
            }

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            if (unit is ICanTakeControlAndHasTurret hasTurret)
            {
                CurrentCrossStyle = hasTurret.CrossStyle;
                CurrentReloadIndicatorStyle = hasTurret.ReloadIndicatorStyle;
            }
            else
            {
                CurrentCrossStyle = CrossStyle.None;
                CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;
            }

            if ((Object)ControllingObject == (Object)unit) return;
            LoseControl();

            if (unit is ICanTakeControlAndHasTurret hasTurret2)
            {
                CurrentCrossStyle = hasTurret2.CrossStyle;
                CurrentReloadIndicatorStyle = hasTurret2.ReloadIndicatorStyle;
            }
            else
            {
                CurrentCrossStyle = CrossStyle.None;
                CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;
            }

            CameraController.FollowObject = ((Component)unit).transform;
            CameraController.JustFollow = false;
            ControllingObject = unit;

            UI.gameObject.SetActive(true);
            BarHealth = UI.rootVisualElement.Q<ProgressBar>("bar-health");
            UI.rootVisualElement.Q<VisualElement>(null, "unity-progress-bar__progress").style.backgroundColor = new StyleColor(Color.green);
            UI.rootVisualElement.Q<VisualElement>("container-ammo").Clear();

            CursorManager.Instance.ForceUpdate();

            UpdateUI();
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
                if ((Object)ControllingObject != null)
                {
                    ulong unitID = ControllingObject.GetNetworkIDForce();
                    Debug.Log($"[{nameof(TakeControlManager)}]: Send lose control request to server (trying to lose control over object \"{ControllingObject.GetGameObject()}\" (networkID: {unitID}))", this);
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

            CurrentCrossStyle = CrossStyle.None;
            CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;

            UI.gameObject.SetActive(false);

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            if ((Object)ControllingObject == null) return;

            if (((Component)ControllingObject).TryGetComponent(out HoveringLabel label))
            {
                label.Hide();
            }

            if (ControllingObject is ICanTakeControlAndHasTurret hasTurret &&
                hasTurret.Turret != null)
            { hasTurret.Turret.SetTarget(Vector3.zero); }

            CameraController.FollowObject = null;
            ControllingObject = null;

            CursorManager.Instance.ForceUpdate();
        }

        void LoseControlClient()
        {
            UI.gameObject.SetActive(false);

            EnableMouseCooldown = 1f;
            FindObjectOfType<SelectionManager>().ClearSelection();

            CurrentCrossStyle = CrossStyle.None;
            CurrentReloadIndicatorStyle = ReloadIndicatorStyle.None;

            if ((Object)ControllingObject == null) return;

            CameraController.FollowObject = null;
            ControllingObject = null;

            CursorManager.Instance.ForceUpdate();
        }

        internal void ControllableDestroyed()
        {
            if (!gameObject.scene.isLoaded) return;
            LoseControl();
        }

        public bool HandleCursorLock(out CursorLockMode locked)
        {
            locked = CursorLockMode.None;

            if (!InputCondition()) return false;
            if (IsControlling)
            {
                if (CameraController.cameraMode == CameraMode.Normal)
                { locked = CursorLockMode.Locked; }
                else
                { locked = CursorLockMode.None; }

                if (IsScoping)
                { locked = CursorLockMode.Locked; }

                return true;
            }
            return false;
        }
        public bool HandleCursor()
        {
            if (!InputCondition()) return false;
            if (IsControlling)
            {
                if (CurrentCrossStyle == CrossStyle.None)
                {
                    return false;
                }
                else
                {
                    UnityEngine.Cursor.visible = false;
                    return true;
                }
            }
            if (!Input.GetKey(KeyCode.LeftControl)) return false;

            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition, CursorHitLayer);

            ICanTakeControl controllable = GetControllableAt(worldPosition);

            if ((Object)controllable == null) return false;

            CursorConfig.Set();
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

            if (!NetcodeUtils.FindGameObject(objectID, out GameObject unitObject))
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Network object {objectID} not found", this);
                return;
            }

            if (!unitObject.TryGetComponent(out ICanTakeControl unit))
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Object {unitObject} does not have an {nameof(ICanTakeControl)} component", this);
                return;
            }

            if (AnybodyControlling(unit, out ulong controlledBy) &&
                controlledBy != serverRpcParams.Receive.SenderClientId)
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Object {unit} already controlled by client {controlledBy}", this);
                return;
            }

            Debug.Log($"[{nameof(TakeControlManager)}]: Object {objectID} is now controlled by {(int)serverRpcParams.Receive.SenderClientId}");

            {
                if (unitObject.TryGetComponent(out HoveringLabel label) &&
                AuthManager.RemoteAccountProvider is IRemoteAccountProviderWithCustomID<ulong> remoteAccounts)
                { label.Show(remoteAccounts.Get(serverRpcParams.Receive.SenderClientId)?.DisplayName); }
            }

            ControllingObjects.Set((int)serverRpcParams.Receive.SenderClientId, objectID, ulong.MaxValue);
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
            { return (Object)ControllingObject == (Object)@this; }

            if ((int)NetworkManager.LocalClientId >= ControllingObjects.Count ||
                (int)NetworkManager.LocalClientId < 0)
            { return false; }

            if (!@this.TryGetComponentInChildren(out NetworkObject netUnit))
            {
                Debug.LogWarning($"[{nameof(TakeControlManager)}]: Object \"{@this}\" does not have a {nameof(NetworkObject)} component", (Object)@this);
                return false;
            }

            return netUnit.NetworkObjectId == ControllingObjects[(int)NetworkManager.LocalClientId];
        }

        internal bool AnybodyControlling(ICanTakeControl @this)
            => AnybodyControlling(@this, out _);
        internal bool AnybodyControlling(ICanTakeControl @this, out ulong clientID)
        {
            clientID = default;

            try
            {
                if (NetcodeUtils.IsOffline)
                { return (Object)ControllingObject == (Object)@this; }
            }
            catch (System.NullReferenceException)
            { return false; }

            ulong objectId = @this.GetNetworkIDForce();

            for (int i = 0; i < ControllingObjects.Count; i++)
            {
                if (ControllingObjects[i] == objectId)
                {
                    clientID = (ulong)i;
                    return true;
                }
            }
            return false;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            if (!IsControlling)
            { return; }

            Vector2 predictedHitPosition = Vector2.zero;
            Vector2 mousePosition = Vector2.zero;

            float reloadPercent = 1f;
            bool isAccurate = true;

            bool hasTargetRect = false;
            Rect targetRect = Rect.zero;

            Vector2 targetPosition = Vector2.zero;
            Vector2 predictedTargetPosition = Vector2.zero;

            bool showReload = false;

            Transform targetObject = null;

            Vector3[] trajectoryPath = null;

            if (ControllingObject is ICanTakeControlAndHasTurret hasTurret &&
                hasTurret.Turret != null)
            {
                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                {
                    if (hasTurret.Turret.TryGetTrajectory(out Ballistics.Trajectory trajectory))
                    {
                        const int iterations = 5;
                        const float step = .2f;

                        trajectoryPath = new Vector3[iterations];

                        float t = 0f;
                        for (int i = 0; i < iterations; i++)
                        {
                            Vector3 point = trajectory.Position(t);
                            t += step;

                            Vector3 screenPoint = MainCamera.Camera.WorldToScreenPoint(point);
                            if (screenPoint.z <= 0f)
                            {
                                trajectoryPath = null;
                                break;
                            }

                            trajectoryPath[i] = GUIUtils.TransformPoint(screenPoint);
                        }
                    }
                }

                targetObject = hasTurret.Turret.TargetTransform;

                showReload = hasTurret.Turret.reloadTime >= .5f;

                Vector3 _targetPosition = hasTurret.Turret.TargetPosition;
                if (_targetPosition != Vector3.zero)
                {
                    targetPosition = GUIUtils.TransformPoint(MainCamera.Camera.WorldToScreenPoint(_targetPosition));

                    Vector3 _targetPredictedOffset = hasTurret.Turret.PredictedOffset;
                    if (_targetPredictedOffset != Vector3.zero)
                    {
                        predictedTargetPosition = GUIUtils.TransformPoint(MainCamera.Camera.WorldToScreenPoint(_targetPosition + _targetPredictedOffset));
                    }
                }

                Vector3 screenPos = MainCamera.Camera.WorldToScreenPoint(hasTurret.Turret.PredictImpact() ?? _targetPosition);

                if (screenPos.z > 0f)
                { predictedHitPosition = GUIUtils.TransformPoint(screenPos); }

                if (hasTurret.Turret.ReloadPercent < 1f)
                { ReloadIndicatorFadeoutAnimation.Reset(); }
                else
                { ReloadIndicatorFadeoutAnimation.Start(); }

                if (hasTurret.Turret.reloadTime > 0.01f)
                { reloadPercent = 1f - Maths.Clamp01(hasTurret.Turret.CurrentReload / hasTurret.Turret.reloadTime); }

                isAccurate = hasTurret.Turret.IsAccurateShoot;

                if (targetObject != null &&
                    targetObject.TryGetComponent(out Hitbox hitbox))
                {
                    Bounds bounds = hitbox.ColliderBounds;

                    if (bounds.size.magnitude > 50f)
                    {
                        hasTargetRect = false;
                    }
                    else
                    {
                        hasTargetRect = UnityUtils.GetScreenCorners(MainCamera.Camera, bounds, out var corners);

                        if (hasTargetRect)
                        {
                            targetRect = RectUtils.FromCorners(corners.TopLeft, corners.BottomRight);

                            targetRect = targetRect.Padding(8f);

                            targetRect.position = GUIUtils.TransformPoint(targetRect.position);
                            targetRect.position = new Vector2(targetRect.position.x, targetRect.position.y - targetRect.height);
                        }
                    }
                }
            }

            TargetLockingAnimationController.Refresh(!hasTargetRect);
            TargetLockingAnimation.Refresh();

            if (this.targetRect == Rect.zero)
            { this.targetRect = targetRect; }
            else
            {
                if (!hasTargetRect)
                {
                    if (!TargetLockingAnimation.IsStarted || TargetLockingAnimation.IsFinished)
                    {
                        this.targetRect = Rect.zero;
                    }
                }
                else
                {
                    this.targetRect.x = Maths.MoveTowards(this.targetRect.x, targetRect.x, 1f);
                    this.targetRect.y = Maths.MoveTowards(this.targetRect.y, targetRect.y, 1f);
                    this.targetRect.width = Maths.MoveTowards(this.targetRect.width, targetRect.width, 1f);
                    this.targetRect.height = Maths.MoveTowards(this.targetRect.height, targetRect.height, 1f);
                }
            }

            if (MouseManager.MouseOnWindow)
            { mousePosition = GUIUtils.TransformPoint(Input.mousePosition); }

            if (predictedHitPosition == Vector2.zero &&
                mousePosition == Vector2.zero &&
                targetPosition == Vector2.zero)
            { return; }

            Vector2 reloadIndicatorCenter = mousePosition;

            const float MeltDistance = 5f;

            GUISkin savedSkin = GUI.skin;
            GUI.skin = GUISkin;
            GL.PushMatrix();
            if (GLUtils.SolidMaterial.SetPass(0))
            {
                // if (trajectoryPath != null) DrawTrajectory(trajectoryPath);

                Vector2 _targetPosition = (predictedTargetPosition != Vector2.zero) ? predictedTargetPosition : targetPosition;

                if (_targetPosition != Vector2.zero && (_targetPosition - mousePosition).sqrMagnitude > MeltDistance)
                { DrawCross(_targetPosition, CrossSize.Inner, CrossSize.Outer, 1f, CrosshairColorPrediction); }

                if (mousePosition != Vector2.zero)
                {
                    float animation = TargetLockingAnimation.Percent;
                    if (hasTargetRect || animation > 0f)
                    {
                        DrawCrossOrRect(mousePosition, CrossSize.Inner, CrossSize.Outer, this.targetRect, animation, CrosshairColor);
                        reloadIndicatorCenter = Vector2.Lerp(mousePosition, this.targetRect.center, animation);

                        if (targetObject != null)
                        {
                            GUI.Label(new Rect(new Vector2(this.targetRect.xMax, this.targetRect.yMin - GUISkin.label.fontSize), new Vector2(100f, GUISkin.label.fontSize)), $"{Maths.Round(MetricUtils.GetMeters((ControllingObject.Object().transform.position - targetObject.position).sqrMagnitude))} m");
                        }
                    }
                    else
                    {
                        DrawCross(mousePosition, CrossSize.Inner, CrossSize.Outer, 1f, CrosshairColor);
                    }
                }

                if (predictedHitPosition != Vector2.zero && (predictedHitPosition - mousePosition).sqrMagnitude > MeltDistance)
                {
                    DrawCross(predictedHitPosition, CrossSize.Inner, CrossSize.Outer, 1f, isAccurate ? CrosshairColorAccurate : CrosshairColorInaccurate);
                }

                if (showReload)
                { DrawReloadIndicator(reloadPercent, reloadIndicatorCenter, ShadowColor); }
            }
            GL.PopMatrix();
            GUI.skin = savedSkin;
        }

        void DrawTrajectory(Vector3[] trajectoryPath)
        {
            GL.Begin(GL.LINE_STRIP);
            for (int i = 0; i < trajectoryPath.Length; i++)
            {
                GL.Color(new Color(1f, 1f, 1f, 1f - ((float)(i + 1) / (float)trajectoryPath.Length)));
                GL.Vertex(trajectoryPath[i]);
            }
            GL.End();
        }

        void DrawReloadIndicator(float value, Vector2 center, Color shadowColor)
        {
            if (CurrentReloadIndicatorStyle == ReloadIndicatorStyle.None) return;

            if (value != 1f)
            {
                if (CurrentReloadIndicatorStyle == ReloadIndicatorStyle.Circle)
                {
                    GLUtils.DrawCircle(center + Vector2.one, ReloadDotsRadius, 2f, shadowColor, value, 24);
                    GLUtils.DrawCircle(center, ReloadDotsRadius, 2f, Color.white, value, 24);
                }
                else if (CurrentReloadIndicatorStyle == ReloadIndicatorStyle.Dots)
                {
                    float step = 1f / (float)ReloadDots;

                    for (int i = 0; i < ReloadDots; i++)
                    {
                        float normalizedIndex = (float)i / (float)ReloadDots;

                        float rad = 2 * Maths.PI * normalizedIndex;
                        Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                        float multiplier = Maths.Clamp01((value - normalizedIndex) / step);

                        if (multiplier <= .01f)
                        { continue; }

                        float size = ReloadDotsSize * multiplier;

                        GUI.DrawTexture(RectUtils.Center(center + (direction * ReloadDotsRadius) + Vector2.one, Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, shadowColor, 0f, 0f);
                        GUI.DrawTexture(RectUtils.Center(center + (direction * ReloadDotsRadius), Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 0f);
                    }
                }
            }
            else
            {
                float fadeOutPercent = ReloadIndicatorFadeoutAnimation.PercentInverted;

                if (fadeOutPercent > .0001f)
                {
                    if (CurrentReloadIndicatorStyle == ReloadIndicatorStyle.Circle)
                    {
                        GLUtils.DrawCircle(center, ReloadDotsRadius + ((1f - fadeOutPercent) * 4f), 2f + ((1f - fadeOutPercent) * 4f), Color.white.Opacity(fadeOutPercent), value, 24);
                    }
                    else if (CurrentReloadIndicatorStyle == ReloadIndicatorStyle.Dots)
                    {
                        for (int i = 0; i < ReloadDots; i++)
                        {
                            float normalizedIndex = (float)i / (float)ReloadDots;

                            float rad = 2 * Maths.PI * normalizedIndex;
                            Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                            float size = ReloadDotsSize;

                            size += (1f - fadeOutPercent) * 4f;

                            Vector2 offset = Vector2.zero;

                            offset += direction * ReloadDotsRadius;
                            offset += direction * ((1f - fadeOutPercent) * 4f);

                            GUI.DrawTexture(RectUtils.Center(center + offset, Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, Color.white.Opacity(fadeOutPercent), 0f, 0f);
                        }
                    }
                }
            }
        }

        void DrawCross(Vector2 center, float innerSize, float outerSize, float thickness, Color color)
        {
            switch (CurrentCrossStyle)
            {
                case CrossStyle.Cross:
                    CrossDrawer.Draw(center, innerSize, outerSize, thickness, color, ShadowColor);
                    break;
                case CrossStyle.DiagonalCross:
                    DiagonalCrossDrawer.Draw(center, innerSize, outerSize, thickness, color, ShadowColor);
                    break;
                case CrossStyle.Cross3:
                    Cross3Drawer.Draw(center, innerSize, outerSize, thickness, color, ShadowColor);
                    break;
                case CrossStyle.None:
                    break;
                default:
                    break;
            }
        }

        void DrawCrossOrRect(Vector2 cross, float innerSize, float outerSize, Rect? rect, float animation, Color color)
        {
            switch (CurrentCrossStyle)
            {
                case CrossStyle.Cross:
                    CrossDrawer.DrawCrossOrRect(cross, innerSize, outerSize, rect, animation, color, ShadowColor);
                    break;
                case CrossStyle.DiagonalCross:
                    DiagonalCrossDrawer.DrawCrossOrRect(cross, innerSize, outerSize, rect, animation, color, ShadowColor);
                    break;
                case CrossStyle.Cross3:
                    Cross3Drawer.DrawCrossOrRect(cross, innerSize, outerSize, rect, animation, color, ShadowColor);
                    break;
                default:
                    break;
            }
        }
    }
}

namespace Game.Components
{
    public interface ICanTakeControl : IComponent
    {
        void DoInput();
        void DoFrequentInput();

        public void OnDestroy();
    }

    public interface ICanTakeControlAndHasTurret : ICanTakeControl
    {
        public Turret Turret { get; }

        public TakeControlManager.CrossStyle CrossStyle { get; }
        public TakeControlManager.ReloadIndicatorStyle ReloadIndicatorStyle { get; }
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

    public static string ControlledByName(this ICanTakeControl self)
    {
        if (TakeControlManager.Instance == null)
        { return "nobody"; }
        return TakeControlManager.Instance.AnybodyControlling(self, out ulong controllingBy) ? $"client {controllingBy}" : "nobody";
    }

    public static ulong ControlledBy(this ICanTakeControl self)
    {
        if (TakeControlManager.Instance == null)
        { return ulong.MaxValue; }
        return TakeControlManager.Instance.AnybodyControlling(self, out ulong controllingBy) ? controllingBy : ulong.MaxValue;
    }
}
