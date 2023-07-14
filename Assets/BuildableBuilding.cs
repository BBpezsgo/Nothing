using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

public class BuildableBuilding : MonoBehaviour
{
    [SerializeField] internal GameObject Building;
    [SerializeField] internal float BuildingProcessRequied = 1f;
    [SerializeField] internal float BuildingProcess = 0f;
    [SerializeField] ParticleSystem Particles;
    [SerializeField, Min(1)] internal float ParticlesAmmount = 1f;
    ParticleSystem.EmissionModule ParticlesEmission;
    [SerializeField, ReadOnly, NonReorderable] Material[] materials;
    internal string Team;

    void OnEnable()
    { RegisteredObjects.BuildableBuildings.Add(this); }
    void OnDisable()
    { RegisteredObjects.BuildableBuildings.Remove(this); }

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
    }

    internal void Build(float progress)
    {
        if (Particles != null)
        {
            Particles.Emit(Mathf.RoundToInt(ParticlesAmmount * progress));
        }

        BuildingProcess += progress;

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
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        { materials[i].SetFloat("_Progress", BuildingProcess); }
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
}
