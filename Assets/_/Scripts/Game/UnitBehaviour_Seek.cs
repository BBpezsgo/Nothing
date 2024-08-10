using System;
using Game.Managers;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class UnitBehaviour_Seek : UnitBehaviour_Base
    {
        public const float DISTANCE_TO_STOP = 4f;
        public const float BRAKING_DISTANCE_ERROR = .5f;
        public const float DISTANCE_TO_STOP_BRUH = 4f;

        [SerializeField] bool FollowCursor;

        [SerializeField, ReadOnly] public Vector3 Target;

        [Header("Movement")]
        [SerializeField] float maxDistanceToReverse = 32f;
        [SerializeField] public bool useBrakingCalculations;

        [SerializeField, ReadOnly] public bool currentlyStopping;
        [SerializeField, ReadOnly] float BrakingDistance;

        public override Vector2? GetOutput()
        {
            if (FollowCursor)
            {
                if (!MenuManager.AnyMenuVisible && MouseManager.MouseOnWindow)
                { Target = MainCamera.Camera.ScreenToWorldPosition(Mouse.LockedPosition); }
            }

            if (Target == default) return null;

            return CalculateInputVector();
        }

        Vector2 CalculateInputVector()
        {
            if (currentlyStopping) return default;

            if (useBrakingCalculations)
            { BrakingDistance = MovementEngine.CalculateBrakingDistance(); }

            return UnitBehaviour_Seek.CalculateInputVector(transform, Target, useBrakingCalculations ? BrakingDistance : null, maxDistanceToReverse, MovementEngine);
        }

        public static Vector2 CalculateInputVector(Transform transform, Vector3 target, float? brakingDistance, float maxDistanceToReverse, MovementEngine movementEngine)
        {
            // Get destination
            Vector3 destinationPosition = target;

            // No destination?
            if (destinationPosition == default) return default;

            float distanceToDestination = Vector3.Distance(transform.position, destinationPosition);

            // === Braking ===
            if (distanceToDestination <= DISTANCE_TO_STOP_BRUH)
            { return default; }
            else if (brakingDistance.HasValue)
            {
                if (distanceToDestination <= Math.Abs(brakingDistance.Value) + BRAKING_DISTANCE_ERROR)
                { return default; }
            }
            else if (distanceToDestination <= DISTANCE_TO_STOP)
            { return default; }
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
                    Maths.Abs(dot) <= 0.8f)
                {
                    // Reverse and turn away from target -> later we can go forward to the target
                    if (VehicleEngine.IsReverse) return new Vector2(Maths.Clamp(-angleToTarget / 15f, -1f, 1f), -1f);
                    else return new Vector2(Maths.Clamp(angleToTarget / 15f, -1f, 1f), -1f);
                }
                // ===  ===
                */

                float steerAmount = Math.Clamp(angleToTarget / 90f, -1f, 1f);

                // ===  ===

                float torque;

                if (distanceToDestination > 32f)
                { torque = 1f; }
                else if (transform.TryGetComponent(out VehicleEngine vehicleEngine) && vehicleEngine.isHaveTracks)
                {
                    steerAmount = Math.Clamp(steerAmount * 16, -1, 1);
                    torque = Math.Abs(steerAmount) < .01f ? 1f : 0f;
                }
                else
                { torque = 1f - Math.Abs(steerAmount) + 0.5f; }

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

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            if (Target == default) return;
            if (currentlyStopping) return;

            Gizmos.color = new Color(1f, 1f, 1f, .5f);
            Gizmos.DrawLine(transform.position, Target);
            Gizmos.color = Color.white;
            GizmosPlus.DrawPoint(Target, 1f);

            Debug3D.Label(Target, "Seek Target");
        }
    }
}
