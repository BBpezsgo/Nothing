using Unity.Netcode;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class UnitBehaviour_Goto : UnitBehaviour_Base
    {
        public const float DISTANCE_TO_STOP = 8f;
        public const float DISTANCE_TO_STOP_BRUH = 4f;
        public const float BRAKING_DISTANCE_ERROR = .5f;

        public Vector3 Target
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
        [SerializeField] public bool useBrakingCalculations;

        [SerializeField, ReadOnly] public bool currentlyStopping;
        [SerializeField, ReadOnly] float BrakingDistance;

        protected virtual void Start()
        {
            Target = transform.position;
        }

        public override Vector2? GetOutput()
        {
            if (Target == default) return null;

            if (currentlyStopping) return default;

            if (useBrakingCalculations)
            { BrakingDistance = MovementEngine.CalculateBrakingDistance(); }

            return UnitBehaviour_Seek.CalculateInputVector(transform, Target, useBrakingCalculations ? BrakingDistance : null, maxDistanceToReverse, MovementEngine);
        }

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            if (Target == default) return;
            if (currentlyStopping) return;

            Gizmos.color = new Color(1f, 1f, 1f, .5f);
            Gizmos.DrawLine(transform.position, Target);
            Gizmos.color = CoolColors.White;
            GizmosPlus.DrawPoint(Target, 1f);
            Debug3D.Label(Target, "Goto Target");
        }
    }
}
