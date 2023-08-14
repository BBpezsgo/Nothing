using UnityEngine;

namespace Game.Components
{
    public class UnitBehaviour_Debug : UnitBehaviour_Base, ICopiable<UnitBehaviour_Debug>
    {
        [SerializeField] internal Vector2 Input;

        internal override Vector2? GetOutput() => Input;

        public override void CopyTo(object destination) => this.CopyTo<UnitBehaviour_Debug>(destination);
        public void CopyTo(UnitBehaviour_Debug destination)
        {
            base.CopyTo(destination);
        }
    }
}
