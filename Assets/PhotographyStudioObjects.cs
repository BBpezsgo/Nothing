using System;
using UnityEngine;

namespace Game.Managers
{
    [Serializable]
    internal class PhotographyStudioObject
    {
        [SerializeField] internal string Name;
        [SerializeField] internal GameObject Object;
    }

    [CreateAssetMenu(fileName = "Photography Studio Objects", menuName = "Photography Studio Objects")]
    public class PhotographyStudioObjects : ScriptableObject
    {
        [SerializeField] internal PhotographyStudioObject[] Objects;
    }
}
