using System;

using UnityEngine;

namespace Grid
{
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
            int x = Maths.FloorToInt((worldPosition.x - origin.x) / CellSize);
            int y = Maths.FloorToInt((worldPosition.z - origin.z) / CellSize);

            x = Maths.Clamp(x, 0, width);
            y = Maths.Clamp(y, 0, height);

            return (x, y);
        }
    }
}
