using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Utilities;

namespace Game.Managers
{
    [RequireComponent(typeof(Camera))]
    public class Focus : MonoBehaviour
    {
        [SerializeField, ReadOnly] Camera Camera;
        [SerializeField] VolumeProfile Volume;
        DepthOfField DepthOfField;
        MinFloatParameter FocusDistance;

        Ray Ray;
        RaycastHit RayHit;
        [SerializeField] LayerMask Mask;

        [SerializeField] float DefaultDistance = 5f;
        [SerializeField] float MinDistance = 0.5f;
        [SerializeField, ReadOnly] float HitDistance;
        [SerializeField, ReadOnly] float TargetFocusDistance;
        [SerializeField] float focusSpeed = 1f;
        [SerializeField] float Laziness;
        [SerializeField, ReadOnly] float NextRefresh;

        void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        void Start()
        {
            Volume.TryGet(out DepthOfField);
            FocusDistance = DepthOfField.focusDistance;
            NextRefresh = Laziness;
        }

        void Update()
        {
            FocusDistance.value = Mathf.Lerp(FocusDistance.value, TargetFocusDistance, focusSpeed * Time.unscaledDeltaTime);

            if (NextRefresh > 0f)
            {
                NextRefresh -= Time.unscaledDeltaTime;
                return;
            }
            NextRefresh = Laziness;

            if (MenuManager.AnyMenuVisible) return;
            if (!MouseManager.MouseOnWindow) return;

            Vector3 mousePosition = Mouse.LockedPosition;

            if (mousePosition.x == int.MinValue ||
                mousePosition.y == int.MinValue)
            {
                TargetFocusDistance = DefaultDistance;
                return;
            }

            Ray = Camera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(Ray, out RayHit, DefaultDistance - 0.1f, Mask))
            {
                HitDistance = RayHit.distance;

                /*
                if (RayHit.collider.gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    Debug3D.DrawSphere(RayHit.point, .11f, Color.gray, Laziness);
                }
                else
                {
                    float closestDistance = float.MaxValue;
                    Vector3 closestPoint = default;

                    var meshes = RayHit.collider.gameObject.GetComponentsInChildren<MeshFilter>(false);
                    for (int i = 0; i < meshes.Length; i++)
                    {
                        if (!meshes[i].mesh.isReadable) continue;
                        var point = meshes[i].ClosestPoint(RayHit.point);
                        float distance = (point - RayHit.point).sqrMagnitude;

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPoint = point;
                        }
                    }
                    HitDistance = (closestPoint - Ray.origin).magnitude;
                }
                */

                if (HitDistance < MinDistance)
                { HitDistance = MinDistance; }
                TargetFocusDistance = HitDistance;

                Debug3D.DrawSphere(RayHit.point, .1f, Color.white, Laziness);
            }
            else
            {
                if (FocusDistance.value < DefaultDistance)
                {
                    TargetFocusDistance = DefaultDistance;
                }
            }
        }
    }
}
