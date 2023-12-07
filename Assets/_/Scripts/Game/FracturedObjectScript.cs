using UnityEngine;

public class FracturedObjectScript : MonoBehaviour
{
    [ReadOnly] public float LifeTime;

    void FixedUpdate()
    {
        LifeTime -= Time.fixedDeltaTime;

        if (LifeTime >= 0f) return;
        if (LifeTime <= 2f) { Destroy(gameObject); return; }

        float lifeTime = (LifeTime + 2f) / 2f;
        transform.localScale = new Vector3(lifeTime, lifeTime, lifeTime);
    }
}
