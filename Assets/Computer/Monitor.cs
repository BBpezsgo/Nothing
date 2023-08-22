using Game;
using UnityEngine;

namespace InGameComputer
{
    public class Monitor : MonoBehaviour
    {
        [SerializeField, ReadOnly] BoxCollider Collider;
        [SerializeField, Min(0f)] float RayMaxDistance;

        /// <summary>
        /// In local normalized screen space
        /// </summary>
        public Vector2 CapturedMousePosition { get; private set; }
        public bool IsMouseOnScreen { get; private set; }

        void Start()
        {
            Collider = GetComponent<BoxCollider>();
        }

        public void CaptureMousePosition()
        {
            RaycastHit[] hits = Physics.RaycastAll(MainCamera.Camera.ScreenPointToRay(Input.mousePosition), RayMaxDistance, Utilities.DefaultLayerMasks.JustDefault, QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != Collider) continue;

                Vector2 point = TransformPoint(hit.point);
                if (!point.IsUnitVector()) break;

                CapturedMousePosition = point;
                IsMouseOnScreen = true;
                return;
            }

            IsMouseOnScreen = false;
        }

        Vector2 TransformPoint(Vector3 worldPosition)
        {
            Vector2 position = (Vector2)transform.InverseTransformPoint(worldPosition);
            Vector2 size = (Vector2)Collider.size;
            Vector2 center = (Vector2)Collider.center;

            position -= center;

            position += size / 2;

            position /= size;

            position = Vector2.one - position;

            return position;
        }
    }
}
