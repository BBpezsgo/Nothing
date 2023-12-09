using UnityEngine;

namespace Game.Components
{
    public class Building : BaseObject, IDamagable
    {
        [SerializeField] GameObject DestroyEffect;
        [SerializeField] public Vector3 GroundOrigin;

        [Header("Debug")]
        [SerializeField, Button(nameof(DebugDestroy), false, true, "Destroy")] string buttonDestroy;

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && DestroyEffect != null && QualityHandler.EnableParticles)
            { GameObject.Instantiate(DestroyEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            base.OnDestroy();
        }

        void OnEnable()
        { RegisteredObjects.Buildings.Add(this); }
        void OnDisable()
        { RegisteredObjects.Buildings.Remove(this); }

        protected override void Start()
        {
            UpdateTeam();
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
            if (TryGetComponent(out Explode explode))
            { explode.Do(); }
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

        void DebugDestroy() => Destroy();
    }
}
