using AssetManager;

using Game.Managers;

using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Seek : UnitBehaviour_Base, IHaveAssetFields
    {
        internal const float DISTANCE_TO_STOP = 4f;
        internal const float BRAKING_DISTANCE_ERROR = .5f;
        internal const float DISTANCE_TO_STOP_BRUH = 4f;

        [SerializeField, AssetField] bool FollowCursor;

        [SerializeField, ReadOnly] internal Vector3 Target;

        [Header("Movement")]
        [SerializeField, AssetField] float maxDistanceToReverse = 32f;
        [SerializeField, AssetField] internal bool useBrakingCalculations;

        [SerializeField, ReadOnly] internal bool currentlyStopping;
        [SerializeField, ReadOnly] float BrakingDistance;

        internal override Vector2? GetOutput()
        {
            if (FollowCursor)
            {
                if (!MenuManager.AnyMenuVisible && MouseManager.MouseOnWindow)
                { Target = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition); }
            }

            if (Target == Vector3.zero) return null;

            return CalculateInputVector();
        }

        Vector2 CalculateInputVector()
        {
            if (currentlyStopping) return Vector2.zero;

            if (useBrakingCalculations)
            { BrakingDistance = MovementEngine.CalculateBrakingDistance(); }

            return UnitBehaviour_Seek.CalculateInputVector(transform, Target, useBrakingCalculations ? BrakingDistance : null, maxDistanceToReverse, MovementEngine);
        }

        internal static Vector2 CalculateInputVector(Transform transform, Vector3 target, float? brakingDistance, float maxDistanceToReverse, MovementEngine movementEngine)
        {
            // Get destination
            Vector3 destinationPosition = target;

            // No destination?
            if (destinationPosition == Vector3.zero) return Vector2.zero;

            float distanceToDestination = Vector3.Distance(transform.position, destinationPosition);

            // === Braking ===
            if (distanceToDestination <= DISTANCE_TO_STOP_BRUH)
            { return Vector2.zero; }
            else if (brakingDistance.HasValue)
            {
                if (distanceToDestination <= Mathf.Abs(brakingDistance.Value) + BRAKING_DISTANCE_ERROR)
                { return Vector2.zero; }
            }
            else if (distanceToDestination <= DISTANCE_TO_STOP)
            { return Vector2.zero; }
            // === ===

            // === Normal ===
            {
                Vector2 vectorToTarget = (destinationPosition - transform.position).To2D();

                // === Turn towards target ===

                // -180 (Left) ... 0 ... 180 (Right)
                float angleToTarget = -Vector2.SignedAngle(transform.forward.To2D(), vectorToTarget);

                float dot = Vector2.Dot(transform.forward.To2D(), vectorToTarget.normalized);

                /*
                // === Reversing 1 ===
                if (
                    // If its close enough
                    distanceToDestination <= 16f &&
                    // If its next to us
                    Mathf.Abs(dot) <= 0.8f)
                {
                    // Reverse and turn away from target -> later we can go forward to the target
                    if (VehicleEngine.IsReverse) return new Vector2(Mathf.Clamp(-angleToTarget / 15f, -1f, 1f), -1f);
                    else return new Vector2(Mathf.Clamp(angleToTarget / 15f, -1f, 1f), -1f);
                }
                // ===  ===
                */

                float steerAmount = Mathf.Clamp(angleToTarget / 90f, -1f, 1f);

                // ===  ===

                float torque;

                if (distanceToDestination > 32f)
                { torque = 1f; }
                else if (transform.TryGetComponent(out VehicleEngine vehicleEngine) && vehicleEngine.isHaveTracks)
                {
                    steerAmount = Mathf.Clamp(steerAmount * 16, -1, 1);
                    torque = Mathf.Abs(steerAmount) < .01f ? 1f : 0f;
                }
                else
                { torque = 1f - Mathf.Abs(steerAmount) + 0.5f; }


                // === Reversing 2 ===
                if (
                    // We are close to the target
                    distanceToDestination <= maxDistanceToReverse &&
                    // If its behind
                    dot < -0.8f)
                {
                    // Reverse and turn to the target
                    torque *= -1f;
                    steerAmount *= -1f;
                }
                // ===  ===

                if (movementEngine.IsReverse) steerAmount *= -1;
                return new Vector2(steerAmount, torque);
            }
            // === ===
        }
    }
}
