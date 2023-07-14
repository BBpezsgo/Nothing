using System.Collections.Generic;

using UnityEngine;

internal class Chunks : SingleInstance<Chunks>
{
    Dictionary<uint, List<Chunkable>> Cells;
    Vector2 CellSize;

    protected override void Awake()
    {
        base.Awake();
        Cells = new Dictionary<uint, List<Chunkable>>();
        CellSize = Vector2.one;
    }

    void OnDrawGizmos()
    {
        if (Cells == null) return;
        Gizmos.color = Color.white;
        foreach (var cell in Cells)
        {
            var p = GetPosition(cell.Key);
            Gizmos.DrawWireCube(new Vector3(p.x * CellSize.x, 0, p.y * CellSize.y), new Vector3(CellSize.x, .1f, CellSize.y));
        }
    }

    public uint GetKey(Vector3 position)
        => GetKey(position.To2D());

    public uint GetKey(Vector2 position)
    {
        position *= CellSize;
        return (((uint)position.x) << 16) | (((uint)position.y) << 0);
    }

    public Vector2 GetPosition(uint key)
    {
        float x = (float)((key & 0xFF00) >> 16);
        float y = (float)((key & 0x00FF));
        x /= CellSize.x;
        y /= CellSize.y;
        return new Vector2(x, y);
    }

    public void Add(Chunkable obj)
    {
        uint key = GetKey(obj.transform.position.To2D());
        List<Chunkable> cell;
        if (Cells.TryGetValue(key, out cell))
        {
            if (cell == null)
            {
                Cells[key] = new List<Chunkable>();
                cell = Cells[key];
            }
        }
        else
        {
            cell = new List<Chunkable>();
            Cells.Add(key, cell);
        }

        if (cell.Contains(obj))
        {
            return;
        }

        cell.Add(obj);
    }

    public void Remove(Chunkable obj)
    {
        uint key = GetKey(obj.transform.position.To2D());
        if (!Cells.TryGetValue(key, out var cell))
        {
            Debug.LogError($"Failed to remove Chunkable from the chunks: key {key} not found");
            return;
        }
        cell.Remove(obj);
    }

    public void Refresh(Chunkable obj, Vector3 oldPosition)
    {
        uint oldKey = GetKey(oldPosition.To2D());
        uint newKey = GetKey(obj.transform.position.To2D());

        if (oldKey == newKey) return;

        if (!Cells.TryGetValue(oldKey, out var oldCell))
        {
            Debug.LogError($"Failed to remove Chunkable from the previous chunk: key {oldKey} not found");
            return;
        }

        oldCell.Remove(obj);

        Add(obj);
    }
}
