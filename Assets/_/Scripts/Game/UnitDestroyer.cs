using UnityEngine;

namespace Game.Components
{
    public class UnitDestroyer : MonoBehaviour
    {
        const int HorizontalThreshold = 5000;
        const int VerticalThreshold = 100;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (transform.position.y < -VerticalThreshold)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.x < -HorizontalThreshold)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.x > HorizontalThreshold)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.z < -HorizontalThreshold)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.z > HorizontalThreshold)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (rb == null)
            { return; }

            if (rb.linearVelocity.sqrMagnitude > 150 * 150)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (rb.angularVelocity.sqrMagnitude > 720 * 720)
            {
                GameObject.Destroy(gameObject);
                return;
            }
        }
    }
}
