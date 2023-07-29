using AssetManager;

using Game.Managers;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    internal class Unit : BaseObject, IDamagable, ISelectable, ICanTakeControlAndHasTurret
    {
        [SerializeField, ReadOnly] Vector3 destination;
        [SerializeField, AssetField] MovementEngine vehicleEngine;
        [SerializeField, AssetField] internal Turret turret;

        public Turret Turret => turret;
        [SerializeField, ReadOnly] ulong controllingByUser;
        public ulong ControllingByUser
        {
            get => controllingByUser;
            set => controllingByUser = value;
        }

        [SerializeField, AssetField] internal GameObject UiSelected;

        [SerializeField, ReadOnly] internal UnitBehaviour UnitBehaviour;

        [SerializeField] internal GameObject DestroyEffect;
        [SerializeField, AssetField] internal float HP;
        float _maxHp;

        internal float NormalizedHP => HP / _maxHp;

        ISelectable.State selectableState = ISelectable.State.None;
        public ISelectable.State SelectableState
        {
            get => selectableState;
            set
            {
                if (this == null) return;
                if (UiSelected == null) return;
                selectableState = value;
                if (selectableState == ISelectable.State.None)
                { UiSelected.SetActive(false); }
                else
                {
                    UiSelected.SetActive(true);
                    if (selectableState == ISelectable.State.Almost)
                    { UiSelected.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>().material.SetEmissionColor(SelectionManager.Instance.AlmostSelectedColor, 1f); }
                    else if (selectableState == ISelectable.State.Selected)
                    { UiSelected.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>().material.SetEmissionColor(SelectionManager.Instance.SelectedColor, 1f); }
                }
            }
        }

        void Awake()
        {
            this.ControllingByUser = ulong.MaxValue;

            if (turret != null) turret.@base = this;

            _maxHp = HP == 0f ? 1f : HP;
        }

        void Start()
        {
            if (!TryGetComponent(out UnitBehaviour))
            { Debug.LogWarning($"[{nameof(Unit)}]: {nameof(UnitBehaviour)} is null", this); }
            if (!TryGetComponent(out vehicleEngine))
            { Debug.LogWarning($"[{nameof(Unit)}]: No VehicleEngine", this); }
            UpdateTeam();

            _maxHp = HP == 0f ? 1f : HP;
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
                var hits = Physics.RaycastAll(ray, 500f, DefaultLayerMasks.Targeting).Exclude(transform);
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

                GameObject.Destroy(gameObject);
            }
        }
    }
}
