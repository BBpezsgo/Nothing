using AssetManager;

using System.Collections.Generic;

using UnityEngine;

internal class UnitBuilder : Unit
{
    [SerializeField, ReadOnly, NonReorderable] BuildableBuilding[] targets = new BuildableBuilding[0];
    [SerializeField, ReadOnly] int nearestTargetIndex = -1;
    [SerializeField, AssetField] float DistanceToBuild = 1f;
    [SerializeField, AssetField] float ConstructionSpeed = 1f;

    [SerializeField, ReadOnly] float TimeToNextTargetSearch = 1f;

    void FindTargets()
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
        targets = result.ToArray();
    }

    int NearestTarget() => targets.ClosestI(transform.position).Item1;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (this.AnybodyControllingThis()) return;

        if (nearestTargetIndex != -1 && targets[nearestTargetIndex] != null)
        {
            if (turret != null)
            {
                turret.target = targets[nearestTargetIndex].transform.position;
                turret.targetTransform = targets[nearestTargetIndex].transform;
            }
            if ((targets[nearestTargetIndex].transform.position - transform.position).To2D().sqrMagnitude <= (DistanceToBuild * DistanceToBuild))
            {
                targets[nearestTargetIndex].Build(ConstructionSpeed * Time.fixedDeltaTime);
            }

            if (TryGetComponent<UnitBehaviour_Seek>(out var seek))
            { seek.Target = targets[nearestTargetIndex].transform.position; }
        }
        else
        {
            if (TimeToNextTargetSearch > 0)
            {
                TimeToNextTargetSearch -= Time.fixedDeltaTime;
            }
            else
            {
                if (targets.Length == 0)
                {
                    FindTargets();
                }

                if (targets.Length != 0)
                {
                    targets = targets.PurgeObjects();
                }

                nearestTargetIndex = NearestTarget();

                TimeToNextTargetSearch = 2f;
            }

            if (turret != null) turret.target = Vector3.zero;

            if (TryGetComponent<UnitBehaviour_Seek>(out var seek))
            { seek.Target = Vector3.zero; }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, DistanceToBuild);
    }
}

