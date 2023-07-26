using AssetManager;

using DataUtilities.ReadableFileFormat;
using DataUtilities.Serializer;

using Game.Components;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Blueprints
{
    [Serializable]
    public class Blueprint : ISerializable<Blueprint>, ISerializableText, IDeserializableText, INetworkSerializable
    {
        public string Name;
        public List<string> Parts;

        public Blueprint()
        {
            Name = "New Blueprint";
            Parts = new List<string>();
        }

        public void Deserialize(Deserializer deserializer)
        {
            Name = deserializer.DeserializeString(INTEGER_TYPE.INT8);
            Parts = new List<string>(deserializer.DeserializeArray<string>(INTEGER_TYPE.INT8));
        }

        public void DeserializeText(Value data)
        {
            Name = data["Name"];
            Parts = data["Parts"].Array.ConvertPrimitive<string>().ToList();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Name, false);
                reader.ReadValueSafe(out int n);
                string[] parts = new string[n];
                for (int i = 0; i < n; i++)
                {
                    reader.ReadValueSafe(out string element, true);
                    parts[i] = element;
                }
                Parts = new List<string>(parts);
            }

            if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Name, false);
                int n = Parts.Count;
                writer.WriteValueSafe(in n);
                for (int i = 0; i < n; i++)
                {
                    string element = Parts[i];
                    writer.WriteValueSafe(element, true);
                }
            }
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Serialize(Name, INTEGER_TYPE.INT8);
            serializer.Serialize(Parts.ToArray(), INTEGER_TYPE.INT8);
        }

        public Value SerializeText()
        {
            Value result = Value.Object();
            result["Name"] = Name;
            result["Pars"] = Value.Object(Parts.ToArray());
            return result;
        }
    }

    public enum PartType : byte
    {
        Unknown,
        Body,
        Turret,
        Controller,
    }

    [Serializable]
    public abstract class BlueprintPart : ISerializable<BlueprintPart>, ISerializableText, IDeserializableText, INetworkSerializable
    {
        [Header("Base")]
        public PartType Type;
        public string ID;
        public string Name;

        public Texture2D Image;

        public void GenerateID()
        {
            ID = Guid.NewGuid().ToString();
        }

        public virtual void Deserialize(Deserializer deserializer)
        {
            Type = (PartType)deserializer.DeserializeByte();
            ID = deserializer.DeserializeString(INTEGER_TYPE.INT8);
            Name = deserializer.DeserializeString(INTEGER_TYPE.INT8);
        }

        public virtual void Serialize(Serializer serializer)
        {
            serializer.Serialize((byte)Type);
            serializer.Serialize(ID, INTEGER_TYPE.INT8);
            serializer.Serialize(Name, INTEGER_TYPE.INT8);
        }

        public virtual Value SerializeText()
        {
            Value result = Value.Object();
            result["Type"] = Type.ToString();
            result["ID"] = ID;
            result["Name"] = Name;
            return result;
        }

        public virtual void DeserializeText(Value data)
        {
            Type = Enum.Parse<PartType>(data["Type"], true);
            ID = data["ID"];
            Name = data["Name"];
        }

        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref ID);
            serializer.SerializeValue(ref Name);
        }
    }

    [Serializable]
    public abstract class BlueprintPartBuiltin : BlueprintPart
    {
        [Header("Prefab")]
        public GameObject Prefab;

        public override void Deserialize(Deserializer deserializer)
        {
            base.Deserialize(deserializer);
            Prefab = null;
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);
            Prefab = null;
        }
    }

    public struct PackedModel : ISerializableText, IDeserializableText
    {
        public string Asset;
        public Vector3 Offset;
        public Bounds Bounds;

        public void DeserializeText(Value data)
        {
            Asset = data["Asset"];
            Offset = data["Offset"].LoadObject<Vector3>();
            Bounds = data["Bounds"].LoadObject<Bounds>();
        }

        public readonly Value SerializeText()
        {
            Value result = Value.Object();
            result["Asset"] = Asset;
            result["Offset"] = Offset.SaveObject();
            result["Bounds"] = Bounds.SaveObject();
            return result;
        }
    }

    public struct PackedCollider : ISerializableText, IDeserializableText
    {
        public Vector3 Center;
        public Vector3 Size;

        public void DeserializeText(Value data)
        {
            Center = data["Center"].LoadObject<Vector3>();
            Size = data["Size"].LoadObject<Vector3>();
        }

        public readonly Value SerializeText()
        {
            Value result = Value.Object();
            result["Center"] = Center.SaveObject();
            result["Size"] = Size.SaveObject();
            return result;
        }
    }

    public struct PartCannon : ISerializableText, IDeserializableText
    {
        public Vector3 Position;
        public PackedModel Model;
        public Vector3 ShootPosition;

        public void DeserializeText(Value data)
        {
            Position = data["Position"].LoadObject<Vector3>();
            Model = new PackedModel();
            Model.DeserializeText(data["Model"]);
            ShootPosition = data["ShootPosition"].LoadObject<Vector3>();
        }

        public readonly Value SerializeText()
        {
            Value result = Value.Object();
            result["Position"] = Position.SaveObject();
            result["Model"] = Model.SaveObject();
            result["ShootPosition"] = ShootPosition.SaveObject();
            return result;
        }
    }

    public struct PartWheel : ISerializableText, IDeserializableText
    {
        public Vector3 Position;
        public string Type;

        public void DeserializeText(Value data)
        {
            Position = data["Position"].LoadObject<Vector3>();
            Type = data["Type"];
        }

        public readonly Value SerializeText()
        {
            Value result = Value.Object();
            result["Position"] = Position.SaveObject();
            result["Type"] = Type;
            return result;
        }
    }

    public class PartBody : BlueprintPart
    {
        public PackedModel Model;
        public PackedCollider Collider;
        public Vector3 TurretPosition;
        public PartWheel[] Wheels;
        public float HP;

        public PartBody()
        {
            base.Type = PartType.Body;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);

            Model = new PackedModel();
            Model.DeserializeText(data["Model"]);

            Collider = new PackedCollider();
            Collider.DeserializeText(data["Collider"]);

            TurretPosition = data["TurretPosition"].LoadObject<Vector3>();

            Wheels = data["Wheels"].Array.Convert<PartWheel>();

            HP = data["HP"].Float ?? 0f;
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            result["Model"] = Model.SerializeText();
            result["Collider"] = Collider.SerializeText();
            result["TurretPosition"] = TurretPosition.SaveObject();
            Value wheels = Value.Object();
            for (int i = 0; i < Wheels.Length; i++)
            { wheels[i] = Wheels[i].SerializeText(); }
            result["Wheels"] = wheels;

            result["HP"] = HP;

            return result;
        }
    }

    [Serializable]
    public class PartBodyBuiltin : BlueprintPartBuiltin
    {
        [Header("Part Specific")]
        [Min(0)] public float HP;

        public PartBodyBuiltin()
        {
            base.Type = PartType.Body;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);

            HP = data["HP"].Float ?? 0f;
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            result["HP"] = HP;

            return result;
        }
    }

    public class PartTurret : BlueprintPart
    {
        public PackedModel Model;
        public PartCannon Cannon;

        public PartTurret()
        {
            base.Type = PartType.Turret;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);

            Model = new PackedModel();
            Model.DeserializeText(data["Model"]);

            Cannon = new PartCannon();
            Cannon.DeserializeText(data["Cannon"]);
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            result["Model"] = Model.SerializeText();
            result["Cannon"] = Cannon.SerializeText();

            return result;
        }
    }

    [Serializable]
    public class PartTurretBuiltin : BlueprintPartBuiltin
    {
        public PartTurretBuiltin()
        {
            base.Type = PartType.Turret;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            return result;
        }
    }

    public class PartController : BlueprintPart
    {
        public string BehaviourType;

        public PartController()
        {
            base.Type = PartType.Controller;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);

            BehaviourType = data["BehaviourType"];
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            result["BehaviourType"] = BehaviourType;

            return result;
        }
    }

    [Serializable]
    public class PartControllerBuiltin : BlueprintPart
    {
        [Header("Part Specific")]
        public string BehaviourType;

        public PartControllerBuiltin()
        {
            base.Type = PartType.Controller;
            base.GenerateID();
        }

        public override void DeserializeText(Value data)
        {
            base.DeserializeText(data);

            BehaviourType = data["BehaviourType"];
        }

        public override Value SerializeText()
        {
            Value result = base.SerializeText();

            result["BehaviourType"] = BehaviourType;

            return result;
        }
    }

    class FuckYouUnity : MonoBehaviour
    {
        [SerializeField, ReadOnly] MeshRenderer MeshRenderer;
        [SerializeField, ReadOnly] internal Transform Parent;

        void Start()
        {
            MeshRenderer = GetComponent<MeshRenderer>();
        }

        void FixedUpdate()
        {
            MeshRenderer.ResetBounds();
            MeshRenderer.ResetLocalBounds();
            if (Parent != null)
            {
                MeshRenderer.bounds = new Bounds(Parent.position, MeshRenderer.bounds.size);
            }
        }

        void OnDrawGizmos()
        {
            var bounds = MeshRenderer.bounds;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    public static class Utils
    {
        public static string NormalizeID(string text)
        {
            string result = text;
            result = result.Trim();
            result = result.ToLowerInvariant();
            result = result.Replace(' ', '-');
            result = result.Replace('\t', '-');
            result = result.Replace("\r", "");
            result = result.Replace("\n", "");
            result = result.Replace("\0", "");
            return result;
        }

        public static string FixFilename(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string result = filename;
            for (int i = 0; i < invalidChars.Length; i++)
            { result = result.Replace(invalidChars[i], '-'); }
            return result;
        }
    }

    public class BlueprintManager : SingleInstance<BlueprintManager>
    {
        [SerializeField] Parts _builtinParts;

        [Header("Photography Studio")]
        [SerializeField] GameObject renderThis;
        [SerializeField, Button(nameof(PlaceObject), true, true, "Place Object")] string btnPlaceObj;
        [SerializeField, ReadOnly] GameObject placedObject;
        [SerializeField] Vector3 _photographyPosition;
        [SerializeField, Min(0f)] float _photographySize = 1f;

        [SerializeField] RenderTexture _photographyStudioRenderer;
        [SerializeField] Transform _photographyStudio;

        [SerializeField, Button(nameof(RenderPhotographyStudio), true, true, "Render Photography Studio")] string _btnRender;
        [SerializeField, Thumbnail(nameof(_photographyStudioRenderer))] Texture2D lastRender = null;

        static BlueprintPart[] LoadedParts = new BlueprintPart[0];
        public static Parts BuiltinParts => Instance._builtinParts;

        static void SetGameLayerRecursive(GameObject @object, int layer)
        {
            @object.layer = layer;
            foreach (Transform child in @object.transform)
            { SetGameLayerRecursive(child.gameObject, layer); }
        }

        static Bounds GetSummedBounds(GameObject @object)
        {
            Bounds result = new(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
            var renderers = @object.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return result;
            result = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                result.Encapsulate(renderers[i].bounds);
            }
            return result;
        }

        void PlaceObject()
        {
            bool lastEnabled = _photographyStudio.gameObject.activeSelf;
            if (!lastEnabled)
            { _photographyStudio.gameObject.SetActive(true); }

            if (placedObject != null)
            {
                if (Application.isPlaying)
                { Texture2D.Destroy(placedObject); }
                else
                { Texture2D.DestroyImmediate(placedObject); }
            }

            placedObject = GameObject.Instantiate(renderThis, Vector3.zero, Quaternion.Euler(0f, -220f, 0f), _photographyStudio);

            float baseTop = _photographyPosition.y - (_photographySize / 2);

            var bounds = GetSummedBounds(placedObject);

            float scale = (_photographySize / (float)Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));

            placedObject.transform.localScale = scale * Vector3.one;

            bounds = GetSummedBounds(placedObject);

            placedObject.transform.position = new Vector3(_photographyPosition.x, baseTop + bounds.extents.y, _photographyPosition.z) - bounds.center;

            // bounds = GetSummedBounds(placedObject);

            // Debug3D.DrawBox(bounds, Color.green, 5f);

            SetGameLayerRecursive(placedObject, _photographyStudio.gameObject.layer);

            if (!lastEnabled)
            {
                _photographyStudio.gameObject.GetComponentInChildren<Camera>().Render();
            }

            if (!lastEnabled)
            { _photographyStudio.gameObject.SetActive(false); }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(_photographyPosition, Vector3.one * _photographySize);
        }

        void RenderPhotographyStudio()
        {
            if (lastRender != null)
            {
                if (Application.isPlaying)
                { Texture2D.Destroy(lastRender); }
                else
                { Texture2D.DestroyImmediate(lastRender); }
            }

            lastRender = new(_photographyStudioRenderer.width, _photographyStudioRenderer.height, TextureFormat.RGBAFloat, false, true);
            RenderTexture oldRenderTexture = RenderTexture.active;
            RenderTexture.active = _photographyStudioRenderer;
            lastRender.ReadPixels(new Rect(0, 0, _photographyStudioRenderer.width, _photographyStudioRenderer.height), 0, 0);
            lastRender.Apply();
            RenderTexture.active = oldRenderTexture;

            File.WriteAllBytes($@"C:\Users\bazsi\Desktop\Nothing Assets 3D\Photography Studio\{"PartImage"}.png", lastRender.EncodeToPNG());
        }

#if UNITY_EDITOR
        static Value PackModel(Transform meshObject)
            => PackModel(meshObject.gameObject);
        static Value PackModel(GameObject meshObject)
        {
            Value model = Value.Object();

            if (meshObject.TryGetComponent(out MeshFilter meshFilter) ||
                (meshObject.transform.childCount == 1 && meshObject.transform.GetChild(0).TryGetComponent(out meshFilter)))
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    string modelName = Path.GetFileName(assetPath);
                    model["Asset"] = modelName;
                }
                else
                {
                    Debug.LogWarning($"Model asset not found", meshFilter);
                }

                if (meshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    model["Bounds"] = meshRenderer.localBounds.SaveObject();
                }
            }
            else
            {
                Debug.LogWarning($"Model object not found");
            }

            model["Offset"] = meshObject.transform.localPosition.SaveObject();

            return model;
        }

        public static Value PackPart(AssetManager.Components.PartPacker part)
        {
            Value result = Value.Object();

            result["Type"] = part.Type.ToString();
            result["ID"] = Utils.NormalizeID(part.gameObject.name);
            result["Name"] = part.gameObject.name;

            Transform objModel = part.transform.Find("Model");
            if (objModel != null)
            { result["Model"] = PackModel(objModel); }

            Transform colliderObject = part.transform.Find("Collider");
            if (colliderObject != null)
            { result["Collider"] = colliderObject.GetComponent<BoxCollider>().SaveComponent(); }

            Transform turretPosition = part.transform.Find("TurretPosition");
            if (turretPosition != null)
            { result["TurretPosition"] = turretPosition.localPosition.SaveObject(); }

            Transform _wheels = part.transform.Find("Wheels");
            if (_wheels != null)
            {
                Value wheels = Value.Object();
                for (int i = 0; i < _wheels.childCount; i++)
                {
                    var _wheel = _wheels.GetChild(i);
                    Value wheel = Value.Object();

                    wheel["Position"] = _wheel.localPosition.SaveObject();
                    wheel["Type"] = _wheel.gameObject.name;

                    wheels[i] = wheel;
                }
                result["Wheels"] = wheels;
            }

            Transform cannonObject = part.transform.Find("Cannon");
            if (cannonObject != null)
            {
                Value cannon = Value.Object();

                cannon["Position"] = cannonObject.localPosition.SaveObject();
                cannon["Model"] = PackModel(cannonObject.Find("Model"));
                cannon["ShootPosition"] = cannonObject.Find("ShootPosition").localPosition.SaveObject();

                result["Cannon"] = cannon;
            }

            return result;
        }
#endif

        static BlueprintPart LoadPart(Value data)
        {
            PartType partType = Enum.Parse<PartType>(data["Type"], true);

            switch (partType)
            {
                case PartType.Body:
                    {
                        PartBody newPart = new();
                        newPart.DeserializeText(data);
                        if (!string.IsNullOrWhiteSpace(newPart.ID))
                        {
                            Debug.Log($"[{nameof(BlueprintManager)}]: Part is \"{newPart.ID}\" loaded");
                            return newPart;
                        }

                        Debug.LogWarning($"[{nameof(BlueprintManager)}]: Part \"{newPart.Name}\" id is null");
                        return null;
                    }

                case PartType.Turret:
                    {
                        PartTurret newPart = new();
                        newPart.DeserializeText(data);
                        if (!string.IsNullOrWhiteSpace(newPart.ID))
                        {
                            Debug.Log($"[{nameof(BlueprintManager)}]: Part is \"{newPart.ID}\" loaded");
                            return newPart;
                        }

                        Debug.LogWarning($"[{nameof(BlueprintManager)}]: Part \"{newPart.Name}\" id is null");
                        return null;
                    }

                case PartType.Controller:
                    {
                        PartController newPart = new();
                        newPart.DeserializeText(data);
                        if (!string.IsNullOrWhiteSpace(newPart.ID))
                        {
                            Debug.Log($"[{nameof(BlueprintManager)}]: Part is \"{newPart.ID}\" loaded");
                            return newPart;
                        }

                        Debug.LogWarning($"[{nameof(BlueprintManager)}]: Part \"{newPart.Name}\" id is null");
                        return null;
                    }

                case PartType.Unknown:
                default:
                    {
                        Debug.LogWarning($"[{nameof(BlueprintManager)}]: Unknown part type {partType} ({(byte)partType})");
                        return null;
                    }
            }
        }

        public static BlueprintPart[] LoadParts()
        {
            Debug.Log($"[{nameof(BlueprintManager)}]: Loading parts ...");
            AssetManager.AssetManager.Instance.LoadIfNot();
            Value[] partsData = AssetManager.AssetManager.Instance.LoadFilesWithInheritacne($"Parts");

            List<BlueprintPart> parts = new();

            for (int i = 0; i < partsData.Length; i++)
            {
                Value partData = partsData[i];
                BlueprintPart newPart = LoadPart(partData);
                if (newPart != null) parts.Add(newPart);
            }

            LoadedParts = parts.ToArray();
            return parts.ToArray();
        }

        public static Blueprint[] LoadBlueprints()
        {
            Debug.Log($"[{nameof(BlueprintManager)}]: Loading blueprints ...");
            string[] blueprintFiles = Storage.GetFiles("Blueprints");
            List<Blueprint> result = new();
            for (int i = 0; i < blueprintFiles.Length; i++)
            {
                Blueprint blueprint = Storage.ReadObject<Blueprint>("Blueprints", blueprintFiles[i]);
                if (blueprint != null)
                { result.Add(blueprint); }
            }
            return result.ToArray();
        }

        public static void SaveBlueprint(Blueprint blueprint)
        {
            Debug.Log($"[{nameof(BlueprintManager)}]: Save blueprint \"{blueprint.Name}\" ...");
            Storage.Write(blueprint, "Blueprints", $"{Utils.FixFilename(blueprint.Name)}.bin");
        }

        public static Blueprint GetBlueprint(string name)
        {
            Blueprint[] blueprints = LoadBlueprints();
            for (int i = 0; i < blueprints.Length; i++)
            {
                if (blueprints[i].Name == name) return blueprints[i];
            }
            return null;
        }
        public static bool TryGetBlueprint(string name, out Blueprint blueprint)
        {
            Blueprint[] blueprints = LoadBlueprints();
            for (int i = 0; i < blueprints.Length; i++)
            {
                if (blueprints[i].Name == name)
                {
                    blueprint = blueprints[i];
                    return true;
                }
            }
            blueprint = null;
            return false;
        }

        public static bool TryGetPart(string id, out BlueprintPart part)
        {
            if (BuiltinParts.TryGetPart(id, out BlueprintPart builtinPart))
            {
                part = builtinPart;
                return true;
            }

            for (int i = 0; i < LoadedParts.Length; i++)
            {
                if (LoadedParts[i].ID == id)
                {
                    part = LoadedParts[i];
                    return true;
                }
            }

            part = null;
            return false;
        }

        public static bool TryGetPart<T>(string id, out T part) where T : BlueprintPart
        {
            if (BuiltinParts.TryGetPart(id, out BlueprintPart builtinPart))
            {
                if (builtinPart is not T _builtinPart)
                {
                    part = null;
                    return false;
                }

                part = _builtinPart;
                return true;
            }

            for (int i = 0; i < LoadedParts.Length; i++)
            {
                if (LoadedParts[i].ID == id)
                {
                    if (LoadedParts[i] is not T _part)
                    {
                        part = null;
                        return false;
                    }

                    part = _part;
                    return true;
                }
            }
            part = null;
            return false;
        }

        static void LoadModel(PackedModel model, Transform parent)
        {
            GameObject modelObject = new("Model");
            modelObject.transform.SetParent(parent);
            modelObject.transform.localPosition = model.Offset;
            LoadModel(model, modelObject);
            modelObject.GetComponent<FuckYouUnity>().Parent = parent;
        }
        static void FixMesh(PackedModel info, Mesh mesh)
        {
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            mesh.RecalculateBounds(UnityEngine.Rendering.MeshUpdateFlags.Default);

            Bounds bounds = info.Bounds;
            // bounds.center = mesh.transform.position;
            mesh.bounds = bounds;

            Debug3D.DrawBox(
                mesh.bounds.center,
                mesh.bounds.size,
                Color.white,
                10f);
        }
        static void LoadModel(PackedModel model, GameObject @object)
        {
            AssetManager.AssetManager.LoadModel(model.Asset, @object);
            FixMesh(model, @object.GetComponent<MeshFilter>().mesh);
            FixMesh(model, @object.GetComponent<MeshFilter>().sharedMesh);

            @object.GetComponent<MeshRenderer>().ResetBounds();
            @object.GetComponent<MeshRenderer>().ResetLocalBounds();

            @object.AddComponent<FuckYouUnity>();

            /*
            Vector3 fixedOffset = model.Offset;
            fixedOffset.x *= -1;
            @object.transform.SetLocalPositionAndRotation(
                fixedOffset,
                Quaternion.identity);
            @object.transform.localScale = Vector3.one;
            */
        }

        static BoxCollider AddCollider(PackedCollider collider, GameObject @object)
        {
            BoxCollider result = @object.AddOrModifyComponent<BoxCollider>();

            result.center = collider.Center;
            result.size = collider.Size;
            result.isTrigger = false;

            return result;
        }

        /// <exception cref="BlueprintException"/>
        public static GameObject InstantiateBlueprint(Blueprint blueprint)
        {
            GameObject result = null;

            LoadParts();

            PartBody bodyPart = null;
            PartBodyBuiltin bodyPartBuiltin = null;
            PartController controller = null;
            PartControllerBuiltin controllerBuiltin = null;

            if (blueprint.TryGetPart(out bodyPartBuiltin))
            {
                result = GameObject.Instantiate(bodyPartBuiltin.Prefab);
                result.SetActive(false);

                result.name = $"{blueprint.Name} Instance";
                result.transform.position = Vector3.zero;
                NetworkObject networkObject = result.AddComponent<NetworkObject>();
                networkObject.AutoObjectParentSync = false;

                if (blueprint.TryGetPart(out PartTurretBuiltin turretPartBuiltin))
                {
                    Transform turretPositionObject = result.transform.Find("TurretPosition");
                    if (turretPositionObject == null)
                    {
                        throw new BlueprintException($"Can not find child \"TurretPosition\" in body object", result);
                    }

                    GameObject turretObject = GameObject.Instantiate(turretPartBuiltin.Prefab, result.transform);
                    turretObject.transform.localPosition = turretPositionObject.localPosition;

                    if (!turretObject.TryGetComponent(out Turret turret))
                    {
                        throw new BlueprintException($"Can not find component \"Turret\" in turret object", turretObject);
                    }

                    turret.cannonRotationSpeed = 50f;
                    turret.rotationSpeed = 50f;
                    turret.reloadTime = .5f;

                    turret.projectileIgnoreCollision.Add(result.transform);
                }
            }
            else if (blueprint.TryGetPart(out bodyPart))
            {
                result = new($"{blueprint.Name} Instance");
                result.SetActive(false);
                result.transform.position = Vector3.zero;
                result.AddComponent<NetworkObject>();

                Rigidbody rigidbody = result.AddOrModifyComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                BoxCollider collider = AddCollider(bodyPart.Collider, result);
                VehicleEngine engine = result.AddOrModifyComponent<VehicleEngine>();
                UnitBehaviour behaviour = result.AddOrModifyComponent<UnitBehaviour>();

                LoadModel(bodyPart.Model, result.transform);

                PartWheel[] wheels = bodyPart.Wheels;
                Transform wheelsObject = new GameObject("Wheels").transform;
                wheelsObject.SetParent(result.transform);
                wheelsObject.localPosition = Vector3.zero;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if (!AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue(wheels[i].Type, out GameObject wheelPrefab))
                    {
                        Debug.LogError($"Wheel \"{wheels[i].Type}\" not found");
                        continue;
                    }
                    GameObject newWheel = Instantiate(wheelPrefab);
                    newWheel.transform.SetParent(wheelsObject);
                    newWheel.transform.localPosition = wheels[i].Position;
                }

                if (blueprint.TryGetPart(out PartTurret turretPart))
                {
                    GameObject turretObject = new("Turret");
                    turretObject.transform.SetParent(result.transform);
                    turretObject.transform.localPosition = bodyPart.TurretPosition;

                    LoadModel(turretPart.Model, turretObject.transform);

                    GameObject cannonObject = new("Cannon");
                    cannonObject.transform.SetParent(turretObject.transform);
                    cannonObject.transform.localPosition = turretPart.Cannon.Position;

                    LoadModel(turretPart.Cannon.Model, cannonObject.transform);

                    GameObject cannonShootPositionObject = new("ShootPosition");
                    cannonShootPositionObject.transform.SetParent(cannonObject.transform);
                    cannonShootPositionObject.transform.localPosition = turretPart.Cannon.ShootPosition;

                    Turret turret = turretObject.AddComponent<Turret>();
                    turret.cannon = cannonObject.transform;
                    turret.shootPosition = cannonShootPositionObject.transform;
                    turret.cannonRotationSpeed = 50f;
                    turret.rotationSpeed = 50f;
                    turret.reloadTime = .5f;
                    turret.projectileIgnoreCollision.Add(result.transform);
                    turret.projectileIgnoreCollision.Add(cannonObject.transform);
                    turret.projectileIgnoreCollision.Add(cannonObject.transform);

                    if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("projectile-mg", out var projectilePrefab))
                    {
                        turret.projectile = projectilePrefab;
                    }
                    else
                    { Debug.LogError($"[{nameof(BlueprintManager)}]: Builtin prefab \"projectile-mg\" not found"); }

                    if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("effect-shoot-mg", out var shootEffect))
                    {
                        turret.ShootEffects = new GameObject[] {
                            shootEffect,
                        };
                    }
                    else
                    { Debug.LogError($"[{nameof(BlueprintManager)}]: Builtin prefab \"effect-shoot-mg\" not found"); }
                }
            }

            if (result == null)
            {
                throw new BlueprintException($"Failed to construct the blueprint: no body");
            }

            if (blueprint.TryGetPart(out controllerBuiltin))
            {
                switch (controllerBuiltin.BehaviourType)
                {
                    case "Attacker":
                        {

                            UnitBehaviour_Seek seek = result.AddComponent<UnitBehaviour_Seek>();
                            UnitAttacker unitAttacker = result.AddComponent<UnitAttacker>();
                            Attacker attacker = result.AddComponent<Attacker>();

                            attacker.turret = result.GetComponentInChildren<Turret>(true);

                            unitAttacker.turret = attacker.turret;

                            if (bodyPartBuiltin != null)
                            {
                                unitAttacker.HP = bodyPartBuiltin.HP;
                            }
                            else
                            {
                                Debug.LogWarning($"Attacker needs a body part");
                            }

                            if (result.transform.Find("__unit-ui-selected") != null)
                            {
                                unitAttacker.UiSelected = result.transform.Find("__unit-ui-selected").gameObject;
                            }
                            else if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("unit-ui-selected", out var selectedPrefab))
                            {
                                GameObject selectedInstance = GameObject.Instantiate(selectedPrefab);
                                selectedInstance.transform.SetParent(result.transform);
                                unitAttacker.UiSelected = selectedInstance;
                            }
                            else
                            {
                                Debug.LogWarning($"[{nameof(BlueprintManager)}]: Builtin prefab \"unit-ui-selected\" not found");
                            }

                            if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("effect-exploison", out var exploisonEffect))
                            {
                                unitAttacker.DestroyEffect = exploisonEffect;
                            }
                            else
                            {
                                Debug.LogWarning($"[{nameof(BlueprintManager)}]: Builtin prefab \"effect-exploison\" not found");
                            }

                            break;
                        }
                    default:
                        Debug.LogWarning($"Unknown controller behaviour type \"{controllerBuiltin.BehaviourType}\"");
                        break;
                }
            }
            else if (blueprint.TryGetPart(out controller))
            {
                switch (controller.BehaviourType)
                {
                    case "Attacker":
                        {

                            UnitBehaviour_Seek seek = result.AddComponent<UnitBehaviour_Seek>();
                            UnitAttacker unitAttacker = result.AddComponent<UnitAttacker>();
                            Attacker attacker = result.AddComponent<Attacker>();

                            attacker.turret = result.GetComponentInChildren<Turret>(true);

                            unitAttacker.turret = attacker.turret;

                            if (bodyPart != null)
                            {
                                unitAttacker.HP = bodyPart.HP;
                            }
                            else
                            {
                                Debug.LogWarning($"Attacker needs a body part");
                            }

                            if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("unit-ui-selected", out var selectedPrefab))
                            {
                                var newa = GameObject.Instantiate(selectedPrefab);
                                newa.transform.SetParent(result.transform);
                                unitAttacker.UiSelected = newa;
                            }
                            else
                            {
                                Debug.LogError($"[{nameof(BlueprintManager)}]: Builtin prefab \"unit-ui-selected\" not found");
                            }

                            if (AssetManager.AssetManager.Instance.BuiltinPrefabs.TryGetValue("effect-exploison", out var exploisonEffect))
                            {
                                unitAttacker.DestroyEffect = exploisonEffect;
                            }
                            else
                            {
                                Debug.LogError($"[{nameof(BlueprintManager)}]: Builtin prefab \"effect-exploison\" not found");
                            }

                            break;
                        }
                    default:
                        Debug.LogWarning($"Unknown controller behaviour type \"{controller.BehaviourType}\"");
                        break;
                }
            }

            return result;
        }
    }

    public static class Extensions
    {
        public static bool TryGetPart(this Blueprint blueprint, string id, out BlueprintPart part)
        {
            for (int i = 0; i < blueprint.Parts.Count; i++)
            {
                if (blueprint.Parts[i] == id)
                {
                    return BlueprintManager.TryGetPart(id, out part);
                }
            }
            part = null;
            return false;
        }

        public static bool TryGetPart<T>(this Blueprint blueprint, string id, out T part) where T : BlueprintPart
        {
            for (int i = 0; i < blueprint.Parts.Count; i++)
            {
                if (blueprint.Parts[i] == id)
                {
                    return BlueprintManager.TryGetPart<T>(id, out part);
                }
            }
            part = null;
            return false;
        }

        public static bool TryGetPart<T>(this Blueprint blueprint, out T part) where T : BlueprintPart
        {
            for (int i = 0; i < blueprint.Parts.Count; i++)
            {
                bool itis = BlueprintManager.TryGetPart<T>(blueprint.Parts[i], out T _part);
                if (itis)
                {
                    part = _part;
                    return true;
                }
            }
            part = null;
            return false;
        }
    }

    public class BlueprintException : Exception
    {
        public GameObject Context;

        public BlueprintException()
        {
            this.Context = null;
        }
        public BlueprintException(string message) : base(message)
        {
            this.Context = null;
        }
        public BlueprintException(string message, GameObject context) : base(message)
        {
            this.Context = context;
        }
    }
}
