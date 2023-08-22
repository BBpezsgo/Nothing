using AssetManager;

using Game.Managers;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    public class AttackerBase : NetworkBehaviour
    {
        [SerializeField, ReadOnly] protected Rigidbody rb;
        [SerializeField, ReadOnly] protected BaseObject BaseObject;
        ICanTakeControlAndHasTurret CanTakeControlObject;
        bool CanTakeControl;

        internal string Team => BaseObject.Team;
        internal int TeamHash => BaseObject.TeamHash;

        [Header("Target Locking")]
        [SerializeField] bool EnableTargetLocking = true;
        [Tooltip("Min distance in screen space to lock on the target")]
        [SerializeField] float TargetLockingThreshold = 10f;

        [Header("Turret")]
        [SerializeField, AssetField] internal Turret turret;
        public Turret Turret => turret;

        [Header("Scope")]
        [SerializeField, AssetField] float ScopeSensitivity = 1f;
        [SerializeField, AssetField] bool HullRotationStabilizer = false;

        [SerializeField, ReadOnly] float lastRotation;
        [SerializeField, ReadOnly] float rotationDelta;

        protected virtual void Start()
        {
            if (!TryGetComponent(out BaseObject))
            { Debug.LogError($"[{nameof(AttackerBase)}]: {nameof(BaseObject)} is null", this); }

            rb = GetComponent<Rigidbody>();
            // if (!TryGetComponent(out rb))
            // { Debug.LogError($"[{nameof(AttackerBase)}]: {nameof(rb)} is null", this); }

            if (BaseObject is ICanTakeControlAndHasTurret canTakeControl)
            {
                CanTakeControlObject = canTakeControl;
                CanTakeControl = true;
            }

            if (turret != null) turret.@base = BaseObject;
        }

        protected virtual void FixedUpdate()
        {
            if (HullRotationStabilizer &&
                CanTakeControl &&
                CanTakeControlObject.IAmControllingThis())
            {
                float rotation = transform.rotation.eulerAngles.y + 360f;
                rotationDelta = (lastRotation - rotation) % 360f;
                lastRotation = rotation;
            }
        }

        public void DoInput()
        {
            if (TakeControlManager.Instance.IsScoping && turret.ScopeHolder != null)
            {
                Vector3 lockFix = Vector3.zero;

                if (HullRotationStabilizer)
                { lockFix.y = rotationDelta; }

                turret.ScopeHolder.localRotation = Quaternion.Euler(turret.ScopeHolder.localRotation.eulerAngles + lockFix);

                if (!MenuManager.AnyMenuVisible && !Input.GetKey(KeyCode.LeftAlt))
                {
                    Vector2 screenCenter = new Vector2(Screen.width, Screen.height) / 2;
                    bool targetLocked = false;

                    if (EnableTargetLocking)
                    {
                        for (int i = RegisteredObjects.Units.Count - 1; i >= 0; i--)
                        {
                            Unit unit = RegisteredObjects.Units[i];

                            if (unit == null)
                            { continue; }

                            if (TeamManager.Instance.GetFuckYou(TeamHash, unit.TeamHash) < 0f)
                            { continue; }

                            Vector3 screenPosition = MainCamera.Camera.WorldToScreenPoint(unit.transform.position);

                            if (screenPosition.z <= 0f)
                            { continue; }

                            if ((screenCenter - screenPosition.To2()).sqrMagnitude > TargetLockingThreshold * TargetLockingThreshold)
                            { continue; }

                            turret.SetTarget(unit.transform);

                            targetLocked = true;
                            break;
                        }
                    }

                    if (!targetLocked)
                    {
                        var ray = MainCamera.Camera.ScreenPointToRay(screenCenter);
                        var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).Exclude(transform);
                        Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

                        if (NetcodeUtils.IsOfflineOrServer)
                        {
                            turret.SetTarget(point);
                        }
                        else if (NetcodeUtils.IsClient)
                        {
                            if ((turret.TargetPosition - point).sqrMagnitude > .5f)
                            {
                                turret.TargetRequest(point);
                            }
                        }
                    }
                }
            }
            else
            {
                if (turret.ScopeHolder != null) turret.ScopeHolder.rotation = Quaternion.Euler(turret.cannon.transform.rotation.eulerAngles.x, turret.transform.rotation.eulerAngles.y, 0f);

                if (!MenuManager.AnyMenuVisible && !Input.GetKey(KeyCode.LeftAlt))
                {
                    Vector2 mousePosition = Input.mousePosition;

                    if (NetcodeUtils.IsOfflineOrServer)
                    {
                        bool targetLocked = false;

                        if (EnableTargetLocking)
                        {
                            for (int i = RegisteredObjects.Units.Count - 1; i >= 0; i--)
                            {
                                Unit unit = RegisteredObjects.Units[i];

                                if (unit == null)
                                { continue; }

                                if (TeamManager.Instance.GetFuckYou(TeamHash, unit.TeamHash) < 0f)
                                { continue; }

                                Vector3 screenPosition = MainCamera.Camera.WorldToScreenPoint(unit.transform.position);

                                if (screenPosition.z <= 0f)
                                { continue; }

                                if ((mousePosition - screenPosition.To2()).sqrMagnitude > TargetLockingThreshold * TargetLockingThreshold)
                                { continue; }

                                turret.SetTarget(unit.transform);

                                targetLocked = true;
                                break;
                            }
                        }

                        if (!targetLocked)
                        {
                            var ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
                            var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).Exclude(transform);
                            Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

                            turret.SetTarget(point);
                        }
                    }
                    else if (NetcodeUtils.IsClient)
                    {
                        var ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
                        var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).Exclude(transform);
                        Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

                        if ((turret.TargetPosition - point).sqrMagnitude > .5f)
                        {
                            turret.TargetRequest(point);
                        }
                    }
                }
            }

            if (Input.GetMouseButton(MouseButton.Left) && !MenuManager.AnyMenuVisible)
            {
                turret.PrepareShooting = true;
                if (turret.IsAccurateShoot)
                {
                    if (NetcodeUtils.IsOfflineOrServer)
                    { turret.Shoot(); }
                    else if (NetcodeUtils.IsClient)
                    { turret.ShootRequest(); }
                }
            }
            else
            {
                turret.PrepareShooting = false;
            }
        }

        public void DoFrequentInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && turret.projectile == null && turret.Projectiles.Length > 0)
            {
                turret.SelectedProjectile++;
                if (turret.SelectedProjectile < 0)
                { turret.SelectedProjectile = turret.Projectiles.Length - 1; }
                else if (turret.SelectedProjectile >= turret.Projectiles.Length)
                { turret.SelectedProjectile = 0; }

                TakeControlManager.Instance.UpdateSelectedProjectile(turret.SelectedProjectile);
            }

            if (TakeControlManager.Instance.IsScoping && turret.ScopeHolder != null && !MenuManager.AnyMenuVisible)
            {
                Vector2 mouseDelta = CameraController.MouseDelta;

                float scopeSensitivity = ScopeSensitivity * Mathf.Clamp((40f - CameraController.Instance.ScopeZoom) / 40f, .5f, 1f);

                Vector3 newRotation = turret.ScopeHolder.localRotation.eulerAngles + new Vector3(mouseDelta.y * (-scopeSensitivity), mouseDelta.x * scopeSensitivity, 0f);

                newRotation.x = Utilities.Utils.NormalizeAngle(newRotation.x);

                float minAngle = -Mathf.Abs(turret.cannonHighestAngle);
                float maxAngle = Mathf.Abs(turret.cannonLowestAngle);

                newRotation.x = Mathf.Clamp(newRotation.x, minAngle, maxAngle);

                turret.ScopeHolder.localRotation = Quaternion.Euler(newRotation);
            }
        }
    }
}
