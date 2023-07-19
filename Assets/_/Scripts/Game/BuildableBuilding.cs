using Game.Managers;

using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public class BuildableBuilding : NetworkBehaviour
    {
        [SerializeField, ReadOnly] GameObject Building;
        [SerializeField, ReadOnly] uint BuildingHash;

        [SerializeField, ReadOnly] float BuildingProcessRequied = 1f;
        [SerializeField, ReadOnly] float BuildingProcess = 0f;
        NetworkVariable<float> NetBuildingProcess = new(0f);
        [SerializeField] ParticleSystem Particles;
        [SerializeField, Min(1)] float ParticlesAmmount = 1f;
        ParticleSystem.EmissionModule ParticlesEmission;
        [SerializeField, ReadOnly, NonReorderable] Material[] materials;
        internal string Team;

        void OnEnable()
        { RegisteredObjects.BuildableBuildings.Add(this); }
        void OnDisable()
        { RegisteredObjects.BuildableBuildings.Remove(this); }

        internal void Init(uint buildingHash, float processRequied)
        {
            BuildingHash = buildingHash;
            BuildingProcessRequied = processRequied;

            PlayerData playerData = PlayerData.GetPlayerData(Team);
            var buildings = (playerData == null) ? new PlayerData.ConstructableBuilding[0] : playerData.ConstructableBuildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].Hash != buildingHash) continue;

                Building = buildings[i].Building;
                return;
            }
            Debug.LogError($"[{nameof(BuildingManager)}]: Building \"{buildingHash}\" not found", this);
        }

        void Start()
        {
            List<Renderer> renderers = new();
            renderers.AddRange(GetComponentsInChildren<Renderer>());

            for (int i = renderers.Count - 1; i >= 0; i--)
            {
                if (renderers[i] is ParticleSystemRenderer)
                { renderers.RemoveAt(i); }
            }

            materials = new Material[renderers.Count];
            for (int i = 0; i < materials.Length; i++)
            { materials[i] = renderers[i].material; }

            if (Particles != null) ParticlesEmission = Particles.emission;

            NetBuildingProcess.OnValueChanged = OnBuildingProcessChanged;
        }

        void OnBuildingProcessChanged(float previousValue, float newValue)
        {
            if (NetcodeUtils.IsServer || !NetcodeUtils.IsClient) return;
            Build(previousValue - newValue);
            BuildingProcess = newValue;
        }

        internal void Build(float progress)
        {
            if (Particles != null)
            {
                Particles.Emit(Mathf.RoundToInt(ParticlesAmmount * progress));
            }

            BuildingProcess += progress;

            for (int i = 0; i < materials.Length; i++)
            { materials[i].SetFloat("_Progress", BuildingProcess); }

            if (NetcodeUtils.IsServer)
            {
                NetBuildingProcess.Value = BuildingProcess;

                if (BuildingProcess >= BuildingProcessRequied)
                {
                    if (Building != null)
                    {
                        GameObject instance = GameObject.Instantiate(Building, transform.position, transform.rotation, ObjectGroups.Game);
                        instance.SpawnOverNetwork();

                        if (instance.TryGetComponent(out BaseObject baseObject))
                        { baseObject.Team = Team; }
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(BuildingHologram)}]: Building is null");
                    }
                    GameObject.Destroy(gameObject);
                }
            }
        }

        internal void SetMaterial(Material material)
        {
            List<Renderer> renderers = new();
            renderers.AddRange(GetComponentsInChildren<Renderer>());

            for (int i = renderers.Count - 1; i >= 0; i--)
            {
                if (renderers[i] is ParticleSystemRenderer)
                { renderers.RemoveAt(i); }
            }

            materials = new Material[renderers.Count];
            for (int i = 0; i < materials.Length; i++)
            {
                renderers[i].materials = new Material[] { Material.Instantiate(material) };
                materials[i] = renderers[i].material;
            }
        }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref BuildingHash);
            serializer.SerializeValue(ref BuildingProcessRequied);
            serializer.SerializeValue(ref Team);

            if (serializer.IsReader)
            {
                PlayerData playerData = PlayerData.GetPlayerData(Team);
                var buildings = (playerData == null) ? new PlayerData.ConstructableBuilding[0] : playerData.ConstructableBuildings.ToArray();
                for (int i = 0; i < buildings.Length; i++)
                {
                    if (buildings[i].Hash != BuildingHash) continue;

                    BuildingManager.ApplyBuildableHologram(this, this.Team, buildings[i]);
                    return;
                }
                Debug.LogError($"[{nameof(BuildingManager)}]: Building \"{BuildingHash}\" not found", this);
            }
        }
    }
}
