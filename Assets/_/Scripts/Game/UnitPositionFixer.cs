using UnityEngine;

namespace Game.Components
{
    public class UnitPositionFixer : MonoBehaviour
    {
        [SerializeField, ReadOnly] float GoodPosition = 0f;
        [SerializeField, ReadOnly] Rigidbody Rigidbody;

        void Start()
        {
            if (TryGetComponent<Collider>(out var collider))
            {
                GoodPosition = collider.bounds.min.y;
            }

            Rigidbody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            // float position = transform.position.y;
            // if (position < GoodPosition)
            {
                if (Rigidbody != null)
                {
                    Rigidbody.position = new Vector3(Rigidbody.position.x, GoodPosition, Rigidbody.position.z);
                }
                transform.position = new Vector3(transform.position.x, GoodPosition, transform.position.z);

            }
        }
    }
}
