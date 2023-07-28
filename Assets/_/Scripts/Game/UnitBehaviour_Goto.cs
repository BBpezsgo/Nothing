using AssetManager;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Goto : UnitBehaviour_Base, IHaveAssetFields
    {
        internal const float DISTANCE_TO_STOP = 4f;
        internal const float BRAKING_DISTANCE_ERROR = .5f;

        internal Vector3 Target
        {
            get
            {
                if (NetcodeUtils.IsClient)
                { return NetTarget.Value; }

                return target;
            }
            set
            {
                if (NetcodeUtils.IsClient)
                { return; }

                target = value;
                NetTarget.Value = value;
            }
        }
        [SerializeField, ReadOnly] Vector3 target;
        readonly NetworkVariable<Vector3> NetTarget = new();

        [Header("Movement")]
        [SerializeField, AssetField] float maxDistanceToReverse = 32f;
        [SerializeField, AssetField] internal bool useBrakingCalculations;

        [SerializeField, ReadOnly] internal bool currentlyStopping;
        [SerializeField, ReadOnly] float BrakingDistance;

        internal override Vector2? GetOutput()
        {
            if (Target == Vector3.zero) return null;

            return CalculateInputVector();
        }

        Vector2 CalculateInputVector()
        {
            // Stop?
            if (currentlyStopping) return Vector2.zero;

            // Get destination
            Vector3 destinationPosition = Target;

            // No destination?
            if (destinationPosition == Vector3.zero) return Vector2.zero;

            float distanceToDestination = Vector3.Distance(transform.position, destinationPosition);

            // === Braking ===
            if (useBrakingCalculations)
            {
                BrakingDistance = MovementEngine.CalculateBrakingDistance();

                if (distanceToDestination < Mathf.Abs(BrakingDistance) + BRAKING_DISTANCE_ERROR)
                { return Vector2.zero; }
            }
            else if (distanceToDestination < DISTANCE_TO_STOP)
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

                float torque = distanceToDestination > 16f ? 1f : (1f - Mathf.Abs(steerAmount) + 0.5f);

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

                if (MovementEngine.IsReverse) steerAmount *= -1;
                return new Vector2(steerAmount, torque);
            }
            // === ===
        }
    }
}
