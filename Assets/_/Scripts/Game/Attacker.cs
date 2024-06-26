using System.Collections.Generic;
using Game.Managers;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    internal class Attacker : AttackerBase
    {
        [Header("Targeting")]
        BaseObject[] targets = new BaseObject[5];
        List<BaseObject> priorityTargets = new();
        [SerializeField, Min(0f)] float DetectionRadius = 300f;

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

        protected virtual void Start()
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

            AI.SortTargets(targets, transform.position, this.TeamHash);
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

        protected override void Update()
        {
            base.Update();

            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            if (BaseObject is ICanTakeControl canTakeControl && canTakeControl.AnybodyControllingThis())
            { return; }

            if (turret != null) turret.LoseTarget();

            /*
            if (NewTargetCooldown > 0f)
            {
                NewTargetCooldown -= Time.deltaTime;
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
                turret.SetTarget(default(Vector3));
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

            bool firstFound = turret.HasNoTarget;

            turret.SetTarget(target.transform);
            turret.PrepareShooting = true;

            if (!firstFound &&
                turret.IsAccurateShoot &&
                !turret.OutOfRange &&
                NetcodeUtils.IsOfflineOrServer &&
                Vector3.Distance(turret.ShootPosition, target.transform.position) <= turret.GetRange())
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
                AI.SortTargets(priorityTargets, transform.position, this.TeamHash);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);

            if (targets != null)
            {
                Gizmos.color = Maths.CoolColors.Blue;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null) continue;
                    GizmosPlus.DrawPoint(targets[i].transform.position, 1f);
                    Debug3D.Label(targets[i].transform.position, "Target");
                }
            }

            if (priorityTargets != null)
            {
                Gizmos.color = Maths.CoolColors.Blue;
                for (int i = 0; i < priorityTargets.Count; i++)
                {
                    if (priorityTargets[i] == null) continue;
                    Gizmos.DrawWireSphere(priorityTargets[i].transform.position, 1f);
                    Debug3D.Label(priorityTargets[i].transform.position, "Priority Target");
                }
            }
        }
    }
}
