using AssetManager;

using UnityEngine;

namespace Game.Components
{
    internal class Building : BaseObject, IDamagable
    {
        [SerializeField] GameObject DestroyEffect;
        [SerializeField, AssetField] internal float HP;
        [SerializeField] public Vector3 GroundOrigin;

        internal float NormalizedHP => HP / _maxHp;

        float _maxHp;

        [Header("Debug")]
        [SerializeField, Button(nameof(DebugDestroy), false, true, "Destroy")] string buttonDestroy;

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && DestroyEffect != null)
            { GameObject.Instantiate(DestroyEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            base.OnDestroy();
        }

        void OnEnable()
        { RegisteredObjects.Buildings.Add(this); }
        void OnDisable()
        { RegisteredObjects.Buildings.Remove(this); }

        protected virtual void Start()
        {
            UpdateTeam();

            _maxHp = HP == 0f ? 1f : HP;
        }

        public void Damage(float ammount)
        {
            HP -= ammount;

            if (HP <= 0f)
            { Destroy(); }
        }

        void Destroy()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            base.TryDropLoot();
            this.OnUnitDestroy();
            GameObject.Destroy(gameObject);
        }

        protected virtual void OnUnitDestroy() { }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(GroundOrigin - Vector3.right, GroundOrigin + Vector3.right);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(GroundOrigin - Vector3.up, GroundOrigin + Vector3.up);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(GroundOrigin - Vector3.forward, GroundOrigin + Vector3.forward);
        }

        internal bool Repair(float v)
        {
            HP = Mathf.Min(_maxHp, HP + v);
            return HP >= _maxHp;
        }

        void DebugDestroy() => Destroy();
    }
}
