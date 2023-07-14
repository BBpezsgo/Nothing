using UnityEngine;

public class WaterManager : MonoBehaviour
{
    static WaterManager instance;

    [SerializeField] float waterLevel;

    public static float WaterLevel => instance.waterLevel;

    void Awake()
    {
        if (instance != null)
        {
            Debug.Log($"[{nameof(WaterManager)}]: Instance already registered");
            Destroy(this);
            return;
        }
        instance = this;
    }
}
