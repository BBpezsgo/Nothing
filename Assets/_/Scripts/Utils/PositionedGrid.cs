using System;

using UnityEngine;

internal class PositionedGrid<T> : Grid<T>
{
    readonly Vector3 origin;

    internal PositionedGrid(int width, int height, int cellSize, Vector3 origin)
        : base(width, height, cellSize)
        => this.origin = origin;

    internal PositionedGrid(int width, int height, int cellSize, Vector3 origin, Func<int, int, T> constructor)
        : base(width, height, cellSize, constructor)
        => this.origin = origin;

    internal override Vector3 GetWorldPosition(int x, int y)
        => new Vector3(x, 0f, y) * this.CellSize + this.origin;
    internal override ValueTuple<int, int> GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - origin.x) / CellSize);
        int y = Mathf.FloorToInt((worldPosition.z - origin.z) / CellSize);

        x = Mathf.Clamp(x, 0, width);
        y = Mathf.Clamp(y, 0, height);

        return (x, y);
    }
}
