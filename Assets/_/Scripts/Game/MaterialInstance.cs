using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MaterialInstance : MonoBehaviour
{
    [SerializeField] Material Material;
    [SerializeField, ReadOnly] Material Instance;

    void Start()
    {
        Instance = Material.Instantiate(Material);

        if (TryGetComponent(out DecalProjector decal))
        { decal.material = Instance; }
    }

    void OnDestroy()
    {
        Material.Destroy(Instance);
    }
}
