using System;

using UnityEngine;

internal class Grid<T>
{
    protected readonly int width;
    protected readonly int height;
    protected readonly int cellSize;
    protected readonly T[,] GridArray;

    internal int Width => width;
    internal int Height => height;
    internal int CellSize => cellSize;

    internal T this[int x, int y]
    {
        get => GridArray[x, y];
        set => GridArray[x, y] = value;
    }

    internal T this[Vector3 worldPosition]
    {
        get => Get(GetGridPosition(worldPosition));
        set => Set(GetGridPosition(worldPosition), value);
    }

    internal T this[Vector2Int position]
    {
        get => Get(position);
        set => Set(position, value);
    }

    internal Grid(int width, int height, int cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        GridArray = new T[width, height];
    }

    internal Grid(int width, int height, int cellSize, Func<int, int, T> constructor)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        this.GridArray = new T[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                this.GridArray[x, y] = constructor.Invoke(x, y);
            }
        }
    }

    internal virtual Vector3 GetWorldPosition(int x, int y) => new Vector3(x, 0f, y) * this.CellSize;
    internal virtual ValueTuple<int, int> GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / CellSize);
        int y = Mathf.FloorToInt(worldPosition.z / CellSize);

        x = Mathf.Clamp(x, 0, width);
        y = Mathf.Clamp(y, 0, height);

        return (x, y);
    }

    internal T Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return default;
        return this.GridArray[x, y];
    }
    internal T Get(ValueTuple<int, int> position) => Get(position.Item1, position.Item2);
    internal T Get(Vector2Int position) => Get(position.x, position.y);
    internal T Get(Vector3 worldPosition) => Get(GetGridPosition(worldPosition));

    internal void Set(int x, int y, T v)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        this.GridArray[x, y] = v;
    }
    internal void Set(ValueTuple<int, int> position, T v) => Set(position.Item1, position.Item2, v);
    internal void Set(Vector2Int position, T v) => Set(position.x, position.y, v);
    internal void Set(Vector3 worldPosition, T v) => Set(GetGridPosition(worldPosition), v);
}

internal static class GridExtensions
{
    internal static void DebugDraw<T>(this Grid<T> grid, Vector3 origin, Color color)
    {
        if (grid == null) return;

        Color savedGuiColor = GUI.color;
        Color savedGizmosColor = Gizmos.color;
        Gizmos.color = color;
        GUI.color = color;

        Vector3 halfCell = Vector3.one * (grid.CellSize / 2f);

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Gizmos.DrawWireCube(grid.GetWorldPosition(x, y) + origin + halfCell, Vector3.one * grid.CellSize);
                Utilities.Debug3D.Label(grid.GetWorldPosition(x, y) + origin + halfCell, grid[x, y].ToString());
            }
        }

        GUI.color = savedGuiColor;
        Gizmos.color = savedGizmosColor;
    }

    internal static void DebugDraw<T>(this Grid<T> grid, Color color)
        => DebugDraw(grid, Vector3.zero, color);
}
