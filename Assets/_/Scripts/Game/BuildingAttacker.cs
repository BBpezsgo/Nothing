using Game.Managers;
using UnityEngine;

namespace Game.Components
{
    internal class BuildingAttacker : Building, IDetailedDamageable, ICanTakeControlAndHasTurret
    {
        public Turret Turret => Attacker.Turret;

        [SerializeField, ReadOnly] AttackerBase Attacker;

        [field: SerializeField] public TakeControlManager.CrossStyle CrossStyle { get; set; }
        [field: SerializeField] public TakeControlManager.ReloadIndicatorStyle ReloadIndicatorStyle { get; set; }
        public System.Action<(Vector3 Position, float Amount, DamageKind Kind)[]> OnDamagedSomebody { get; set; }

        public virtual void DoInput() => Attacker.DoInput();
        public virtual void DoFrequentInput() => Attacker.DoFrequentInput();

        protected virtual void Awake()
        {
            if (!TryGetComponent(out Attacker))
            { Debug.LogError($"[{nameof(BuildingAttacker)}]: {nameof(Attacker)} is null", this); }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (this.IAmControllingThis() && gameObject.scene.isLoaded)
            { TakeControlManager.Instance.ControllableDestroyed(); }
        }

        public void Damage(float amount, Projectile source)
        {
            if (amount > .0001f &&
                source != null &&
                source.Owner != null &&
                TeamManager.Instance.GetFuckYou(this, source.Owner) >= 0f &&
                Attacker is Attacker attacker)
            {
                attacker.SomeoneDamagedMe(source.Owner);
            }

            Damage(amount);
        }
    }
}
