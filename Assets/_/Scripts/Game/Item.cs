using UnityEngine;

namespace Game.Components
{
    public class Item : MonoBehaviour
    {
        [SerializeField] internal string ItemID;
        [field: SerializeField, ReadOnly] internal bool IsPickedUp { get; private set; }

        internal void PickUp(Transform holder)
        {
            IsPickedUp = true;

            if (TryGetComponent(out Rigidbody itemRigidbody))
            { itemRigidbody.isKinematic = true; }

            if (TryGetComponent(out Collider itemCollider))
            { itemCollider.enabled = false; }

            transform.SetParent(holder);
            transform.SetLocalPositionAndRotation(default, Quaternion.identity);
        }

        internal void Drop()
        {
            IsPickedUp = false;

            if (TryGetComponent(out Rigidbody itemRigidbody))
            { itemRigidbody.isKinematic = false; }

            if (TryGetComponent(out Collider itemCollider))
            { itemCollider.enabled = true; }

            transform.SetParent(ObjectGroups.Items);
        }

        internal void Drop(Vector3 velocity)
        {
            IsPickedUp = false;

            if (TryGetComponent(out Rigidbody itemRigidbody))
            {
                itemRigidbody.isKinematic = false;
                itemRigidbody.velocity = velocity;
            }

            if (TryGetComponent(out Collider itemCollider))
            { itemCollider.enabled = true; }

            transform.SetParent(ObjectGroups.Items);
        }
    }
}
