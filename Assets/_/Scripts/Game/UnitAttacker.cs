using Game.Managers;

using UnityEngine;

namespace Game.Components
{
    internal class UnitAttacker : Unit, IDetailedDamagable
    {
        [Header("Other")]
        [SerializeField, ReadOnly] AttackerBase Attacker;

        public override void DoInput()
        {
            base.DoInput();
            Attacker.DoInput();
        }

        public override void DoFrequentInput()
        {
            base.DoFrequentInput();
            Attacker.DoFrequentInput();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (TryGetComponent(out UnitBehaviour_Seek seek))
            {
                if (!turret.HasNoTarget && Maths.Distance(turret.TargetPosition, turret.ShootPosition) >= turret.Range)
                { seek.Target = turret.TargetPosition; }
                else
                { seek.Target = default; }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (!TryGetComponent(out Attacker))
            { Debug.LogError($"[{nameof(BuildingAttacker)}]: No Attacker!"); }
        }

        public void Damage(float ammount, Projectile source)
        {
            if (ammount > .0001f &&
                source != null &&
                source.Owner != null &&
                TeamManager.Instance.GetFuckYou(this, source.Owner) >= 0f &&
                Attacker is Attacker attacker)
            { attacker.SomeoneDamagedMe(source.Owner); }

            Damage(ammount);
        }
    }
}
