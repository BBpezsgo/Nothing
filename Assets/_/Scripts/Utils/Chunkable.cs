using UnityEngine;

public class Chunkable : MonoBehaviour
{
    /*
    [SerializeField, ReadOnly] Vector3 LastPosition;
    [SerializeField, ReadOnly] float NextRefresh = 2f;

    void Start()
    {
        LastPosition = transform.position;
        Chunks.Instance.Add(this);
    }

    void OnDisable()
    {
        Chunks.Instance.Remove(this);
    }

    void FixedUpdate()
    {
        if (NextRefresh > 0f)
        {
            NextRefresh -= Time.fixedDeltaTime;
            return;
        }
        NextRefresh = 5f;

        Chunks.Instance.Refresh(this, LastPosition);
        LastPosition = transform.position;
    }
    */
}
