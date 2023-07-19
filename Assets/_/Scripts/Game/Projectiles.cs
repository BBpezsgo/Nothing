using System;
using UnityEngine;

[CreateAssetMenu]
public class Projectiles : ScriptableObject
{
	[Serializable]
	public struct Projectile
	{
		public string Name;
		public Texture2D Image;

		public GameObject Prefab;
	}

	[SerializeField] Projectile[] projectiles;

    public int Length => projectiles.Length;

    public Projectile this[int index]
    {
        get => projectiles[index];
        set => projectiles[index] = value;
    }

    public Projectile this[Index index]
    {
        get => projectiles[index];
    }
}
