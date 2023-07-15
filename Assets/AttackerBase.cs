using AssetManager;

using Unity.Netcode;

using UnityEngine;

using Utilities;

public class AttackerBase : NetworkBehaviour
{
    [SerializeField, ReadOnly] protected Rigidbody rb;
    [SerializeField, ReadOnly] protected BaseObject BaseObject;

    internal string Team => BaseObject.Team;
    internal int TeamHash => BaseObject.TeamHash;

    [Header("Turret")]
    [SerializeField, AssetField] internal Turret turret;
    public Turret Turret => turret;

    [Header("Scope")]
    [SerializeField, AssetField] float ScopeSensitivity = 1f;
    [SerializeField, AssetField] bool HullRotationStabilizer = false;

    [SerializeField, ReadOnly] float lastRotation;
    [SerializeField, ReadOnly] float rotationDelta;

    protected virtual void Awake()
    {
        if (!TryGetComponent(out BaseObject))
        { Debug.LogError($"[{nameof(AttackerBase)}]: No BaseObject!"); }

        if (turret != null) turret.@base = BaseObject;
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void FixedUpdate()
    {
        if (HullRotationStabilizer)
        {
            var rotation = transform.rotation.eulerAngles.y + 360f;
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

                var ray = MainCamera.Camera.ScreenPointToRay(screenCenter);
                var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Targeting).Exclude(transform);
                Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

                if (NetcodeUtils.IsOfflineOrServer)
                {
                    turret.target.Value = point;
                }
                else if (NetcodeUtils.IsClient)
                {
                    if ((turret.target.Value - point).sqrMagnitude > .5f)
                    {
                        turret.TargetRequest(point);
                    }
                }
            }
        }
        else
        {
            if (turret.ScopeHolder != null) turret.ScopeHolder.rotation = Quaternion.Euler(turret.cannon.transform.rotation.eulerAngles.x, turret.transform.rotation.eulerAngles.y, 0f);

            if (!MenuManager.AnyMenuVisible && !Input.GetKey(KeyCode.LeftAlt))
            {
                var ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
                var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Targeting).Exclude(transform);
                Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

                if (NetcodeUtils.IsOfflineOrServer)
                {
                    turret.target.Value = point;
                }
                else if (NetcodeUtils.IsClient)
                {
                    if ((turret.target.Value - point).sqrMagnitude > .5f)
                    {
                        turret.TargetRequest(point);
                    }
                }
            }
        }

        if (Input.GetMouseButton(Utilities.MouseButton.Left) && !MenuManager.AnyMenuVisible)
        {
            if (turret.IsAccurateShoot)
            {
                if (NetcodeUtils.IsOfflineOrServer)
                {
                    turret.Shoot();
                }
                else if (NetcodeUtils.IsClient)
                {
                    turret.ShootRequest();
                }
            }
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
