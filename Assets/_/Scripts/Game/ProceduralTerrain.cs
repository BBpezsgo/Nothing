using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    [SerializeField] Terrain Terrain;
    [SerializeField] TerrainData Data;

    [SerializeField, Min(0f)] float NoiseScale;
    [SerializeField, Min(0f)] float HeightScale;

    [SerializeField, Button(nameof(Generate), false, true)] string ButtonGenerate;

    void Awake()
    {
        Terrain = GetComponent<Terrain>();
    }

    void Start()
    {
        Data = new TerrainData();
        Terrain.terrainData = Data;
        Generate();
    }

    void Generate()
    {
        var heights = new float[33, 33];
        var w = heights.GetLength(0);
        var h = heights.GetLength(1);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                heights[x, y] = Mathf.PerlinNoise(x * NoiseScale, y * NoiseScale) * HeightScale;
            }
        }
        Data.SetHeights(0, 0, heights);
    }
}
