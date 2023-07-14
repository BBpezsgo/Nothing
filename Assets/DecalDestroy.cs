using UnityEngine;

public class DecalDestroy : MonoBehaviour
{
    // [SerializeField, ReadOnly] UnityEngine.Rendering.Universal.DecalProjector Decal;

    [SerializeField] float LifeTime;
    // float LifeTimeStart;

    // void Start()
    // {
        // if (!TryGetComponent(out Decal))
        // { Decal = GetComponentInChildren<UnityEngine.Rendering.Universal.DecalProjector>(); }
        // if (Decal == null) Debug.LogWarning($"[{nameof(DecalDestroy)}]: {nameof(UnityEngine.Rendering.Universal.DecalProjector)} not found");

        // LifeTimeStart = LifeTime;
    // }

    void FixedUpdate()
    {
        LifeTime -= Time.fixedDeltaTime;
        if (LifeTime <= 0f)
        { GameObject.Destroy(gameObject); }
        // else
        // { Decal.fadeFactor = Mathf.Clamp01(LifeTime / LifeTimeStart); }
    }
}
