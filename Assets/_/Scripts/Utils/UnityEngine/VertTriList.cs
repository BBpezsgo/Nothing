using UnityEngine;

#nullable enable

/// <summary>
/// This is a list recording which triangles use each vertex. It is essentially
/// a jagged array with one top level entry per vertex. Each of the separate
/// sub-arrays is a list of all the triangles that use that vertex.
/// </summary>
public class VertTriList
{
    public int[][]? list;

    public int[] this[int index] => list![index];

    public VertTriList(Mesh mesh)
    {
        Init(mesh.triangles, mesh.vertexCount);
    }

    /// <summary>
    /// You don't usually need to call this - it's just to assist the implementation
    /// of the constructors.
    /// </summary>
    void Init(int[] tri, int numVerts)
    {
        // First, go through the triangles, keeping a count of how many times
        // each vert is used.
        int[] counts = new int[numVerts];

        for (int i = 0; i < tri.Length; i++)
        {
            counts[tri[i]]++;
        }

        // Initialize an empty jagged array with the appropriate number of elements
        // for each vert.
        list = new int[numVerts][];

        for (int i = 0; i < counts.Length; i++)
        {
            list[i] = new int[counts[i]];
        }

        // Assign the appropriate triangle number each time a given vert
        // is encountered in the triangles.
        for (int i = 0; i < tri.Length; i++)
        {
            int vert = tri[i];
            list[vert][--counts[vert]] = i / 3;
        }
    }
}
