using AssetManager;

using UnityEngine;

internal class Building : BaseObject, IDamagable
{
    [SerializeField] GameObject DestroyEffect;
    [SerializeField, AssetField] internal float HP;
    [SerializeField] public Vector3 GroundOrigin;

    internal float NormalizedHP => HP / _maxHp;

    float _maxHp;

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

    void Start()
    {
        UpdateTeam();

        _maxHp = HP == 0f ? 1f : HP;
    }

    public void Damage(float ammount)
    {
        HP -= ammount;
        if (HP <= 0f)
        {
            Destroy();
        }
    }

    void Destroy()
    {
        if (this.IsOfflineOrServer())
        { GameObject.Destroy(gameObject); }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(GroundOrigin - Vector3.right, GroundOrigin + Vector3.right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(GroundOrigin - Vector3.up, GroundOrigin + Vector3.up);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(GroundOrigin - Vector3.forward, GroundOrigin + Vector3.forward);
    }
}
