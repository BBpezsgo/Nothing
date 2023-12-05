using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Goto : UnitBehaviour_Base
    {
        internal const float DISTANCE_TO_STOP = 8f;
        internal const float DISTANCE_TO_STOP_BRUH = 4f;
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

                if (NetcodeUtils.IsServer)
                { NetTarget.Value = value; }
            }
        }
        [SerializeField, ReadOnly] Vector3 target;
        readonly NetworkVariable<Vector3> NetTarget = new();

        [Header("Movement")]
        [SerializeField] float maxDistanceToReverse = 32f;
        [SerializeField] internal bool useBrakingCalculations;

        [SerializeField, ReadOnly] internal bool currentlyStopping;
        [SerializeField, ReadOnly] float BrakingDistance;

        internal override Vector2? GetOutput()
        {
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
    }
}
