using UnityEngine;

namespace Game.Components
{
    public class Explode : MonoBehaviour
    {
        [SerializeField, Min(0f)] float ExplodeForce;
        [Tooltip("Mass/Volume")]
        [SerializeField, Min(0f)] float Density;
        [SerializeField, Range(0f, 1f)] float FragmentSmokeProbability;
        [SerializeField, MinMax(0f, 2f)] Vector2 FragmentSmokeScale;
        [SerializeField] GameObject FragmentSmokePrefab;

        void Reset()
        {
            ExplodeForce = .2f;
            Density = .1f;
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

            if (obj.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.materials = mesh.GetComponent<MeshRenderer>().materials;
            }
            else
            { Debug.LogWarning($"Failed to add component {nameof(MeshRenderer)} to {obj}", this); }

            if (obj.TryGetComponent(out BoxCollider boxCollider))
            {
                boxCollider.size = mesh.GetComponent<MeshRenderer>().localBounds.size;
            }
            else
            { Debug.LogWarning($"Failed to add component {nameof(BoxCollider)} to {obj}", this); }

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

            if (mesh.TryGetComponent(out DinoFracture.RuntimeFracturedGeometry selfFracture))
            {
                DinoFracture.RuntimeFracturedGeometry fracture = obj.AddComponent<DinoFracture.RuntimeFracturedGeometry>();
                fracture.CopyFrom(selfFracture);
                Vector3 position = transform.position;
                fracture.Fracture().OnFractureComplete += (e) =>
                {
                    if (mesh != null) Destroy(mesh);
                    int childCount = e.FracturePiecesRootObject.transform.childCount;
                    e.FracturePiecesRootObject.AddComponent<FracturedObjectsRoot>();
                    for (int i = 0; i < childCount; i++)
                    {
                        GameObject child = e.FracturePiecesRootObject.transform.GetChild(i).gameObject;

                        if (child.TryGetComponent(out BoxCollider childBoxCollider) &&
                            child.TryGetComponent(out MeshRenderer childMeshRenderer))
                        {
                            childBoxCollider.size = childMeshRenderer.localBounds.size;
                            childBoxCollider.center = childMeshRenderer.localBounds.center;
                        }

                        FracturedObjectScript fracturedObject = child.AddComponent<FracturedObjectScript>();
                        fracturedObject.LifeTime = Random.Range(20f, 40f);
                        fracturedObject.Do = true;

                        if (FragmentSmokePrefab != null &&
                            Random.value >= FragmentSmokeProbability)
                        {
                            GameObject smoke = Instantiate(FragmentSmokePrefab, child.transform);
                            smoke.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                            smoke.transform.localScale = Vector3.one * Random.Range(FragmentSmokeScale.x, FragmentSmokeScale.y);
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
                FracturedObjectScript fracturedObject = obj.AddComponent<FracturedObjectScript>();
                fracturedObject.LifeTime = Random.Range(20f, 40f);
                fracturedObject.Do = true;

                if (ExplodeForce > float.Epsilon && rigidbody != null)
                { rigidbody.AddExplosionForce(ExplodeForce, transform.position, 20f, 5f); }

                if (FragmentSmokePrefab != null &&
                    Random.value >= FragmentSmokeProbability)
                {
                    GameObject smoke = Instantiate(FragmentSmokePrefab, mesh.transform);
                    smoke.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    smoke.transform.localScale = Vector3.one * Random.Range(FragmentSmokeScale.x, FragmentSmokeScale.y);
                }

                float childVolume = Maths.Volume(meshFilter, boxCollider);

                if (Density > float.Epsilon && childVolume > float.Epsilon)
                { rigidbody.mass = childVolume * Density; }
                else if (selfMass > float.Epsilon)
                { rigidbody.mass = selfMass / count; }
            }

            return obj;
        }
    }
}
