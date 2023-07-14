using AssetManager;

using DataUtilities.Serializer;

using Messages;

using Unity.Netcode;

using UnityEngine;

public class BaseObject : NetworkBehaviour, IHaveAssetFields, INetworkObservable
{
    [Header("Team")]
    [SerializeField, AssetField] internal string Team;
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
        string lastTeam = Team;
        serializer.SerializeValue(ref lastTeam, true);
        if (lastTeam != Team)
        { UpdateTeam(); }
    }
}
