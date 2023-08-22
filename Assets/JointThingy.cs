using UnityEngine;

public class JointThingy : MonoBehaviour
{
    [SerializeField] public Transform Target;
    [SerializeField, ReadOnly] Rigidbody Rigidbody;
    [SerializeField, ReadOnly] Vector3 Offset;

    void Start() => Refresh();

    public void Refresh()
    {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody == null)
        { this.enabled = false; }

        Offset = transform.position - Target.position;
    }

    void FixedUpdate()
    {
        if (Target == null)
        {
            this.enabled = false;
            return;
        }

        Vector3 offset = Rigidbody.position - Target.position;
        float distance = offset.magnitude;
        if (distance < .2f)
        {
            Vector3 up = Target.up;
            Rigidbody.MovePosition(Vector3.Lerp(Rigidbody.position, Target.position + (new Vector3(Offset.x * up.x, Offset.y * up.y, Offset.z * up.z)), .5f));
            Rigidbody.MoveRotation(Quaternion.Lerp(Rigidbody.rotation, Target.rotation, .5f));
        }
    }
}
