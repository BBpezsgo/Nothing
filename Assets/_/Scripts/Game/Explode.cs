using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class Explode : MonoBehaviour
    {
        [SerializeField, Min(0f)] float ExplodeForce;
        [Tooltip("Mass/Volume")]
        [SerializeField, ReadOnly] float Density;
        [SerializeField, Range(0f, 1f)] float FragmentSmokeProbability;
        [SerializeField, MinMax(0f, 2f)] Vector2 FragmentSmokeScale;
        [SerializeField] GameObject FragmentSmokePrefab;

        [SerializeField] AudioClip[] AudioClips;

        [SerializeField, Button(nameof(Do), false, true, "Explode")] string ButtonExplode;

        void Reset()
        {
            ExplodeForce = .2f;
            AudioClips = new AudioClip[0];
        }

        float CalculateDensity()
        {
            if (!TryGetComponent(out Rigidbody rigidbody)) return default;

            float mass = rigidbody.mass;
            float volume = Maths.TotalMeshVolume(gameObject, true);

            return mass / volume;
        }

        void Start()
        {
            Density = CalculateDensity();
        }

        public void Do()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(false);
            for (int i = 0; i < meshFilters.Length; i++)
            { Generate(meshFilters[i], $"{gameObject.name} Fracture {i}", meshFilters.Length); }
        }

        GameObject Generate(MeshFilter mesh, string name, int count)
        {
            GameObject obj = new(
                name,
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(BoxCollider),
                typeof(Rigidbody));

            obj.transform.SetParent(ObjectGroups.Effects, false);
            obj.transform.localScale = mesh.transform.lossyScale;
            obj.transform.SetPositionAndRotation(mesh.transform.position, mesh.transform.rotation);

            if (mesh.TryGetComponent(out SimpleMaterial selfSimpleMaterial))
            {
                SimpleMaterial simpleMaterial = obj.AddComponent<SimpleMaterial>();
                simpleMaterial.CopyFrom(selfSimpleMaterial);
            }

            if (obj.TryGetComponent(out MeshFilter meshFilter))
            {
                meshFilter.mesh = mesh.mesh;
            }
            else
            { Debug.LogWarning($"Failed to add component {nameof(MeshFilter)} to {obj}", this); }

            BoxCollider boxCollider = null;

            if (obj.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.materials = mesh.GetComponent<MeshRenderer>().materials;

                if (obj.TryGetComponent(out boxCollider))
                {
                    boxCollider.center = meshRenderer.localBounds.center;
                    boxCollider.size = meshRenderer.localBounds.size;
                }
                else
                { Debug.LogWarning($"Failed to add component {nameof(BoxCollider)} to {obj}", this); }
            }
            else
            { Debug.LogWarning($"Failed to add component {nameof(MeshRenderer)} to {obj}", this); }

            Rigidbody selfRigidbody = null;

            if (obj.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.useGravity = true;

                if (TryGetComponent(out selfRigidbody))
                {
                    rigidbody.velocity = selfRigidbody.velocity;
                    rigidbody.angularDrag = selfRigidbody.angularDrag;
                    rigidbody.drag = selfRigidbody.drag;
                }
            }
            else
            { Debug.LogWarning($"Failed to add component {nameof(Rigidbody)} to {obj}", this); }

            float selfMass = (selfRigidbody != null) ? selfRigidbody.mass : 0f;

            if (mesh.TryGetComponent(out DinoFracture.RuntimeFracturedGeometry selfFracture) &&
                QualityHandler.EnableModelFragmentation)
            {
                DinoFracture.RuntimeFracturedGeometry fracture = obj.AddComponent<DinoFracture.RuntimeFracturedGeometry>();
                fracture.CopyFrom(selfFracture);
                Vector3 position = transform.position;
                fracture.Fracture().OnFractureComplete += (e) =>
                {
                    if (mesh != null) Destroy(mesh);
                    if (obj != null) Destroy(obj);
                    int childCount = e.FracturePiecesRootObject.transform.childCount;
                    e.FracturePiecesRootObject.AddComponent<FracturedObjectsRoot>();
                    for (int i = 0; i < childCount; i++)
                    {
                        GameObject child = e.FracturePiecesRootObject.transform.GetChild(i).gameObject;

                        HandleFracture(child);

                        if (child.TryGetComponent(out BoxCollider childBoxCollider) &&
                            child.TryGetComponent(out MeshRenderer childMeshRenderer))
                        {
                            childBoxCollider.center = childMeshRenderer.localBounds.center;
                            childBoxCollider.size = childMeshRenderer.localBounds.size;
                        }

                        if (child.TryGetComponent(out Rigidbody childRigidbody))
                        {
                            if (ExplodeForce > float.Epsilon)
                            { childRigidbody.AddExplosionForce(ExplodeForce, position, 0f, 5f); }

                            float childVolume = Maths.Volume(child.GetComponent<MeshFilter>(), childBoxCollider);

                            if (Density > float.Epsilon && childVolume > float.Epsilon)
                            { childRigidbody.mass = childVolume * Density; }
                            else if (selfMass > float.Epsilon)
                            { childRigidbody.mass = selfMass / childCount; }
                        }
                    }
                };
            }
            else
            {
                HandleFracture(obj);

                if (ExplodeForce > float.Epsilon && rigidbody != null)
                { rigidbody.AddExplosionForce(ExplodeForce, transform.position, 20f, 5f); }

                float childVolume = Maths.Volume(meshFilter, boxCollider);

                if (Density > float.Epsilon && childVolume > float.Epsilon)
                { rigidbody.mass = childVolume * Density; }
                else if (selfMass > float.Epsilon)
                { rigidbody.mass = selfMass / count; }
            }

            return obj;
        }

        void HandleFracture(GameObject fracture)
        {
            fracture.layer = LayerMask.GetMask(LayerMaskNames.IgnoreRaycast);

            if (AudioClips.Length > 0)
            {
                AudioSource childAudio = fracture.AddComponent<AudioSource>();
                childAudio.maxDistance = 10f;
            }

            FracturedObjectScript fracturedObject = fracture.AddComponent<FracturedObjectScript>();
            fracturedObject.LifeTime = Random.Range(20f, 40f);
            fracturedObject.AudioClips = AudioClips;

            if (FragmentSmokePrefab != null &&
                Random.value >= FragmentSmokeProbability &&
                QualityHandler.EnableParticles)
            {
                GameObject smoke = Instantiate(FragmentSmokePrefab, fracture.transform);
                smoke.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
                smoke.transform.localScale = Vector3.one * Random.Range(FragmentSmokeScale.x, FragmentSmokeScale.y);
            }
        }
    }
}
