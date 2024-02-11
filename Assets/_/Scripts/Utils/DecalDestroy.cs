using UnityEngine;

namespace Utilities.Components
{
    public class DecalDestroy : MonoBehaviour
    {
        [SerializeField] float LifeTime;

        void Update()
        {
            if (LifeTime <= 0f)
            { return; }

            LifeTime -= Time.deltaTime;

            if (LifeTime >= 0f)
            { return; }

            GameObject.Destroy(gameObject);
            LifeTime = 0f;
        }
    }
}
