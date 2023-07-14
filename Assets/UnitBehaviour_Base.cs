using AssetManager;

using System;

using Unity.Netcode;

using UnityEngine;

[RequireComponent(typeof(UnitBehaviour))]
public class UnitBehaviour_Base : NetworkBehaviour, IComparable<UnitBehaviour_Base>, IHaveAssetFields
{
    [SerializeField, AssetField] internal float Priority;
    [SerializeField, ReadOnly] protected VehicleEngine VehicleEngine;

    protected virtual void Start()
    {
        if (!TryGetComponent(out VehicleEngine))
        { Debug.LogWarning($"[{nameof(UnitBehaviour_Base)}]: {nameof(VehicleEngine)} is null", this); }
    }

    public int CompareTo(UnitBehaviour_Base other) => (-Priority).CompareTo(-other.Priority);

    /// <summary>
    /// <b>Steering (X):</b> <br/>
    /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code> <br/>
    /// <b>Acceleration (Y):</b> <br/>
    /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code> <br/>
    /// </summary>
    internal virtual Vector2? GetOutput()
        => null;
}
