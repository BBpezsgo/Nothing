using UnityEngine;

public class EffectLight : MonoBehaviour
{
    [SerializeField, ReadOnly] Light Light;

    [Header("Settings")]
    [SerializeField] float MaxAge;

    [SerializeField] bool UseCurve;
    [SerializeField] bool Reverse;
    [SerializeField] AnimationCurve Curve;

    [SerializeField, ReadOnly] float Lifetime;

    float LifetimePercent => Maths.Clamp01(Lifetime / MaxAge);

    [SerializeField, ReadOnly] float OriginalIntensity;

    void Start()
    {
        if (!TryGetComponent(out Light))
        {
            Debug.LogWarning($"[{nameof(EffectLight)}]: {nameof(Light)} is null", gameObject);
            EffectLight.Destroy(this);
        }

        if (MaxAge <= float.Epsilon)
        {
            Debug.LogWarning($"[{nameof(EffectLight)}]: {nameof(MaxAge)} is zero", gameObject);
            EffectLight.Destroy(this);
        }

        OriginalIntensity = Light.intensity;
    }

    void Update()
    {
        Lifetime += Time.deltaTime;

        float percent = LifetimePercent;
        if (Reverse)
        { percent = 1 - percent; }

        if (UseCurve)
        { Light.intensity = OriginalIntensity * Curve.Evaluate(percent); }
        else
        { Light.intensity = OriginalIntensity * percent; }

        if (Lifetime >= MaxAge)
        {
            Light.enabled = false;
            this.enabled = false;
        }
    }
}
