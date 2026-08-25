using UnityEngine;

public class FlowFieldCell
{
    public int X { get; }
    public int Z { get; }

    public Vector3 WorldPosition { get; }

    public float Height { get; }
    public Vector3 GroundNormal { get; }

    public bool Walkable { get; }

    public int Cost { get; set; }

    public Vector3 Direction { get; set; }

    public FlowFieldCell(
        int x,
        int z,
        Vector3 worldPosition,
        float height,
        Vector3 groundNormal,
        bool walkable)
    {
        X = x;
        Z = z;

        WorldPosition = worldPosition;

        Height = height;
        GroundNormal = groundNormal;

        Walkable = walkable;

        Cost = int.MaxValue;
        Direction = Vector3.zero;
    }
}