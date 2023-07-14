using UnityEngine;

public class TEST : MonoBehaviour
{
    [SerializeField] float TimeScale = 1f;

    void FixedUpdate()
    {
        Time.timeScale = TimeScale;
    }
}
