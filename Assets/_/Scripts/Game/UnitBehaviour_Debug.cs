using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Debug : UnitBehaviour_Base
    {
        [SerializeField] internal Vector2 Input;

        internal override Vector2? GetOutput() => Input;
    }
}
