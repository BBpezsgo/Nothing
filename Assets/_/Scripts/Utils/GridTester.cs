using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTester : MonoBehaviour
{
    [SerializeField] int Width;
    [SerializeField] int Height;
    [SerializeField] int CellSize;
    Grid<int> Grid;

    [SerializeField, Button(nameof(GenerateGrid), true, true, "Generate")] string btn_0;

    void Start() => GenerateGrid();

    void GenerateGrid()
    {
        Grid = new Grid<int>(Width, Height, CellSize);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition);
            Grid[mousePosition]++;
        }
    }

    void OnDrawGizmos()
    {
        Grid?.DebugDraw(transform.position, Color.white);
    }
}
