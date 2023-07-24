using AssetManager;

using Game.Managers;

using System.Collections.Generic;

using UnityEngine;

namespace Game.Components
{
    internal class Attacker : AttackerBase, IHaveAssetFields
    {
        [Header("Targeting")]
        [SerializeField, ReadOnly, NonReorderable] BaseObject[] targets = new BaseObject[5];
        [SerializeField, ReadOnly] int PriorityTargetCount = 0;
        [SerializeField, ReadOnly, NonReorderable] List<BaseObject> priorityTargets = new();

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
        [SerializeField, ReadOnly] float NewTargetCooldown = 1f;

        protected virtual void Awake()
        {
            NewTargetCooldown = Random.value + 1f;
        }

        void FindTargets()
        {
            if (TeamHash == -1) return;
            BaseObject[] result = new BaseObject[5];
            int j = 0;
            for (int i = RegisteredObjects.Units.Count - 1; i >= 0; i--)
            {
                if (j >= result.Length) break;
                if (RegisteredObjects.Units[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(this.TeamHash, RegisteredObjects.Units[i].TeamHash) > 0f)
                { result[j++] = RegisteredObjects.Units[i]; }
            }
            for (int i = RegisteredObjects.Buildings.Count - 1; i >= 0; i--)
            {
                if (j >= result.Length) break;
                if (RegisteredObjects.Buildings[i] == null) continue;
                if (TeamManager.Instance.GetFuckYou(this.TeamHash, RegisteredObjects.Buildings[i].TeamHash) > 0f)
                { result[j++] = RegisteredObjects.Buildings[i]; }
            }
            targets = Utilities.AI.SortTargets(result, transform.position, this.TeamHash);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            if (BaseObject is ICanTakeControl canTakeControl && canTakeControl.AnybodyControllingThis())
            { return; }

            if (turret != null) turret.LoseTarget();

            if (NewTargetCooldown > 0f)
            {
                NewTargetCooldown -= Time.fixedDeltaTime;
            }
            else
            {
                NewTargetCooldown = 1f;

                if (NeedNewTargets)
                { FindTargets(); }
            }

            for (int j = priorityTargets.Count - 1; j >= 0; j--)
            {
                if (priorityTargets[j] == null)
                {
                    priorityTargets.RemoveAt(j);
                    PriorityTargetCount = priorityTargets.Count;
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
                PriorityTargetCount = priorityTargets.Count;
            }
        }

        void OnDrawGizmosSelected()
        {
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
