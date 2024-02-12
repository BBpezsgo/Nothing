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
        OrthographicTopDown,
    }

    internal delegate void OnCameraModeChanged(CameraMode mode);

    public class CameraController : SingleInstance<CameraController>
    {
        [SerializeField] Camera theCamera;
        [SerializeField] Transform cameraRotation;
        [SerializeField] internal CameraMode cameraMode;
        CameraMode lastCameraMode;

        [SerializeField] float Height = 1f;

        [Header("Following")]
        [SerializeField] internal Transform FollowObject;
        internal bool IsFollowing => FollowObject != null;
        [SerializeField, Tooltip("Use the default movement behavior when following an object")] internal bool JustFollow;
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
        [SerializeField] public CameraLockable LockOn;
        public bool IsLocked => LockOn != null;

        public bool TryOverrideLock(CameraLockable @lock, int lockPriority)
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
        internal float ZoomValue => Maths.Max(0f, -theCamera.transform.localPosition.z);
        internal Vector3 CameraPosition => theCamera.transform.position;
        internal event OnCameraModeChanged OnCameraModeChanged;

        TouchZoom TouchZoom;

        void Start()
        {
            Zoom = -theCamera.transform.localPosition.z;
            TargetZoom = Zoom;

            TargetAngle = Maths.Clamp(cameraRotation.transform.localRotation.eulerAngles.x, 10f, 80f);

            Rotation = transform.rotation.eulerAngles.y;
            TargetRotation = Rotation;

            lastCameraMode = cameraMode;

            TouchZoom = new TouchZoom(1, () => !MenuManager.AnyMenuVisible);
            TouchZoom.OnZoom += OnTouchZoom;
            TouchZoom.OnMove += OnTouchMove;

            currentZoomSpeed = zoomSpeed;
            targetZoomSpeed = currentZoomSpeed;

            if (cameraMode == CameraMode.OrthographicTopDown)
            {
                Zoom = 60;
                TargetZoom = Zoom;
            }
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
                        TargetAngle = Maths.Clamp(TargetAngle - (delta.y * inputAngleMultiplier), (IsFollowing && !JustFollow) ? -80 : 10f, 80f);
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
                        TargetAngle = Maths.Clamp(Zoom * .5f, 10f, 80f);
                        break;
                    }
                case CameraMode.OrthographicTopDown:
                    {
                        TargetRotation = 45f;
                        TargetAngle = 45f;
                        break;
                    }
            }
        }

        void OnTouchZoom(TouchZoom sender, float delta)
        {
            float zoomInput = delta * 30f;
            zoomInput *= Maths.Max(Maths.Log(ZoomValue), 1f);

            TargetZoom = Maths.Max(TargetZoom + zoomInput, 0f);

            Zoom = Maths.Max(TargetZoom, 0f);
        }

        void Update()
        {
            currentZoomSpeed = Maths.MoveTowards(currentZoomSpeed, targetZoomSpeed, 5 * Time.unscaledDeltaTime);

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
                MovementInput = default;
                TargetVelocity = default;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            { cameraMode = (CameraMode)(((int)cameraMode + 1) % 4); }

            HandleRotationInput();

            HandleZooming();

            if (IsFollowing)
            { DoFollowing(Time.unscaledDeltaTime); }
            else
            { DoMovement(Time.unscaledDeltaTime); }

            DoRotation(Time.unscaledDeltaTime);

            DoZooming(Time.unscaledDeltaTime);

            DoAngle(Time.unscaledDeltaTime);

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

        void HandleZooming()
        {
            if (MenuManager.AnyMenuVisible ||
                !MouseManager.MouseOnWindow ||
                MouseManager.IsPointerOverUI())
            { return; }

            float zoomInput;
            if (MouseAlt.HasScrollDelta)
            { zoomInput = MouseAlt.ScrollDelta * .2f; }
            else
            { zoomInput = -Mouse.ScrollDelta * zoomInputMultiplier; }

            if (Input.GetKey(KeyCode.LeftShift))
            { zoomInput *= ZoomBonusOnShift; }

            zoomInput *= Maths.Max(Maths.Log(ZoomValue), 1f);

            TargetZoom = Maths.Max(TargetZoom + zoomInput, 0f);
        }

        void HandleMovementInput()
        {
            if (MenuManager.AnyMenuVisible)
            {
                MovementInput = default;
                TargetVelocity = default;
                return;
            }

            if (TouchJoystick.Instance != null && TouchJoystick.Instance.IsActiveAndCaptured)
            {
                MovementInput = TouchJoystick.Instance.RawInput;
                MovementInput = new Vector2(MovementInput.y, MovementInput.x);
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
                        if (!MenuManager.AnyMenuVisible)
                        {
                            if (MouseAlt.HasDelta)
                            {
                                TargetRotation += MouseAlt.DeltaX;
                                TargetAngle = Maths.Clamp(TargetAngle - (MouseAlt.DeltaY), (IsFollowing && !JustFollow) ? -80 : 10f, 80f);
                            }
                            else if ((IsFollowing && !JustFollow) || Input.GetMouseButton(Mouse.Middle))
                            {
                                TargetRotation += Mouse.DeltaX * inputAngleMultiplier;
                                TargetAngle = Maths.Clamp(TargetAngle - (Mouse.DeltaY * inputAngleMultiplier), (IsFollowing && !JustFollow) ? -80 : 10f, 80f);
                            }
                        }

                        break;
                    }
                case CameraMode.TopDown:
                    {
                        if (!MenuManager.AnyMenuVisible)
                        {
                            if (MouseAlt.HasDelta)
                            { TargetRotation += Mouse.DeltaX; }
                            else if (Input.GetMouseButton(Mouse.Middle))
                            { TargetRotation += Mouse.DeltaX * inputAngleMultiplier; }
                        }

                        TargetAngle = 80f;
                        break;
                    }
                case CameraMode.ZoomBased:
                    {
                        if (!MenuManager.AnyMenuVisible)
                        {
                            if (MouseAlt.HasDelta)
                            { TargetRotation += MouseAlt.DeltaX; }
                            else if (Input.GetMouseButton(Mouse.Middle))
                            { TargetRotation += Mouse.DeltaX * inputAngleMultiplier; }
                        }

                        TargetAngle = Maths.Clamp(Zoom * .5f, 10f, 80f);
                        break;
                    }

                case CameraMode.OrthographicTopDown:
                    {
                        TargetRotation = 45f;
                        TargetAngle = 45f;
                        break;
                    }
            }
        }

        void HandleAndDoLocking(float deltaTime)
        {
            if (IsTotallyLocked)
            {
                transform.SetPositionAndRotation(LockOn.position, LockOn.rotation);
                theCamera.transform.localPosition = default;
            }
            else
            {
                float lockSpeed = 50f;

                lockSpeed *= Maths.Clamp(Maths.Sqrt(LockedValue) / 3, .001f, 10f);

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
                theCamera.fieldOfView = Maths.Clamp(theCamera.fieldOfView - (Input.mouseScrollDelta.y * 2f), 20f, 60f);
            }
        }

        void DoFollowing(float deltaTime)
        {
            if ((transform.position - FollowObject.position).sqrMagnitude > 10f)
            { ActualFollowSpeed = Maths.Sqrt((transform.position - FollowObject.position).sqrMagnitude) * 3f; }
            else if (FollowObject.gameObject.TryGetComponent(out VehicleEngine vehicle))
            { ActualFollowSpeed = vehicle.Speed + 5f; }

            Vector3 displacement = FollowObject.position - (transform.position + new Vector3(0f, Maths.Clamp(ZoomValue * -0.1f, -FollowHeightDisplacement.y, -FollowHeightDisplacement.x), 0f));
            displacement *= .9f;
            displacement = Vector3.ClampMagnitude(displacement, ActualFollowSpeed * deltaTime);

            float verticalDisplacement = ((FollowObject.position.y + Maths.Clamp(ZoomValue, FollowHeightDisplacement.x, FollowHeightDisplacement.y)) - transform.position.y) * deltaTime;
            displacement.y = verticalDisplacement;

            transform.Translate(displacement, Space.World);
        }

        void DoMovement(float deltaTime)
        {
            if (float.IsNaN(Velocity.x) || float.IsNaN(Velocity.y))
            { Velocity = default; }

            Velocity = Vector2.MoveTowards(Velocity, TargetVelocity, deltaTime * acceleration / 2);

            float verticalVelocity = Height - transform.position.y;

            if (Velocity != default || verticalVelocity != 0f)
            {
                // float heightMultiplier = Maths.Clamp((ZoomValue) * 0.1f, 0.5f, 1.0f);
                float heightMultiplier = Maths.Max(.2f, Maths.Log(ZoomValue));

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

            Rotation = Maths.LerpAngle(Rotation, TargetRotation, 1f - Maths.Pow(.5f, rotationSpeed * deltaTime));
            transform.rotation = Quaternion.Euler(0f, Rotation, 0f);
        }

        void DoZooming(float deltaTime)
        {
            if (cameraMode == CameraMode.OrthographicTopDown)
            {
                Zoom = Maths.Lerp(Zoom, TargetZoom, 1f - Maths.Pow(.5f, currentZoomSpeed * deltaTime));
                Zoom = Maths.Max(Zoom, 0f);

                float zoomTransition = Zoom - ZoomValue;
                if (zoomTransition != 0)
                { theCamera.transform.Translate(new Vector3(0f, 0f, -zoomTransition), Space.Self); }

                if (!theCamera.orthographic)
                {
                    theCamera.orthographic = true;
                    theCamera.ResetProjectionMatrix();
                }

                theCamera.orthographicSize = Maths.Max(Zoom * .4f, 1f);
            }
            else
            {
                Zoom = Maths.Lerp(Zoom, TargetZoom, 1f - Maths.Pow(.5f, currentZoomSpeed * deltaTime));
                Zoom = Maths.Max(Zoom, 0f);

                float zoomTransition = Zoom - ZoomValue;
                if (zoomTransition != 0)
                { theCamera.transform.Translate(new Vector3(0f, 0f, -zoomTransition), Space.Self); }

                if (theCamera.orthographic)
                {
                    theCamera.orthographic = false;
                    theCamera.ResetProjectionMatrix();
                }
            }
        }

        void DoAngle(float deltaTime)
        {
            cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.Euler(TargetAngle, 0f, 0f), 1f - Maths.Pow(.5f, AngleSpeed * deltaTime));
        }
    }
}
