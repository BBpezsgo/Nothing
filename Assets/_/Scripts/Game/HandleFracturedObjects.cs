using DinoFracture;
using UnityEngine;

public class HandleFracturedObjects : MonoBehaviour
{
    public void OnFracture(OnFractureEventArgs e)
    {
        int childCount = e.FracturePiecesRootObject.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            GameObject child = e.FracturePiecesRootObject.transform.GetChild(i).gameObject;
            FracturedObjectScript fracturedObject = child.AddComponent<FracturedObjectScript>();
            fracturedObject.LifeTime = Random.Range(20f, 40f);
            fracturedObject.Do = true;
        }
    }
}
