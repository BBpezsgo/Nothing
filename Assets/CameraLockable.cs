using UnityEngine;

public class CameraLockable : MonoBehaviour
{
    public int Priority = 0;
    public bool FreeMode = true;
    public bool Zoomable = true;

    public Vector3 position => transform.position;
    public Quaternion rotation => transform.rotation;

    public readonly struct Priorities
    {
        public const int ControllableThing = 1;
        public const int ComputerView = 2;
    }
}
