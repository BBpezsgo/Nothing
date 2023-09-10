using Game.Components;
using InputUtils;
using UnityEngine;

namespace Game.Managers
{
    internal enum CameraMode
    {
        Normal,
        TopDown,
        ZoomBased,
    }

    internal delegate void OnCameraModeChanged(CameraMode mode);

    public class CameraController : MonoBehaviour
    {
        internal static CameraController Instance;

        [SerializeField] Camera theCamera;
        [SerializeField] Transform cameraRotation;
        [SerializeField] internal CameraMode cameraMode;
        CameraMode lastCameraMode;

        [SerializeField] float Height = 1f;

        [Header("Following")]
        [SerializeField] internal Transform FollowObject;
        internal bool IsFollowing => FollowObject != null;
        [SerializeField, Tooltip("Use the default movement behaviour when following an object")] internal bool JustFollow;
        [SerializeField, ReadOnly] float ActualFollowSpeed = 1f;
        [SerializeField, MinMax(0f, 10f)] Vector2 FollowHeightDisplacement = Vector2.one;

        [Header("Movement")]
        [SerializeField] float acceleration = 1f;
        [SerializeField] float maxSpeed = 1f;
        [SerializeField] float SpeedBonusOnShift = 1f;

        [SerializeField, ReadOnly] Vector2 MovementInput;
        [SerializeField, ReadOnly] Vector2 Velocity;
        [SerializeField, ReadOnly] Vector2 TargetVelocity;

        [Header("Rotation")]
        [SerializeField] float rotationSpeed = 1f;

        [SerializeField, ReadOnly] float TargetRotation;
        [SerializeField, ReadOnly] float Rotation;

        [Header("Angle")]
        [SerializeField] float AngleSpeed = 2f;
        [SerializeField] float inputAngleMultiplier = 1f;
        [SerializeField, ReadOnly] float TargetAngle;

        [Header("Zoom")]
        [SerializeField] float zoomInputMultiplier = 1f;
        [SerializeField] float ZoomBonusOnShift = 1f;
        [SerializeField] float zoomSpeed = 1f;
        [SerializeField, ReadOnly] float targetZoomSpeed;
        [SerializeField, ReadOnly] float currentZoomSpeed;

        [SerializeField, ReadOnly] float TargetZoom;
        [SerializeField, ReadOnly] float Zoom;

        [Header("Scoping")]
        [SerializeField] internal CameraLockable LockOn;
        internal bool IsLocked => LockOn != null;

        internal bool TryOverrideLock(CameraLockable @lock, int lockPriority)
        {
            if (LockOn != null && LockOn.Priority > lockPriority) return false;

            LockOn = @lock;
            return true;
        }

        /// <summary>
        /// From 0f to 40f
        /// </summary>
        internal float ScopeZoom => 60f - theCamera.fieldOfView;
        float LockedValue => (theCamera.transform.position - LockOn.position).sqrMagnitude;
        [SerializeField, ReadOnly] bool IsTotallyLocked = false;

        // [Header("Other")]
        internal float ZoomValue => Mathf.Max(0f, -theCamera.transform.localPosition.z);
        internal Vector3 CameraPosition => theCamera.transform.position;
        internal event OnCameraModeChanged OnCameraModeChanged;

        internal static Vector2 MouseDelta => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        TouchZoom TouchZoom;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(CameraController)}]: Instance already registered, destroying self");
                GameObject.Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            TargetZoom = -theCamera.transform.localPosition.z;
            Zoom = -theCamera.transform.localPosition.z;

            TargetAngle = Mathf.Clamp(cameraRotation.transform.localRotation.eulerAngles.x, 10f, 80f);

            TargetRotation = transform.rotation.eulerAngles.y;
            Rotation = transform.rotation.eulerAngles.y;

            lastCameraMode = cameraMode;

            TouchZoom = new TouchZoom(1, () => !MenuManager.AnyMenuVisible);
            TouchZoom.OnZoom += OnTouchZoom;
            TouchZoom.OnMove += OnTouchMove;

            targetZoomSpeed = zoomSpeed;
            currentZoomSpeed = zoomSpeed;
        }

        void OnTouchMove(AdvancedTouch sender)
        {
            Vector2 delta = sender.PositionDelta / AdvancedInput.ScreenSize;

            delta *= 50f;

            switch (cameraMode)
            {
                case CameraMode.Normal:
                    {
                        TargetRotation += delta.x * inputAngleMultiplier;
                        TargetAngle = Mathf.Clamp(TargetAngle - (delta.y * inputAngleMultiplier), (IsFollowing && !JustFollow) ? -80 : 10f, 80f);
                        break;
                    }
                case CameraMode.TopDown:
                    {
                        TargetRotation += delta.x * inputAngleMultiplier;
                        TargetAngle = 80f;
                        break;
                    }
                case CameraMode.ZoomBased:
                    {
                        TargetRotation += delta.x * inputAngleMultiplier;
                        TargetAngle = Mathf.Clamp(Zoom * .5f, 10f, 80f);
                        break;
                    }
            }
        }

        void OnTouchZoom(TouchZoom sender, float delta)
        {
            float zoomInput = delta * 30f;
            zoomInput *= Mathf.Max(Mathf.Log(ZoomValue), 1f);

            TargetZoom = Mathf.Max(TargetZoom + zoomInput, 0f);

            Zoom = Mathf.Max(TargetZoom, 0f);
        }

        void Update()
        {
            currentZoomSpeed = Mathf.MoveTowards(currentZoomSpeed, targetZoomSpeed, 5 * Time.unscaledDeltaTime);

            if (IsLocked)
            {
                HandleAndDoLocking(Time.unscaledDeltaTime);

                // targetZoomSpeed = 0;
                // currentZoomSpeed = 0;

                Zoom = -theCamera.transform.localPosition.z;
                Rotation = transform.rotation.eulerAngles.y;

                return;
            }
            else
            {
                targetZoomSpeed = zoomSpeed;
            }

            theCamera.fieldOfView = 60f;

            if (!IsFollowing || JustFollow)
            { HandleMovementInput(); }
            else
            {
                MovementInput = Vector2.zero;
                TargetVelocity = Vector2.zero;
            }

            HandleRotationInput();

            if (IsFollowing)
            { DoFollowing(Time.unscaledDeltaTime); }
            else
            { DoMovement(Time.unscaledDeltaTime); }

            DoRotation(Time.unscaledDeltaTime);

            DoZooming(Time.unscaledDeltaTime);

            DoAngle(Time.unscaledDeltaTime);

            if (!MenuManager.AnyMenuVisible &&
                MouseManager.MouseOnWindow &&
                !MouseManager.IsPointerOverUI())
            {
                float zoomInput = -Input.mouseScrollDelta.y * zoomInputMultiplier;

                if (Input.GetKey(KeyCode.LeftShift))
                { zoomInput *= ZoomBonusOnShift; }

                zoomInput *= Mathf.Max(Mathf.Log(ZoomValue), 1f);

                TargetZoom = Mathf.Max(TargetZoom + zoomInput, 0f);
            }
        }

        void HandleMovementInput()
        {
            if (MenuManager.AnyMenuVisible)
            {
                MovementInput = Vector2.zero;
                TargetVelocity = Vector2.zero;
                return;
            }

            if (TouchJoystick.Instance != null && TouchJoystick.Instance.IsActiveAndCaptured)
            {
                MovementInput = TouchJoystick.Instance.NormalizedInput;
            }
            else
            {
                MovementInput.x = Input.GetAxisRaw("Vertical");
                MovementInput.y = Input.GetAxisRaw("Horizontal");
            }

            TargetVelocity = MovementInput * maxSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            { TargetVelocity *= SpeedBonusOnShift; }
        }

        void HandleRotationInput()
        {
            switch (cameraMode)
            {
                case CameraMode.Normal:
                    {
                        if (!MenuManager.AnyMenuVisible && ((IsFollowing && !JustFollow) || Input.GetMouseButton(MouseButton.Middle)))
                        {
                            TargetRotation += MouseDelta.x * inputAngleMultiplier;
                            TargetAngle = Mathf.Clamp(TargetAngle - (MouseDelta.y * inputAngleMultiplier), (IsFollowing && !JustFollow) ? -80 : 10f, 80f);
                        }
                        break;
                    }
                case CameraMode.TopDown:
                    {
                        if (!MenuManager.AnyMenuVisible && Input.GetMouseButton(MouseButton.Middle))
                        { TargetRotation += MouseDelta.x * inputAngleMultiplier; }
                        TargetAngle = 80f;
                        break;
                    }
                case CameraMode.ZoomBased:
                    {
                        if (!MenuManager.AnyMenuVisible && ((IsFollowing && !JustFollow) || Input.GetMouseButton(MouseButton.Middle)))
                        { TargetRotation += MouseDelta.x * inputAngleMultiplier; }
                        TargetAngle = Mathf.Clamp(Zoom * .5f, 10f, 80f);
                        break;
                    }
            }
        }

        void HandleAndDoLocking(float deltaTime)
        {
            if (IsTotallyLocked)
            {
                transform.SetPositionAndRotation(LockOn.position, LockOn.rotation);
                theCamera.transform.localPosition = Vector3.zero;
            }
            else
            {
                float lockSpeed = 50f;

                lockSpeed *= Mathf.Clamp(Mathf.Sqrt(LockedValue) / 3, .001f, 10f);

                {
                    Vector3 displacement = LockOn.position - transform.position;
                    displacement *= .9f;
                    displacement = Vector3.ClampMagnitude(displacement, lockSpeed * deltaTime);
                    transform.Translate(displacement, Space.World);

                    transform.rotation = Quaternion.Lerp(transform.rotation, LockOn.rotation, rotationSpeed * deltaTime);
                }

                {
                    Vector3 displacement = theCamera.transform.localPosition * -0.9f;
                    displacement = Vector3.ClampMagnitude(displacement, lockSpeed * deltaTime);
                    theCamera.transform.Translate(displacement, Space.Self);
                }
            }

            cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.identity, AngleSpeed * deltaTime);

            if (LockOn.FreeMode)
            {
                Rotation = LockOn.rotation.eulerAngles.y;
                TargetRotation = Rotation;
            }

            if (LockOn.Zoomable &&
                !MenuManager.AnyMenuVisible &&
                MouseManager.MouseOnWindow)
            {
                theCamera.fieldOfView = Mathf.Clamp(theCamera.fieldOfView - (Input.mouseScrollDelta.y * 2f), 20f, 60f);
            }
        }

        void DoFollowing(float deltaTime)
        {
            if ((transform.position - FollowObject.position).sqrMagnitude > 10f)
            { ActualFollowSpeed = Mathf.Sqrt((transform.position - FollowObject.position).sqrMagnitude) * 3f; }
            else if (FollowObject.gameObject.TryGetComponent<VehicleEngine>(out var vehicle))
            { ActualFollowSpeed = vehicle.Speed + 5f; }

            Vector3 displacement = FollowObject.position - (transform.position + new Vector3(0f, Mathf.Clamp(ZoomValue * -0.1f, -FollowHeightDisplacement.y, -FollowHeightDisplacement.x), 0f));
            displacement *= .9f;
            displacement = Vector3.ClampMagnitude(displacement, ActualFollowSpeed * deltaTime);

            float verticalDisplacement = ((FollowObject.position.y + Mathf.Clamp(ZoomValue, FollowHeightDisplacement.x, FollowHeightDisplacement.y)) - transform.position.y) * deltaTime;
            displacement.y = verticalDisplacement;

            transform.Translate(displacement, Space.World);
        }

        void DoMovement(float deltaTime)
        {
            if (float.IsNaN(Velocity.x) || float.IsNaN(Velocity.y))
            { Velocity = Vector2.zero; }

            Velocity = Vector2.MoveTowards(Velocity, TargetVelocity, deltaTime * acceleration / 2);

            float verticalVelocity = Height - transform.position.y;

            if (Velocity != Vector2.zero || verticalVelocity != 0f)
            {
                // float heightMultiplier = Mathf.Clamp((ZoomValue) * 0.1f, 0.5f, 1.0f);
                float heightMultiplier = Mathf.Max(.2f, Mathf.Log(ZoomValue));

                Vector2 scaledVelocity = Velocity * heightMultiplier;

                Vector3 forwardVelocity = transform.forward * scaledVelocity.x;
                Vector3 rightVelocity = transform.right * scaledVelocity.y;

                Vector3 transition = (forwardVelocity + rightVelocity + new Vector3(0f, verticalVelocity, 0f)) * deltaTime;

                if (!float.IsNaN(transition.x) &&
                    !float.IsNaN(transition.y) &&
                    !float.IsNaN(transition.z))
                { transform.Translate(transition, Space.World); }
            }

            Velocity = Vector2.MoveTowards(Velocity, TargetVelocity, deltaTime * acceleration / 2);
        }

        void DoRotation(float deltaTime)
        {
            if (Rotation == TargetRotation) return;

            Rotation = Mathf.LerpAngle(Rotation, TargetRotation, 1f - Mathf.Pow(.5f, rotationSpeed * deltaTime));
            transform.rotation = Quaternion.Euler(0f, Rotation, 0f);
        }

        void DoZooming(float deltaTime)
        {
            Zoom = Mathf.Lerp(Zoom, TargetZoom, 1f - Mathf.Pow(.5f, currentZoomSpeed * deltaTime));
            Zoom = Mathf.Max(Zoom, 0f);

            float zoomTransition = Zoom - ZoomValue;

            if (zoomTransition == 0) return;

            theCamera.transform.Translate(new Vector3(0f, 0f, -zoomTransition), Space.Self);
        }

        void DoAngle(float deltaTime)
        {
            cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.Euler(TargetAngle, 0f, 0f), 1f - Mathf.Pow(.5f, AngleSpeed * deltaTime));
        }

        void FixedUpdate()
        {
            if (IsLocked)
            {
                if (!IsTotallyLocked)
                {
                    if (LockedValue < .0001f)
                    { IsTotallyLocked = true; }
                }

                return;
            }

            Height = TheTerrain.Height(transform.position) + 1f;

            theCamera.transform.localRotation = Quaternion.identity;
            IsTotallyLocked = false;

            if (cameraMode != lastCameraMode)
            {
                OnCameraModeChanged?.Invoke(cameraMode);
                lastCameraMode = cameraMode;
            }
        }
    }
}
