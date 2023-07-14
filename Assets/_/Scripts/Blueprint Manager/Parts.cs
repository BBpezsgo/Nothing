using UnityEngine;

namespace Blueprints
{
    [CreateAssetMenu]
    public class Parts : ScriptableObject
    {
        public PartBodyBuiltin[] Bodies;
        public PartTurretBuiltin[] Turrets;
        public PartControllerBuiltin[] Controllers;

        public bool TryGetPart(string id, out PartBodyBuiltin part)
        {
            for (int i = 0; i < Bodies.Length; i++)
            {
                if (Bodies[i].ID == id)
                {
                    part = Bodies[i];
                    return true;
                }
            }

            part = null;
            return false;
        }

        public bool TryGetPart(string id, out PartTurretBuiltin part)
        {
            for (int i = 0; i < Turrets.Length; i++)
            {
                if (Turrets[i].ID == id)
                {
                    part = Turrets[i];
                    return true;
                }
            }

            part = null;
            return false;
        }

        public bool TryGetPart(string id, out PartControllerBuiltin part)
        {
            for (int i = 0; i < Controllers.Length; i++)
            {
                if (Controllers[i].ID == id)
                {
                    part = Controllers[i];
                    return true;
                }
            }

            part = null;
            return false;
        }
       
        public bool TryGetPart(string id, out BlueprintPart part)
        {
            if (TryGetPart(id, out PartBodyBuiltin body))
            {
                part = body;
                return true;
            }

            if (TryGetPart(id, out PartTurretBuiltin turret))
            {
                part = turret;
                return true;
            }

            if (TryGetPart(id, out PartControllerBuiltin controller))
            {
                part = controller;
                return true;
            }

            part = null;
            return false;
        }
    }
}