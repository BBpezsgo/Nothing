using UnityEngine;

public class CameraLockable : MonoBehaviour
{
    public int Priority = 0;
    public bool FreeMode = true;
    public bool CanZoom = true;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public readonly struct Priorities
    {
        public const int ControllableThing = 1;
        public const int ComputerView = 2;
    }
}
