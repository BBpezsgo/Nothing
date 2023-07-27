using Authentication;

using Game.Components;
using Game.Managers;
using Game.UI;
using Game.UI.Components;

using System.Linq;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

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

        Texture2D SphereFilled;
        float reloadedAt = 0f;
        float targetedAt = 0f;
        bool lastTargeted = false;

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

        void ControllingObjectsChanged(NetworkListEvent<ulong> changeEvent)
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
            /*
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
            */
        }

        void HideCursor(Transform cursor)
        {
            /*
            cursor.position = new Vector3(0f, -50f, 0f);
            cursor.localScale = Vector3.one * .5f;
            */
        }

        void SetWindowCursor()
        {
            if (CameraController.cameraMode == CameraMode.Normal)
            { UnityEngine.Cursor.lockState = CursorLockMode.Locked; }
            else
            { UnityEngine.Cursor.lockState = CursorLockMode.None; }
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

        void OnGUI()
        {
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

            if (ControllingObject is ICanTakeControlAndHasTurret hasTurret &&
                hasTurret.Turret != null)
            {
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
                { reloadedAt = 0f; }
                else if (reloadedAt == 0f)
                { reloadedAt = Time.unscaledTime; }

                if (hasTurret.Turret.reloadTime > 0.01f)
                { reloadPercent = 1f - Mathf.Clamp01(hasTurret.Turret.CurrentReload / hasTurret.Turret.reloadTime); }

                isAccurate = hasTurret.Turret.IsAccurateShoot;

                if (hasTurret.Turret.TargetTransform != null &&
                    hasTurret.Turret.TargetTransform.TryGetComponent(out Hitbox hitbox))
                {
                    Bounds bounds = hitbox.Bounds;

                    if (bounds.size.magnitude > 50f)
                    {
                        hasTargetRect = false;
                    }
                    else
                    {
                        hasTargetRect = Utils.GetScreenCorners(bounds, out var corners);

                        if (hasTargetRect)
                        {
                            targetRect = RectFromCorners(corners.TopLeft, corners.BottomRight);

                            targetRect.Padding(8f);

                            targetRect.position = GUIUtils.TransformPoint(targetRect.position);
                            targetRect.position = new Vector2(targetRect.position.x, targetRect.position.y - targetRect.height);
                        }
                    }
                }
            }

            if (MouseManager.MouseOnWindow)
            { mousePosition = GUIUtils.TransformPoint(Input.mousePosition); }

            if (predictedHitPosition == Vector2.zero &&
                mousePosition == Vector2.zero &&
                targetPosition == Vector2.zero)
            { return; }

            if (lastTargeted)
            {
                if (!hasTargetRect)
                {
                    lastTargeted = false;
                    targetedAt = Time.time;
                }
            }
            else
            {
                if (hasTargetRect)
                {
                    lastTargeted = true;
                    targetedAt = Time.time;
                }
            }

            Color shadowColor = new(0f, 0f, 0f, .8f);

            GL.PushMatrix();
            if (GLUtils.SolidMaterial.SetPass(0))
            {
                float innerSize = 4f;
                float outerSize = 12f;

                if (targetPosition != Vector2.zero)
                {
                    if (predictedTargetPosition != Vector2.zero)
                    {
                        DrawCross(predictedTargetPosition + Vector2.one, innerSize, outerSize, 1f, shadowColor);
                        DrawCross(predictedTargetPosition, innerSize, outerSize, 1f, CrosshairColorPrediction);
                    }
                    else
                    {
                        DrawCross(targetPosition + Vector2.one, innerSize, outerSize, 1f, shadowColor);
                        DrawCross(targetPosition, innerSize, outerSize, 1f, CrosshairColorPrediction);
                    }
                }

                if (mousePosition != Vector2.zero)
                {
                    float animation = Mathf.Clamp01((Time.time - targetedAt) * TargetLockAnimationSpeed);
                    if (!lastTargeted)
                    { animation = 1f - animation; }

                    if (hasTargetRect && animation != 0f)
                    {
                        if (animation != 1f)
                        {
                            DrawCornerBoxFromCross(targetRect.center, targetRect.size, 8f, mousePosition, innerSize, outerSize, animation, CrosshairColor);
                        }
                        else
                        {
                            DrawCornerBox(targetRect.center + Vector2.one, targetRect.size, 8f, shadowColor);
                            DrawCornerBox(targetRect.center, targetRect.size, 8f, CrosshairColor);
                        }
                    }
                    else
                    {
                        DrawCross(mousePosition + Vector2.one, innerSize, outerSize, 1f, shadowColor);
                        DrawCross(mousePosition, innerSize, outerSize, 1f, CrosshairColor);
                    }
                }

                if (predictedHitPosition != Vector2.zero)
                {
                    DrawCross(predictedHitPosition + Vector2.one, innerSize, outerSize, 1f, shadowColor);
                    DrawCross(predictedHitPosition, innerSize, outerSize, 1f, isAccurate ? CrosshairColorAccurate : CrosshairColorInaccurate);
                }
            }
            GL.PopMatrix();

            if (reloadPercent != 1f)
            {
                float step = 1f / (float)ReloadDots;

                for (int i = 0; i < ReloadDots; i++)
                {
                    float normalizedIndex = (float)i / (float)ReloadDots;

                    float rad = 2 * Mathf.PI * normalizedIndex;
                    Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                    float multiplier = Mathf.Clamp01((reloadPercent - normalizedIndex) / step);

                    if (multiplier <= .01f)
                    { continue; }

                    float size = ReloadDotsSize * multiplier;

                    GUI.DrawTexture(RectUtils.Center(mousePosition + (direction * ReloadDotsRadius) + Vector2.one, Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, shadowColor, 0f, 0f);
                    GUI.DrawTexture(RectUtils.Center(mousePosition + (direction * ReloadDotsRadius), Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 0f);
                }
            }
            else
            {
                float fadeOutPercent = 1f - Mathf.Clamp01((Time.unscaledTime - reloadedAt) * ReloadDotsFadeoutSpeed);

                if (fadeOutPercent > .0001f)
                {
                    for (int i = 0; i < ReloadDots; i++)
                    {
                        float normalizedIndex = (float)i / (float)ReloadDots;

                        float rad = 2 * Mathf.PI * normalizedIndex;
                        Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                        float size = ReloadDotsSize;

                        size += (1f - fadeOutPercent) * 4f;

                        Vector2 offset = Vector2.zero;

                        offset += direction * ReloadDotsRadius;
                        offset += direction * ((1f - fadeOutPercent) * 4f);

                        GUI.DrawTexture(RectUtils.Center(mousePosition + offset, Vector2.one * size), SphereFilled, ScaleMode.StretchToFill, true, 0f, Color.white.Opacity(fadeOutPercent), 0f, 0f);
                    }
                }
            }
        }

        void DrawCross(Vector2 center, float innerSize, float outerSize, float thickness, Color color)
        {
            Vector2 innerPointV = Vector2.up * innerSize;
            Vector2 outerPointV = Vector2.up * outerSize;
            Vector2 innerPointH = Vector2.left * innerSize;
            Vector2 outerPointH = Vector2.left * outerSize;

            GLUtils.DrawLine(center + innerPointV, center + outerPointV, thickness, color);
            GLUtils.DrawLine(center - innerPointV, center - outerPointV, thickness, color);
            GLUtils.DrawLine(center + innerPointH, center + outerPointH, thickness, color);
            GLUtils.DrawLine(center - innerPointH, center - outerPointH, thickness, color);
        }

        void DrawCornerBox(Vector2 center, Vector2 size, float cornerSize, Color color)
        {
            Vector2 halfSize = size / 2;

            float cornerSizeWidth = Mathf.Min(halfSize.x, cornerSize);
            float cornerSizeHeight = Mathf.Min(halfSize.y, cornerSize);

            Vector2 topleft = new(-halfSize.x, -halfSize.y);
            Vector2 topright = new(halfSize.x, -halfSize.y);
            Vector2 bottomleft = new(-halfSize.x, halfSize.y);
            Vector2 bottomright = new(halfSize.x, halfSize.y);

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(topleft.x + cornerSizeWidth, topleft.y));
                GL.Vertex(center + topleft);
                GL.Vertex(center + new Vector2(topleft.x, topleft.y + cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(topright.x - cornerSizeWidth, topright.y));
                GL.Vertex(center + topright);
                GL.Vertex(center + new Vector2(topright.x, topright.y + cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(bottomleft.x + cornerSizeWidth, bottomleft.y));
                GL.Vertex(center + bottomleft);
                GL.Vertex(center + new Vector2(bottomleft.x, bottomleft.y - cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(bottomright.x - cornerSizeWidth, bottomright.y));
                GL.Vertex(center + bottomright);
                GL.Vertex(center + new Vector2(bottomright.x, bottomright.y - cornerSizeHeight));
                GL.End();
            }
        }

        void DrawCornerBoxFromCross(Vector2 boxCenter, Vector2 boxSize, float boxCornerSize, Vector2 crossCenter, float crossInnerSize, float crossOuterSize, float t, Color color)
        {
            Vector2 halfSize = boxSize / 2;

            float boxCornerSizeWidth = Mathf.Min(halfSize.x, boxCornerSize);
            float boxCornerSizeHeight = Mathf.Min(halfSize.y, boxCornerSize);

            Vector2 innerPointV = Vector2.up * crossInnerSize;
            Vector2 outerPointV = Vector2.up * crossOuterSize;
            Vector2 innerPointH = Vector2.left * crossInnerSize;
            Vector2 outerPointH = Vector2.left * crossOuterSize;

            (Vector2 Inner, Vector2 Outer) crossUp = (innerPointV, outerPointV);
            (Vector2 Inner, Vector2 Outer) crossDown = (-innerPointV, -outerPointV);
            (Vector2 Inner, Vector2 Outer) crossRight = (innerPointH, outerPointH);
            (Vector2 Inner, Vector2 Outer) crossLeft = (-innerPointH, -outerPointH);

            Vector2 center = Vector2.Lerp(crossCenter, boxCenter, t);

            {
                GL.Begin(GL.LINES);
                GL.Color(color);

                Vector2 crossLeftOuter = new(Mathf.Lerp(crossLeft.Outer.x, halfSize.x, t), crossLeft.Outer.y);
                Vector2 crossRightOuter = new(Mathf.Lerp(crossRight.Outer.x, -halfSize.x, t), crossRight.Outer.y);

                GL.Vertex(center + Vector2.Lerp(crossLeft.Inner, crossLeftOuter, t));
                GL.Vertex(center + crossLeftOuter);

                GL.Vertex(center + Vector2.Lerp(crossRight.Inner, crossRightOuter, t));
                GL.Vertex(center + crossRightOuter);

                GL.End();
            }

            Vector2 topleft = new(-halfSize.x, -halfSize.y);
            Vector2 topright = new(halfSize.x, -halfSize.y);
            Vector2 bottomleft = new(-halfSize.x, halfSize.y);
            Vector2 bottomright = new(halfSize.x, halfSize.y);

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topleft.x + boxCornerSizeWidth, topleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topleft, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topleft.x, topleft.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topright.x - boxCornerSizeWidth, topright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topright, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topright.x, topright.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomleft.x + boxCornerSizeWidth, bottomleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomleft, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomleft.x, bottomleft.y - boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomright.x - boxCornerSizeWidth, bottomright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomright, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomright.x, bottomright.y - boxCornerSizeHeight), t));
                GL.End();
            }
        }

        static Rect RectFromCorners(Vector2 topLeft, Vector2 bottomRight)
            => new(topLeft, bottomRight - topLeft);

        static bool GetScreenCorners(Vector3[] points, out (Vector2 TopLeft, Vector2 BottomRight) corners)
        {
            Vector2 topLeft = points[0];
            Vector2 bottomRight = points[0];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.z < 0)
                {
                    corners = (Vector2.zero, Vector2.zero);
                    return false;
                }

                if (p.x < topLeft.x)
                { topLeft.x = p.x; }

                if (p.y < topLeft.y)
                { topLeft.y = p.y; }

                if (p.x > bottomRight.x)
                { bottomRight.x = p.x; }

                if (p.y > bottomRight.y)
                { bottomRight.y = p.y; }
            }

            corners = (topLeft, bottomRight);
            return true;
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
    public static Component GetObject(this IAmObject self) => (Component)self;
}
