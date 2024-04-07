using System.Collections;
using UnityEngine;

#nullable enable

public class GlobalCoroutines : MonoBehaviour
{
    static GameObject? _instance;

    static GameObject GetInstance()
    {
        if (_instance == null)
        {
            _instance = new GameObject("GlobalCoroutines", typeof(GlobalCoroutines));
            GameObject.DontDestroyOnLoad(_instance);
        }
        return _instance;
    }

    public static Coroutine Run(IEnumerator coroutine)
    {
        GameObject instance = GetInstance();
        GlobalCoroutines monoBehavior = instance.GetComponent<GlobalCoroutines>();
        return monoBehavior.StartCoroutine(coroutine);
    }
}
