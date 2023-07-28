using UnityEngine;

namespace Utilities.Components
{
    public class DecalDestroy : MonoBehaviour
    {
        [SerializeField] float LifeTime;

        void FixedUpdate()
        {
            if (LifeTime <= 0f)
            { return; }

            LifeTime -= Time.fixedDeltaTime;

            if (LifeTime >= 0f)
            { return; }

            GameObject.Destroy(gameObject);
            LifeTime = 0f;
        }
    }
}
