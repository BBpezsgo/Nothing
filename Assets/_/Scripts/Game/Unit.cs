using Game.Managers;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    internal class Unit : BaseObject, IDamageable, ICanTakeControlAndHasTurret
    {
        [SerializeField] MovementEngine vehicleEngine;
        [SerializeField] protected Turret turret;

        public Turret Turret
        {
            get => turret;
            set => turret = value;
        }

        [SerializeField, ReadOnly] internal UnitBehaviour UnitBehavior;

        [SerializeField] internal GameObject DestroyEffect;

        [field: SerializeField] public TakeControlManager.CrossStyle CrossStyle { get; set; }
        [field: SerializeField] public TakeControlManager.ReloadIndicatorStyle ReloadIndicatorStyle { get; set; }
        public System.Action<(Vector3 Position, float Amount, DamageKind Kind)[]> OnDamagedSomebody { get; set; }

        [Header("Debug")]
        [SerializeField, Button(nameof(DebugDestroy), false, true, "Destroy")] string buttonDestroy;

        protected virtual void Awake()
        {
            if (!TryGetComponent(out UnitBehavior))
            { Debug.LogWarning($"[{nameof(Unit)}]: {nameof(UnitBehavior)} is null", this); }
            if (!TryGetComponent(out vehicleEngine))
            { Debug.LogWarning($"[{nameof(Unit)}]: No VehicleEngine", this); }
            if (turret != null) turret.@base = this;
        }

        protected override void Start()
        {
            base.Start();
            UpdateTeam();
        }

        void OnEnable()
        { RegisteredObjects.Units.Add(this); }
        void OnDisable()
        { RegisteredObjects.Units.Remove(this); }

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && DestroyEffect != null && QualityHandler.EnableParticles)
            { GameObject.Instantiate(DestroyEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            base.OnDestroy();

            if (this.IAmControllingThis() && gameObject.scene.isLoaded)
            { TakeControlManager.Instance.ControllableDestroyed(); }
        }

        public virtual void DoInput()
        {
            if (turret == null) return;

            if (!Input.GetKey(KeyCode.LeftAlt))
            {
                Ray ray = MainCamera.Camera.ScreenPointToRay(Mouse.LockedPosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).ExcludeTransforms(transform);
                Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits[hits.Closest(transform.position).Index].point;

                if (NetcodeUtils.IsOfflineOrServer)
                {
                    turret.SetTarget(point);
                }
                else if (NetcodeUtils.IsClient)
                {
                    if ((turret.TargetPosition - point).sqrMagnitude > .5f)
                    {
                        turret.TargetRequest(point);
                    }
                }
            }

            if (Input.GetMouseButton(Mouse.Left))
            {
                turret.PrepareShooting = true;

                if (turret.IsAccurateShoot)
                {
                    if (NetcodeUtils.IsOfflineOrServer)
                    {
                        turret.Shoot();
                    }
                    else if (NetcodeUtils.IsClient)
                    {
                        turret.ShootRequest();
                    }
                }
            }
            else
            {
                turret.PrepareShooting = false;
            }
        }
        public virtual void DoFrequentInput()
        { }

        protected virtual void Update()
        {
            if (vehicleEngine == null)
            { return; }

            if (this.IAmControllingThis())
            {
                if (MouseManager.MouseOnWindow)
                { vehicleEngine.DoUserInput(); }
                return;
            }

            if (this.AnybodyControllingThis())
            { return; }

            vehicleEngine.InputVector = GetInputVector();
        }

        Vector2 GetInputVector()
        {
            if (UnitBehavior == null) return default;
            return UnitBehavior.GetOutput();
        }

        public void Damage(float amount)
        {
            HP -= amount;

            if (HP <= 0f)
            { Destroy(); }
        }

        void Destroy()
        {
            if (NetcodeUtils.IsOfflineOrServer)
            {
                base.TryDropLoot();
                this.OnUnitDestroy();
                if (TryGetComponent(out Explode explode))
                { explode.Do(); }
                GameObject.Destroy(gameObject);
            }
        }

        protected virtual void OnUnitDestroy() { }

        void DebugDestroy() => Destroy();
    }
}
