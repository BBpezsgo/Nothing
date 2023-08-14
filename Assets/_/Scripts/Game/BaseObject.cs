using System.Collections.Generic;
using AssetManager;

using DataUtilities.Serializer;

using Game.Managers;

using Networking;
using Networking.Messages;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public class BaseObject : NetworkBehaviour, IHaveAssetFields, INetworkObservable
    {
        [Header("Team")]
        [SerializeField] internal string Team;
        [SerializeField, ReadOnly] internal int TeamHash = -1;
        [SerializeField, AssetField] protected Renderer[] teamRenderers = new Renderer[0];

        protected void UpdateTeam()
        {
            if (teamRenderers == null) return;
            TeamManager.Team team = TeamManager.Instance.GetTeam(Team);
            if (team == null)
            {
                Debug.LogWarning($"[{nameof(BaseObject)}]: Team \"{Team}\" not found", this);
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

        void INetworkObservable.OnRPC(RpcHeader header)
        {

        }

        void INetworkObservable.OnSerializeView(Deserializer deserializer, Serializer serializer, NetcodeMessageInfo messageInfo)
        {
            if (messageInfo.IsReading)
            {
                Team = deserializer.DeserializeString();
                UpdateTeam();
            }
            else if (messageInfo.IsWriting)
            {
                serializer.Serialize(Team);
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
    }
}
