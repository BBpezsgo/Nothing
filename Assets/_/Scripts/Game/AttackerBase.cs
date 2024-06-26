using System;
using Game.Managers;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class AttackerBase : Unity.Netcode.NetworkBehaviour
    {
        [SerializeField, ReadOnly] protected BaseObject BaseObject;
        ICanTakeControlAndHasTurret CanTakeControlObject;
        bool CanTakeControl;

        internal int TeamHash => BaseObject.TeamHash;

        [Header("Target Locking")]
        [SerializeField] bool EnableTargetLocking = true;
        [Tooltip("Min distance in screen space to lock on the target")]
        [SerializeField] float TargetLockingThreshold = 10f;

        [Header("Turret")]
        [SerializeField] protected Turret turret;
        public Turret Turret
        {
            get => turret;
            set => turret = value;
        }

        [Header("Scope")]
        [SerializeField] float ScopeSensitivity = 1f;
        [SerializeField] bool HullRotationStabilizer = false;

        [SerializeField, ReadOnly] float lastRotation;
        [SerializeField, ReadOnly] float rotationDelta;

        protected virtual void Awake()
        {
            if (!TryGetComponent(out BaseObject))
            { Debug.LogError($"[{nameof(AttackerBase)}]: {nameof(BaseObject)} is null", this); }

            if (BaseObject is ICanTakeControlAndHasTurret canTakeControl)
            {
                CanTakeControlObject = canTakeControl;
                CanTakeControl = true;
            }

            if (turret != null) turret.@base = BaseObject;
        }

        protected virtual void Update()
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
            bool anyTouchCaptured = false;
            for (int i = Input.touchCount - 1; i >= 0; i--)
            {
                Touch touch = Input.touches[i];
                if (MouseManager.IsTouchCaptured(touch.fingerId))
                { anyTouchCaptured = true; }
                else
                { anyTouchCaptured = false; }
            }

            if (anyTouchCaptured) return;

            if (TakeControlManager.Instance.IsScoping && turret.ScopeHolder != null)
            {
                Vector3 lockFix = default;

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

                            if ((screenCenter - (Vector2)screenPosition).sqrMagnitude > TargetLockingThreshold * TargetLockingThreshold)
                            { continue; }

                            turret.SetTarget(unit.transform);

                            targetLocked = true;
                            break;
                        }
                    }

                    if (!targetLocked)
                    {
                        Ray ray = MainCamera.Camera.ScreenPointToRay(screenCenter);
                        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).ExcludeTransforms(transform);
                        Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits[hits.Closest(transform.position).Index].point;

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

                                if (unit == BaseObject)
                                { continue; }

                                if (TeamManager.Instance.GetFuckYou(TeamHash, unit.TeamHash) < 0f)
                                { continue; }

                                Vector3 screenPosition = MainCamera.Camera.WorldToScreenPoint(unit.transform.position);

                                if (screenPosition.z <= 0f)
                                { continue; }

                                if ((mousePosition - (Vector2)screenPosition).sqrMagnitude > TargetLockingThreshold * TargetLockingThreshold)
                                { continue; }

                                turret.SetTarget(unit.transform);

                                targetLocked = true;
                                break;
                            }
                        }

                        if (!targetLocked)
                        {
                            Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
                            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).ExcludeTransforms(transform);
                            Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits[hits.Closest(transform.position).Index].point;

                            turret.SetTarget(point);
                        }
                    }
                    else if (NetcodeUtils.IsClient)
                    {
                        Ray ray = MainCamera.Camera.ScreenPointToRay(mousePosition);
                        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).ExcludeTransforms(transform);
                        Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits[hits.Closest(transform.position).Index].point;

                        if ((turret.TargetPosition - point).sqrMagnitude > .5f)
                        {
                            turret.TargetRequest(point);
                        }
                    }
                }
            }

            if (Mouse.IsHold(Mouse.Left) && !MenuManager.AnyMenuVisible)
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
                float scopeSensitivity = ScopeSensitivity * Math.Clamp((40f - CameraController.Instance.ScopeZoom) / 40f, .5f, 1f);

                Vector3 newRotation = turret.ScopeHolder.localRotation.eulerAngles + new Vector3(Mouse.DeltaY * (-scopeSensitivity), Mouse.DeltaX * scopeSensitivity, 0f);

                newRotation.x = Maths.General.NormalizeAngle(newRotation.x);

                float minAngle = -Math.Abs(turret.cannonHighestAngle);
                float maxAngle = Math.Abs(turret.cannonLowestAngle);

                newRotation.x = Math.Clamp(newRotation.x, minAngle, maxAngle);

                turret.ScopeHolder.localRotation = Quaternion.Euler(newRotation);
            }
        }
    }
}
