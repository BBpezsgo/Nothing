using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBehaviour_Debug : UnitBehaviour_Base
{

    [SerializeField] internal Vector2 Input;

    internal override Vector2? GetOutput() => Input;
}
