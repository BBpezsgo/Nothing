using UnityEngine;

namespace Game.Components
{
    public class FracturedObjectsRoot : MonoBehaviour
    {
        void FixedUpdate()
        {
            if (transform.childCount == 0)
            { Destroy(gameObject); }
        }
    }
}
