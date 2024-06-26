using System;
using System.Collections.Generic;
using Game.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class BuildableBuilding : NetworkBehaviour
    {
        [SerializeField, ReadOnly] GameObject Building;
        [SerializeField, ReadOnly] uint BuildingHash;
        [SerializeField, ReadOnly] bool IsConstructed;

        [SerializeField, ReadOnly] float BuildingProcessRequied = 1f;
        [SerializeField, ReadOnly] float BuildingProcess = 0f;
        readonly NetworkVariable<float> NetBuildingProcess = new();
        [SerializeField] ParticleSystem Particles;
        [SerializeField, Min(1)] float ParticlesAmmount = 1f;
        ParticleSystem.EmissionModule ParticlesEmission;
        [SerializeField, ReadOnly, NonReorderable] Material[] materials;
        [SerializeField, ReadOnly] public string Team;

        void OnEnable()
        { RegisteredObjects.BuildableBuildings.Add(this); }
        void OnDisable()
        { RegisteredObjects.BuildableBuildings.Remove(this); }

        public void Init(uint buildingHash, float processRequied)
        {
            BuildingHash = buildingHash;
            BuildingProcessRequied = processRequied;

            PlayerData playerData = PlayerData.GetPlayerData(Team);
            var buildings = (playerData == null) ? new StaticPlayerData.ConstructableBuilding[0] : playerData.Data.ConstructableBuildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].Hash != buildingHash) continue;

                Building = buildings[i].Building;
                return;
            }
            Debug.LogError($"[{nameof(BuildingManager)}]: Building \"{buildingHash}\" not found", this);

            IsConstructed = false;
            BuildingProcess = 0f;
        }

        void Awake()
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

        public bool Build(float progress)
        {
            if (IsConstructed)
            { return true; }

            if (Particles != null && QualityHandler.EnableParticles)
            { Particles.Emit((int)MathF.Round(ParticlesAmmount * progress)); }

            BuildingProcess += progress;

            for (int i = 0; i < materials.Length; i++)
            { materials[i].SetFloat("_Progress", BuildingProcess / BuildingProcessRequied); }

            if (BuildingProcess >= BuildingProcessRequied)
            { IsConstructed = true; }

            if (NetcodeUtils.IsServer)
            { NetBuildingProcess.Value = BuildingProcess; }

            return BuildingProcess >= BuildingProcessRequied;
        }

        public void SetMaterial(Material material)
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
                var buildings = (playerData == null) ? new StaticPlayerData.ConstructableBuilding[0] : playerData.Data.ConstructableBuildings.ToArray();
                for (int i = 0; i < buildings.Length; i++)
                {
                    if (buildings[i].Hash != BuildingHash) continue;

                    BuildingManager.ApplyBuildableHologram(this, this.Team, buildings[i]);
                    return;
                }
                Debug.LogError($"[{nameof(BuildingManager)}]: Building \"{BuildingHash}\" not found", this);
            }
        }

        void Update()
        {
            if (!IsConstructed) return;

            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (BuildingProcess < BuildingProcessRequied) return;

            if (Building == null)
            { Debug.LogWarning($"[{nameof(BuildingHologram)}]: Building is null"); }
            else
            {
                GameObject instance = GameObject.Instantiate(Building, transform.position, transform.rotation, ObjectGroups.Game);
                instance.SpawnOverNetwork();

                if (instance.TryGetComponent(out BaseObject baseObject))
                { baseObject.Team = Team; }
            }

            GameObject.Destroy(gameObject);
        }
    }
}
