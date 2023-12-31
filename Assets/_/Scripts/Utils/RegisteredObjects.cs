using Game.Components;

using System.Collections.Generic;

namespace Game
{
    internal static class RegisteredObjects
    {
        public static List<Building> Buildings = new(20);
        public static List<Unit> Units = new(20);
        public static List<Projectile> Projectiles = new(50);
        public static List<BuildableBuilding> BuildableBuildings = new();
    }
}
