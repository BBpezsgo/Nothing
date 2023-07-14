public class OnEnabled : UnityEngine.MonoBehaviour
{
    internal delegate void OnEnableEvent();
    internal event OnEnableEvent onEnable;
    void OnEnable() => onEnable?.Invoke();
}

public static class OnEnableExtension
{
    public static OnEnabled OnEnabled(this UnityEngine.GameObject self) => self == null ? null : self.GetComponent<OnEnabled>();
    public static OnEnabled OnEnabled(this UnityEngine.Component self) => self == null ? null : self.GetComponent<OnEnabled>();
}