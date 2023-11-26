using System.Collections.Generic;
using AssetManager;

using Game.Managers;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public class BaseObject : NetworkBehaviour
    {
        [Header("Health")]
        [SerializeField] internal float HP;
        [SerializeField, ReadOnly] float MaxHP;

        internal float NormalizedHP => HP / MaxHP;

        [Header("Team")]
        [SerializeField] internal string Team;
        [SerializeField, ReadOnly] internal int TeamHash = -1;
        [SerializeField] protected Renderer[] teamRenderers = new Renderer[0];

        protected virtual void Awake()
        {
            MaxHP = HP == 0f ? 1f : HP;
        }

        protected void UpdateTeam()
        {
            if (teamRenderers == null) return;
            TeamManager.Team team = TeamManager.Instance.GetTeam(Team);
            if (team == null)
            {
                if (!string.IsNullOrWhiteSpace(Team))
                { Debug.LogWarning($"[{nameof(BaseObject)}]: Team \"{Team}\" not found", this); }
                
                UpdateColor(Color.white);
                return;
            }
            TeamHash = team.Hash;
            UpdateColor(team.Color);
        }
        protected void UpdateColor(Color color)
        {
            for (int i = 0; i < teamRenderers.Length; i++)
            {
                if (teamRenderers[i] != null &&
                    teamRenderers[i].material != null) teamRenderers[i].material.color = color;
            }
        }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref Team);
            serializer.SerializeValue(ref TeamHash);
            UpdateTeam();
        }

        protected void TryDropLoot()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (TryGetComponent(out ItemLoot itemLoot))
            { itemLoot.DropLoots(); }
        }

        internal void CollectTeamRenderers()
        {
            TeamRenderer[] teamRenderers = gameObject.GetComponentsInChildren<TeamRenderer>(false);
            List<MeshRenderer> renderers = new();
            for (int i = 0; i < teamRenderers.Length; i++)
            { renderers.AddRange(teamRenderers[i].Renderers ?? new MeshRenderer[0]); }
            this.teamRenderers = renderers.ToArray();
        }

        internal bool Repair(float v)
        {
            HP = Maths.Min(MaxHP, HP + v);
            return HP >= MaxHP;
        }
    }
}
