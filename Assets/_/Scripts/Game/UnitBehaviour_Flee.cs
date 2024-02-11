using System.Collections.Generic;
using Game.Managers;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class UnitBehaviour_Flee : UnitBehaviour_Base
    {
        [SerializeField, Min(0f)] float FleeDistance;
        [SerializeField, ReadOnly] public Transform FleeFrom;
        [SerializeField, ReadOnly] float TimeToCheck = .1f;
        [SerializeField, ReadOnly] BaseObject BaseObject;
        [SerializeField, ReadOnly] Vector3 Target;

        public int TeamHash => BaseObject.TeamHash;

        protected override void Awake()
        {
            base.Awake();
            BaseObject = GetComponent<BaseObject>();
        }

        protected virtual void Start()
        {
            TimeToCheck = Random.value + 1f;
        }

        void Update()
        {
            TimeToCheck -= Time.deltaTime;
        }

        public override Vector2? GetOutput()
        {
            if (TimeToCheck <= 0f)
            { Detect(); }

            if (FleeFrom == null) return null;

            Target = (transform.position - FleeFrom.position);

            Target = FleeFrom.transform.position + (Target.normalized * FleeDistance);

            return UnitBehaviour_Seek.CalculateInputVector(transform, Target, null, 32f, MovementEngine);
        }

        void Detect()
        {
            if (TeamHash == -1) return;

            List<Unit> allUnit = RegisteredObjects.Units;
            List<Building> allBuilding = RegisteredObjects.Buildings;

            for (int i = allUnit.Count - 1; i >= 0; i--)
            {
                if (allUnit[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(TeamHash, allUnit[i].TeamHash) <= 0f)
                { continue; }
                float distance = Maths.Distance(transform.position, allUnit[i].transform.position);
                if (distance > FleeDistance)
                { continue; }
                if (FleeFrom != null && Maths.Distance(transform.position, FleeFrom.position) < distance)
                { continue; }

                FleeFrom = allUnit[i].transform;
            }

            for (int i = allBuilding.Count - 1; i >= 0; i--)
            {
                if (allBuilding[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(TeamHash, allBuilding[i].TeamHash) <= 0f)
                { continue; }
                float distance = Maths.Distance(transform.position, allBuilding[i].transform.position);
                if (distance > FleeDistance)
                { continue; }
                if (FleeFrom != null && Maths.Distance(transform.position, FleeFrom.position) < distance)
                { continue; }

                FleeFrom = allBuilding[i].transform;
            }
        }

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            if (FleeFrom == null) return;

            Gizmos.color = CoolColors.Red;
            Gizmos.DrawLine(transform.position, FleeFrom.position);
            GizmosPlus.DrawPoint(FleeFrom.position, 1f);
            Debug3D.Label(FleeFrom.position, "Flee From");

            Gizmos.color = new Color(1f, 1f, 1f, .5f);
            Gizmos.DrawLine(transform.position, Target);
            Gizmos.color = CoolColors.White;
            GizmosPlus.DrawPoint(Target, 1f);

            Debug3D.Label(Target, "Flee Target");
        }
    }
}
