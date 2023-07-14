using UnityEngine;

using Utilities;

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

    [SerializeField, ReadOnly] Terrain Terrain;
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

    [SerializeField, ReadOnly] float TargetZoom;
    [SerializeField, ReadOnly] float Zoom;

    [Header("Scoping")]
    [SerializeField, ReadOnly] internal Transform CurrentScope;
    internal bool IsScoping => CurrentScope != null;
    /// <summary>
    /// From 0f to 40f
    /// </summary>
    internal float ScopeZoom => 60f - theCamera.fieldOfView;
    [SerializeField, ReadOnly] bool ScopeTotallyLocked = false;

    // [Header("Other")]
    internal float ZoomValue => Mathf.Max(0f, -theCamera.transform.localPosition.z);
    internal Vector3 CameraPosition => theCamera.transform.position;
    internal event OnCameraModeChanged OnCameraModeChanged;

    internal static Vector2 MouseDelta => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"[{nameof(CameraController)}]: Instance already registered");
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

        Terrain = FindObjectOfType<Terrain>();
    }

    void Update()
    {
        if (IsScoping)
        {
            HandleAndDoScope(Time.unscaledDeltaTime);
            return;
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

        if (!MenuManager.AnyMenuVisible)
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
        if (!MenuManager.AnyMenuVisible)
        {
            MovementInput.x = Input.GetAxisRaw("Vertical");
            MovementInput.y = Input.GetAxisRaw("Horizontal");

            TargetVelocity = MovementInput * maxSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            { TargetVelocity *= SpeedBonusOnShift; }
        }
        else
        {
            MovementInput = Vector2.zero;
            TargetVelocity = Vector2.zero;
        }
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

    void HandleAndDoScope(float deltaTime)
    {
        if (ScopeTotallyLocked)
        {
            transform.SetPositionAndRotation(CurrentScope.position, CurrentScope.rotation);
            theCamera.transform.localPosition = Vector3.zero;
        }
        else
        {
            float lockSpeed = 50f;
            {
                Vector3 displacement = CurrentScope.position - transform.position;
                displacement *= .9f;
                displacement = Vector3.ClampMagnitude(displacement, lockSpeed * deltaTime);
                transform.Translate(displacement, Space.World);

                transform.rotation = Quaternion.Lerp(transform.rotation, CurrentScope.rotation, rotationSpeed * deltaTime);
            }

            {
                Vector3 displacement = theCamera.transform.localPosition * -0.9f;
                displacement = Vector3.ClampMagnitude(displacement, lockSpeed * deltaTime);
                theCamera.transform.Translate(displacement, Space.Self);
            }
        }

        cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.identity, AngleSpeed * deltaTime);

        Rotation = CurrentScope.rotation.eulerAngles.y;
        TargetRotation = Rotation;

        if (!MenuManager.AnyMenuVisible)
        { theCamera.fieldOfView = Mathf.Clamp(theCamera.fieldOfView - (Input.mouseScrollDelta.y * 2f), 20f, 60f); }
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

        // float heightMultiplier = Mathf.Clamp((ZoomValue) * 0.1f, 0.5f, 1.0f);
        float heightMultiplier = Mathf.Max(.2f, Mathf.Log(ZoomValue));

        Vector2 scaledVelocity = Velocity * heightMultiplier;

        Vector3 forwardVelocity = transform.forward * scaledVelocity.x;
        Vector3 rightVelocity = transform.right * scaledVelocity.y;

        float verticalVelocity = Height - transform.position.y;

        Vector3 transition = (forwardVelocity + rightVelocity + new Vector3(0f, verticalVelocity, 0f)) * deltaTime;

        if (!float.IsNaN(transition.x) &&
            !float.IsNaN(transition.y) &&
            !float.IsNaN(transition.z))
        { transform.Translate(transition, Space.World); }

        Velocity = Vector2.MoveTowards(Velocity, TargetVelocity, deltaTime * acceleration / 2);
    }

    void DoRotation(float deltaTime)
    {
        Rotation = Mathf.LerpAngle(Rotation, TargetRotation, 1f - Mathf.Pow(.5f, rotationSpeed * deltaTime));
        transform.rotation = Quaternion.Euler(0f, Rotation, 0f);
    }

    void DoZooming(float deltaTime)
    {
        Zoom = Mathf.Lerp(Zoom, TargetZoom, 1f - Mathf.Pow(.5f, zoomSpeed * deltaTime));
        Zoom = Mathf.Max(Zoom, 0f);

        float zoomTransition = Zoom - ZoomValue;

        theCamera.transform.Translate(new Vector3(0f, 0f, -zoomTransition), Space.Self);
    }

    void DoAngle(float deltaTime)
    {
        cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.Euler(TargetAngle, 0f, 0f), 1f - Mathf.Pow(.5f, AngleSpeed * deltaTime));
    }

    void FixedUpdate()
    {
        if (IsScoping)
        {
            if (!ScopeTotallyLocked)
            {
                if ((theCamera.transform.position - CurrentScope.position).sqrMagnitude < .1f)
                { ScopeTotallyLocked = true; }
            }

            return;
        }

        if (Terrain != null)
        {
            Height = Terrain.SampleHeight(transform.position) + Terrain.transform.position.y + 1f;
        }

        theCamera.transform.localRotation = Quaternion.identity;
        ScopeTotallyLocked = false;

        if (cameraMode != lastCameraMode)
        {
            OnCameraModeChanged?.Invoke(cameraMode);
            lastCameraMode = cameraMode;
        }
    }

    /*
    void FixedUpdate()
    {
        if (IsScoping)
        {
            if (!ScopeTotallyLocked)
            {
                float lockSpeed = 50f;
                {
                    Vector3 displacement = CurrentScope.position - transform.position;
                    displacement *= .9f;
                    displacement = Vector3.ClampMagnitude(displacement, lockSpeed * Time.fixedUnscaledDeltaTime);
                    transform.Translate(displacement, Space.World);

                    transform.rotation = Quaternion.Lerp(transform.rotation, CurrentScope.rotation, rotationSpeed * Time.fixedUnscaledDeltaTime);
                }

                {
                    Vector3 displacement = theCamera.transform.localPosition * -0.9f;
                    displacement = Vector3.ClampMagnitude(displacement, lockSpeed * Time.fixedUnscaledDeltaTime);
                    theCamera.transform.Translate(displacement, Space.Self);
                }

                if ((theCamera.transform.position - CurrentScope.position).sqrMagnitude < .1f)
                { ScopeTotallyLocked = true; }
            }

            cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.identity, AngleSpeed * Time.fixedUnscaledDeltaTime);

            rotation = CurrentScope.rotation.eulerAngles.y;
            targetRotation = rotation;

            return;
        }
        else
        {
            theCamera.transform.localRotation = Quaternion.identity;
            ScopeTotallyLocked = false;
        }

        if (cameraMode != lastCameraMode)
        {
            OnCameraModeChanged?.Invoke(cameraMode);
            lastCameraMode = cameraMode;
        }

        if (IsFollowing)
        {
            if ((transform.position - FollowObject.position).sqrMagnitude > 10f)
            { ActualFollowSpeed = Mathf.Sqrt((transform.position - FollowObject.position).sqrMagnitude) * 3f; }
            else if (FollowObject.gameObject.TryGetComponent<VehicleEngine>(out var vehicle))
            { ActualFollowSpeed = vehicle.Speed + 5f; }

            Vector3 displacement = FollowObject.position - (transform.position + new Vector3(0f, Mathf.Clamp(ZoomValue * -0.1f, -FollowHeightDisplacement.y, -FollowHeightDisplacement.x), 0f));
            displacement *= .9f;
            displacement = Vector3.ClampMagnitude(displacement, ActualFollowSpeed * Time.fixedUnscaledDeltaTime);

            float verticalDisplacement = ((FollowObject.position.y + Mathf.Clamp(ZoomValue, FollowHeightDisplacement.x, FollowHeightDisplacement.y)) - transform.position.y) * Time.fixedUnscaledDeltaTime;
            displacement.y = verticalDisplacement;

            transform.Translate(displacement, Space.World);
        }
        else
        {
            if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y))
            { velocity = Vector2.zero; }

            velocity = Vector2.Lerp(velocity, targetVelocity, Time.fixedUnscaledDeltaTime * acceleration);
            Vector2 currentVelocity = velocity * Mathf.Clamp(-theCamera.transform.localPosition.z * 0.05f, minVelocity, maxVelocity);
            Vector3 forwardVelocity = transform.forward * currentVelocity.x;
            Vector3 rightVelocity = transform.right * currentVelocity.y;

            float verticalDisplacement = (1f - transform.position.y) * Time.fixedUnscaledDeltaTime;

            Vector3 transition = forwardVelocity + rightVelocity + new Vector3(0f, verticalDisplacement, 0f);
            if (!float.IsNaN(transition.x) && !float.IsNaN(transition.y) && !float.IsNaN(transition.z))
            { transform.Translate(transition, Space.World); }
        }

        {
            rotation = Mathf.LerpAngle(rotation, targetRotation, Time.fixedUnscaledDeltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        {
            float currentZoomSpeed = zoomSpeed * Mathf.Clamp(-theCamera.transform.localPosition.z * 0.05f, minZoomSpeed, maxZoomSpeed);
            zoom = Mathf.Lerp(zoom, targetZoom, Time.fixedUnscaledDeltaTime * currentZoomSpeed);
            zoom = Mathf.Max(zoom, 0f);

            theCamera.transform.Translate(new Vector3(0f, 0f, (-zoom) - theCamera.transform.localPosition.z), Space.Self);
        }

        cameraRotation.transform.localRotation = Quaternion.Lerp(cameraRotation.transform.localRotation, Quaternion.Euler(targetAngle, 0f, 0f), AngleSpeed * Time.fixedUnscaledDeltaTime);
    }
    */
}
