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
            { Rigidbody.Destroy(itemRigidbody); }

            transform.SetParent(holder);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
