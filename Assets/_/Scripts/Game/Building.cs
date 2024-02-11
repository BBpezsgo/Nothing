using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class Building : BaseObject, IDamageable
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
            GizmosPlus.DrawAxes(GroundOrigin);
        }

        void DebugDestroy() => Destroy();
    }
}
