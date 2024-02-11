using UnityEngine;

namespace Game.Components
{
    public class FracturedObjectsRoot : MonoBehaviour
    {
        void Update()
        {
            if (transform.childCount == 0)
            { Destroy(gameObject); }
        }
    }
}
