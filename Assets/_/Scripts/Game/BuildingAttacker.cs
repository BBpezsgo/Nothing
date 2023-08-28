using AssetManager;

using Game.Managers;

using UnityEngine;

namespace Game.Components
{
    internal class BuildingAttacker : Building, IDetailedDamagable, ICanTakeControlAndHasTurret
    {
        public Turret Turret => Attacker.turret;
        [SerializeField, ReadOnly] ulong controllingByUser;
        public ulong ControllingByUser
        {
            get => controllingByUser;
            set => controllingByUser = value;
        }

        [SerializeField, ReadOnly] AttackerBase Attacker;

        [field: SerializeField] public TakeControlManager.CrossStyle CrossStyle { get; set; }
        [field: SerializeField] public TakeControlManager.ReloadIndicatorStyle ReloadIndicatorStyle { get; set; }

        public virtual void DoInput() => Attacker.DoInput();
        public virtual void DoFrequentInput() => Attacker.DoFrequentInput();

        void Awake()
        {
            this.ControllingByUser = ulong.MaxValue;
        }

        protected override void Start()
        {
            base.Start();

            if (!TryGetComponent(out Attacker))
            { Debug.LogError($"[{nameof(BuildingAttacker)}]: {nameof(Attacker)} is null", this); }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (this.IAmControllingThis() && gameObject.scene.isLoaded)
            { TakeControlManager.Instance.ControllableDestroyed(); }
        }

        public void Damage(float ammount, Projectile source)
        {
            if (ammount > .0001f &&
                source != null &&
                source.Owner != null &&
                TeamManager.Instance.GetFuckYou(this, source.Owner) >= 0f &&
                Attacker is Attacker attacker)
            {
                attacker.SomeoneDamagedMe(source.Owner);
            }

            Damage(ammount);
        }
    }
}
