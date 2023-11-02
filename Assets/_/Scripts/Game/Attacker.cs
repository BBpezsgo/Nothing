using System.Collections.Generic;
using AssetManager;
using Game.Managers;
using UnityEngine;

namespace Game.Components
{
    internal class Attacker : AttackerBase, IHaveAssetFields
    {
        [Header("Targeting")]
        BaseObject[] targets = new BaseObject[5];
        List<BaseObject> priorityTargets = new();
        [SerializeField, AssetField, Min(0f)] float DetectionRadius = 300f;

        bool NeedNewTargets
        {
            get
            {
                if (priorityTargets.Count != 0) return false;
                for (int i = 0; i < targets.Length; i++)
                { if (targets[i] != null) return false; }
                return true;
            }
        }

        protected virtual void Awake()
        {
            this.Interval(TryFindTargets, 1f, TargetCondition);
        }

        void Reset()
        {
            targets = new BaseObject[5];
            priorityTargets = new();
            DetectionRadius = 300f;
        }

        void FindTargets()
        {
            if (TeamHash == -1) return;

            int j = 0;

            List<Unit> allUnit = RegisteredObjects.Units;
            List<Building> allBuilding = RegisteredObjects.Buildings;

            for (int i = allUnit.Count - 1; i >= 0; i--)
            {
                if (j >= targets.Length) break;
                if (allUnit[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(TeamHash, allUnit[i].TeamHash) <= 0f)
                { continue; }
                if (Vector3.Distance(transform.position, allUnit[i].transform.position) > DetectionRadius)
                { continue; }

                targets[j++] = allUnit[i];
            }

            for (int i = allBuilding.Count - 1; i >= 0; i--)
            {
                if (j >= targets.Length) break;
                if (allBuilding[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(TeamHash, allBuilding[i].TeamHash) <= 0f)
                { continue; }
                if (Vector3.Distance(transform.position, allBuilding[i].transform.position) > DetectionRadius)
                { continue; }

                targets[j++] = allBuilding[i];
            }

            Utilities.AI.SortTargets(targets, transform.position, this.TeamHash);
        }

        void TryFindTargets()
        {
            if (NeedNewTargets)
            { FindTargets(); }
        }

        bool TargetCondition() =>
            NetcodeUtils.IsOfflineOrServer && (
                BaseObject is not ICanTakeControl canTakeControl ||
                !canTakeControl.AnybodyControllingThis()
            );

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            if (BaseObject is ICanTakeControl canTakeControl && canTakeControl.AnybodyControllingThis())
            { return; }

            if (turret != null) turret.LoseTarget();

            /*
            if (NewTargetCooldown > 0f)
            {
                NewTargetCooldown -= Time.fixedDeltaTime;
            }
            else
            {
                NewTargetCooldown = 1f;
                TryFindTargets();
            }
            */

            for (int j = priorityTargets.Count - 1; j >= 0; j--)
            {
                if (priorityTargets[j] == null)
                {
                    priorityTargets.RemoveAt(j);
                    continue;
                }
                if (ThinkOnTarget(priorityTargets[j])) return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                if (ThinkOnTarget(targets[i])) return;
            }

            if (turret != null)
            {
                turret.SetTarget(Vector3.zero);
                turret.PrepareShooting = false;
            }
        }

        bool ThinkOnTarget(BaseObject target)
        {
            if (target.TryGetComponent(out RequiredShoots requiredShoots))
            {
                if (requiredShoots.EstimatedHP < 0f)
                { return false; }
            }

            bool first = false;
            if (turret.HasNoTarget)
            { first = true; }

            turret.SetTarget(target.transform);
            turret.PrepareShooting = true;

            if (!first && turret.IsAccurateShoot && NetcodeUtils.IsOfflineOrServer)
            {
                if (target.TryGetComponent(out requiredShoots))
                { turret.Shoot(requiredShoots); }
                else
                { turret.Shoot(); }
            }

            return true;
        }

        internal void SomeoneDamagedMe(BaseObject source)
        {
            if (BaseObject == null) return;
            bool alreadyAdded = false;
            for (int i = priorityTargets.Count - 1; i >= 0; i--)
            {
                if (priorityTargets[i] == source)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            if (!alreadyAdded)
            {
                priorityTargets.Add(source);
                Utilities.AI.SortTargets(priorityTargets, transform.position, this.TeamHash);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);

            if (targets != null)
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null) continue;
                    Gizmos.DrawWireSphere(targets[i].transform.position, 1f);
                }
            }

            if (priorityTargets != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < priorityTargets.Count; i++)
                {
                    if (priorityTargets[i] == null) continue;
                    Gizmos.DrawWireSphere(priorityTargets[i].transform.position, 1f);
                }
            }
        }
    }
}
