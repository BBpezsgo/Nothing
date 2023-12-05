using UnityEngine;

public class FracturedObjectScript : MonoBehaviour
{
    [ReadOnly] public float LifeTime;
    [ReadOnly] public bool Do;

    void FixedUpdate()
    {
        if (!Do) return;

        LifeTime -= Time.fixedDeltaTime;

        if (LifeTime >= 0f) return;
        if (LifeTime <= 2f) Destroy(gameObject);

        float lifeTime = (LifeTime + 2f) / 2f;
        transform.localScale = new Vector3(lifeTime, lifeTime, lifeTime);
    }
}
