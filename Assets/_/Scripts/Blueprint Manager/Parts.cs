using System.Collections.Generic;
using UnityEngine;

namespace Game.Blueprints
{
    [CreateAssetMenu]
    public class Parts : ScriptableObject
    {
        public PartBodyBuiltin[] Bodies;
        public PartTurretBuiltin[] Turrets;
        public PartControllerBuiltin[] Controllers;

        [SerializeField, Button(nameof(GenerateGUIDs), true, false, "Generate GUIDs")] string buttonGenerateGUIDs;

        public bool TryGetPart<T>(string id, out T part) where T : BlueprintPart
            => TryGetPart(GetBlueprintParts(), id, out part);
        static bool TryGetPart<T>(BlueprintPart[] parts, string id, out T part) where T : BlueprintPart
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].ID == id)
                {
                    if (parts[i] is not T _part)
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

        public bool TryGetPart<T>(byte guid, out T part) where T : BlueprintPart
            => TryGetPart(GetBlueprintParts(), guid, out part);
        static bool TryGetPart<T>(BlueprintPart[] parts, byte guid, out T part) where T : BlueprintPart
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].GUID == guid)
                {
                    if (parts[i] is not T _part)
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

        BlueprintPart[] GetBlueprintParts()
        {
            List<BlueprintPart> result = new();
            result.AddRange(Bodies);
            result.AddRange(Turrets);
            result.AddRange(Controllers);
            return result.ToArray();
        }

        bool IsUnique(BlueprintPart part)
            => IsUnique(part, GetBlueprintParts());

        bool IsUnique(BlueprintPart part, BlueprintPart[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (object.ReferenceEquals(part, parts[i]))
                {
                    continue;
                }

                if (part.GUID == parts[i].GUID)
                {
                    return false;
                }
            }
            return true;
        }

        byte GenerateGUID()
        {
            return (byte)Random.Range(byte.MinValue, byte.MaxValue);
        }

        int UniqueGUIDCapacity => byte.MaxValue + 1;

        void GenerateGUIDs()
        {
            BlueprintPart[] parts = GetBlueprintParts();
            int maxGenerateIterations = 32;

            if (parts.Length > UniqueGUIDCapacity)
            {
                Debug.LogError($"Impossible to generate unique GUIDs", this);
                return;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                int maxIterations = maxGenerateIterations;
                while (!IsUnique(part, parts))
                {
                    if (maxIterations-- < 0)
                    {
                        Debug.LogError($"Failed to generate GUID: max iterations exceeded", this);
                        break;
                    }

                    part.GUID = GenerateGUID();
                }
            }
        }
    }
}
