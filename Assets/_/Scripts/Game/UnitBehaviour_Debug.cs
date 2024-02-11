using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Debug : UnitBehaviour_Base
    {
        [SerializeField] public Vector2 Input;

        public override Vector2? GetOutput() => Input;
    }
}
