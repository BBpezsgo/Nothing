using UnityEngine;

namespace InGameComputer
{
    [CreateAssetMenu(fileName = "ComputerFont", menuName = "Computer/Font")]
    public class ComputerFont : ScriptableObject
    {
        [SerializeField] public Texture Bitmap;
        [SerializeField] public Vector2 Scaler;
        [SerializeField] public Vector2Int Spacing;
        [SerializeField] public Vector2Int Letters;

        public Vector2Int BitmapSize => new(Bitmap.width, Bitmap.height);

        void Reset()
        {
            Scaler = Vector2.one;
            Spacing = Vector2Int.zero;
            Letters = new Vector2Int(16, 16);
        }

        public Vector2Int CharacterSize()
        {
            Vector2Int fontsheetSize = this.BitmapSize;
            Vector2Int characterSize = new(fontsheetSize.x / Letters.x, fontsheetSize.y / Letters.y);
            return Vector2Int.RoundToInt(characterSize * this.Scaler);
        }

        public Vector2Int CharacterSize(int fontSize) => ComputerFont.ResizeCharacter(this.CharacterSize(), fontSize);


        public static Vector2Int ResizeCharacter(Vector2Int originalSize, int fontSize)
        {
            float aspectRatio = (float)originalSize.x / (float)originalSize.y;
            originalSize = new Vector2Int(fontSize, fontSize);
            originalSize.x = Maths.RoundToInt(aspectRatio * originalSize.y);
            return originalSize;
        }
    }
}
