using AssetManager;

using Game.Managers;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    internal class Unit : BaseObject, IDamagable, ICanTakeControlAndHasTurret
    {
        [SerializeField, AssetField] MovementEngine vehicleEngine;
        [SerializeField, AssetField] protected Turret turret;

        public Turret Turret
        {
            get => turret;
            set => turret = value;
        }

        [SerializeField, ReadOnly] internal UnitBehaviour UnitBehaviour;

        [SerializeField] internal GameObject DestroyEffect;

        [field: SerializeField] public TakeControlManager.CrossStyle CrossStyle { get; set; }
        [field: SerializeField] public TakeControlManager.ReloadIndicatorStyle ReloadIndicatorStyle { get; set; }

        [Header("Debug")]
        [SerializeField, Button(nameof(DebugDestroy), false, true, "Destroy")] string buttonDestroy;

        protected override void Awake()
        {
            base.Awake();
            if (turret != null) turret.@base = this;
        }

        void Start()
        {
            if (!TryGetComponent(out UnitBehaviour))
            { Debug.LogWarning($"[{nameof(Unit)}]: {nameof(UnitBehaviour)} is null", this); }
            if (!TryGetComponent(out vehicleEngine))
            { Debug.LogWarning($"[{nameof(Unit)}]: No VehicleEngine", this); }
            UpdateTeam();
        }

        void OnEnable()
        { RegisteredObjects.Units.Add(this); }
        void OnDisable()
        { RegisteredObjects.Units.Remove(this); }

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && DestroyEffect != null)
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
                var ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
                var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Solids).Exclude(transform);
                Vector3 point = hits.Length == 0 ? ray.GetPoint(500f) : hits.Closest(transform.position).point;

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

            if (Input.GetMouseButton(MouseButton.Left))
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

        protected virtual void FixedUpdate()
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
            if (UnitBehaviour == null) return Vector2.zero;
            return UnitBehaviour.GetOutput();
        }

        public void Damage(float ammount)
        {
            HP -= ammount;

            if (HP <= 0f)
            { Destroy(); }
        }

        void Destroy()
        {
            if (NetcodeUtils.IsOfflineOrServer)
            {
                base.TryDropLoot();
                this.OnUnitDestroy();
                GameObject.Destroy(gameObject);
            }
        }

        protected virtual void OnUnitDestroy() { }

        void DebugDestroy() => Destroy();
    }
}
