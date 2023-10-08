using UnityEngine;

namespace Game
{
    public static class MainCamera
    {
        static Camera camera;
        public static Camera Camera
        {
            get
            {
                if (camera == null)
                { camera = Camera.main; }
                return camera;
            }
        }
    }
}
