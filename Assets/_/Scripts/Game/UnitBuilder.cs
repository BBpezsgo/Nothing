using AssetManager;

using System.Collections.Generic;

using UnityEngine;

namespace Game.Components
{
    internal class UnitBuilder : Unit
    {
        [SerializeField, ReadOnly, NonReorderable] BuildableBuilding[] ToBeBuilt = new BuildableBuilding[0];
        [SerializeField, ReadOnly, NonReorderable] Building[] ToBeRepair = new Building[0];

        [SerializeField, ReadOnly] int nearestToBeBuilt = -1;
        [SerializeField, ReadOnly] int nearestToBeRepair = -1;

        [SerializeField, AssetField] float DistanceToBuild = 1f;
        [SerializeField, AssetField] float ConstructionSpeed = 1f;

        [SerializeField, ReadOnly] float TimeToNextTargetSearch = 1f;

        void FindToBeBuilt()
        {
            if (string.IsNullOrEmpty(Team)) return;
            List<BuildableBuilding> result = new();
            for (int i = RegisteredObjects.BuildableBuildings.Count - 1; i >= 0; i--)
            {
                if (RegisteredObjects.BuildableBuildings == null)
                { continue; }
                if (this.Team != RegisteredObjects.BuildableBuildings[i].Team)
                { continue; }

                result.Add(RegisteredObjects.BuildableBuildings[i]);
            }
            ToBeBuilt = result.ToArray();
        }

        void FindToBeRepair()
        {
            if (string.IsNullOrEmpty(Team)) return;
            List<Building> result = new();
            for (int i = RegisteredObjects.Buildings.Count - 1; i >= 0; i--)
            {
                if (RegisteredObjects.Buildings == null)
                { continue; }
                if (this.Team != RegisteredObjects.Buildings[i].Team)
                { continue; }
                if (RegisteredObjects.Buildings[i].NormalizedHP >= 1f)
                { continue; }

                result.Add(RegisteredObjects.Buildings[i]);
            }
            ToBeRepair = result.ToArray();
        }

        int NearestToBeBuilt() => ToBeBuilt.ClosestI(transform.position).Item1;
        int NearestToBeRepair() => ToBeRepair.ClosestI(transform.position).Item1;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.AnybodyControllingThis()) return;

            if (nearestToBeBuilt != -1 && ToBeBuilt[nearestToBeBuilt] != null)
            {
                DoBuild();
            }
            else if (nearestToBeRepair != -1 && ToBeRepair[nearestToBeRepair] != null)
            {
                DoRepair();
            }
            else
            {
                DoIdle();
            }
        }

        void DoBuild()
        {
            if (nearestToBeBuilt < 0 || nearestToBeBuilt >= ToBeRepair.Length)
            { return; }
            if (ToBeBuilt[nearestToBeBuilt] == null)
            { return; }

            if (turret != null)
            {
                turret.SetTarget(ToBeBuilt[nearestToBeBuilt].transform);
            }

            if ((ToBeBuilt[nearestToBeBuilt].transform.position - transform.position).To2D().sqrMagnitude <= (DistanceToBuild * DistanceToBuild))
            {
                ToBeBuilt[nearestToBeBuilt].Build(ConstructionSpeed * Time.fixedDeltaTime);

                if (TryGetComponent(out UnitBehaviour_Seek seek))
                { seek.Target = Vector3.zero; }
            }
            else
            {
                if (TryGetComponent(out UnitBehaviour_Seek seek))
                { seek.Target = ToBeBuilt[nearestToBeBuilt].transform.position; }
            }

            if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
            { avoidObstacles.IgnoreCollision = ToBeBuilt[nearestToBeBuilt].transform; }
        }

        void DoRepair()
        {
            if (nearestToBeRepair < 0 || nearestToBeRepair >= ToBeRepair.Length)
            { return; }
            if (ToBeRepair[nearestToBeRepair] == null)
            { return; }

            if (turret != null)
            {
                turret.SetTarget(ToBeRepair[nearestToBeRepair].transform);
            }

            if ((ToBeRepair[nearestToBeRepair].transform.position - transform.position).To2D().sqrMagnitude <= (DistanceToBuild * DistanceToBuild))
            {
                ToBeRepair[nearestToBeRepair].Repair(ConstructionSpeed * Time.fixedDeltaTime);
                if (ToBeRepair[nearestToBeRepair].NormalizedHP >= 1f)
                { ToBeRepair[nearestToBeRepair] = null; }

                if (TryGetComponent(out UnitBehaviour_Seek seek))
                { seek.Target = Vector3.zero; }
            }
            else
            {
                if (TryGetComponent(out UnitBehaviour_Seek seek))
                { seek.Target = ToBeRepair[nearestToBeRepair].transform.position; }
            }

            if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
            { avoidObstacles.IgnoreCollision = ToBeRepair[nearestToBeRepair].transform; }
        }

        void DoIdle()
        {
            if (TimeToNextTargetSearch > 0)
            {
                TimeToNextTargetSearch -= Time.fixedDeltaTime;
            }
            else
            {
                if (ToBeBuilt.Length == 0 && ToBeRepair.Length == 0)
                {
                    FindToBeBuilt();
                    FindToBeRepair();
                }

                if (ToBeBuilt.Length != 0)
                { ToBeBuilt = ToBeBuilt.PurgeObjects(); }

                if (ToBeRepair.Length != 0)
                { ToBeRepair = ToBeRepair.PurgeObjects(); }

                nearestToBeBuilt = NearestToBeBuilt();
                nearestToBeRepair = NearestToBeRepair();

                TimeToNextTargetSearch = 2f;
            }

            if (turret != null) turret.SetTarget(Vector3.zero);

            if (TryGetComponent(out UnitBehaviour_Seek seek))
            { seek.Target = Vector3.zero; }

            if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
            { avoidObstacles.IgnoreCollision = null; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, DistanceToBuild);
        }
    }
}
