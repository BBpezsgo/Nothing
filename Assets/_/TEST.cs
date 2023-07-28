using UnityEngine;

public class TEST : MonoBehaviour
{
    [SerializeField] float TimeScale = 1f;
    [SerializeField, Button(nameof(FixedUpdate), false, true, "Refresh")] string btnRefresh;

    void FixedUpdate()
    {
        Time.timeScale = TimeScale;
    }
}
