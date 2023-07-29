using UnityEngine;

namespace Game.Components
{
    public class UnitDestroyer : MonoBehaviour
    {
        Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (transform.position.y < -100)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.x < -5000)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.x > 5000)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.y < -5000)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (transform.position.y > 5000)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            if (rb == null)
            { return; }

            if (rb.velocity.sqrMagnitude > 150 * 150)
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
