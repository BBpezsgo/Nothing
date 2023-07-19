using UnityEngine;

namespace Game.Components
{
    public class CollideEffects : MonoBehaviour
    {
        [SerializeField] GameObject EffectPrefab;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer != gameObject.layer)
            { return; }

            ContactPoint contact = collision.GetContact(0);
            GameObject.Instantiate(EffectPrefab, contact.point, Quaternion.LookRotation(contact.normal), ObjectGroups.Effects);
        }
    }
}
