using System;
using UnityEngine;

namespace InGameComputer
{
    public class VideoCard : MonoBehaviour
    {
        [SerializeField] RenderTexture Output;

        [SerializeField] public ComputerFont[] Fonts = new ComputerFont[0];
        [SerializeField] public int SelectedFontIndex = 0;
        public ComputerFont SelectedFont => (SelectedFontIndex < 0 || SelectedFontIndex >= Fonts.Length) ? null : Fonts[SelectedFontIndex];

        [SerializeField, ReadOnly] Monitor ComputerScreen;
        [SerializeField, ReadOnly] Computer Hardware;
        [SerializeField, ReadOnly] OperatingSystem OperatingSystem;

        static Material MaterialSolid;
        static Material MaterialTexture;
        static Material MaterialFont;

        enum SelectedMaterialID
        {
            None,
            Solid,
            Texture,
            Font,
        }

        SelectedMaterialID _selectedMaterial = SelectedMaterialID.None;
        SelectedMaterialID SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (_selectedMaterial != value)
                {
                    switch (value)
                    {
                        case SelectedMaterialID.Solid:
                            MaterialSolid.SetPass(0);
                            break;
                        case SelectedMaterialID.Texture:
                            MaterialTexture.SetPass(0);
                            break;
                        case SelectedMaterialID.Font:
                            MaterialFont.SetPass(0);
                            break;
                        case SelectedMaterialID.None:
                        default:
                            break;
                    }
                }
                _selectedMaterial = value;
            }
        }

        public Vector2Int ScreenSize => new(Output.width, Output.height);

        void Start()
        {
            RegenerateMaterials();

            ComputerScreen = GetComponentInChildren<Monitor>(false);
        }

        public void AttachHardware(Computer hardware)
        {
            Hardware = hardware;
            OperatingSystem = hardware.OperatingSystem;
        }

        RectInt CharacterRect(char @char)
        {
            int cols = SelectedFont.Letters.x;
            int rows = SelectedFont.Letters.y;

            // int index = CharacterIndex(@char);

            Vector2Int fontsheetSize = SelectedFont.BitmapSize;

            int characterX = @char % cols;
            int characterY = (@char / rows) + 1;

            Vector2Int characterSize = new(fontsheetSize.x / cols, fontsheetSize.y / rows);
            Vector2Int characterOffset = new(characterX * characterSize.x, characterY * characterSize.y);

            return new RectInt(characterOffset, Vector2Int.RoundToInt(characterSize * SelectedFont.Scaler));
        }

        internal static Rect ConvertToUV(RectInt rect, Vector2 sizePixels)
        {
            Vector2 position = rect.position / sizePixels;
            Vector2 size = rect.size / sizePixels;

            position.y = 1 - position.y;

            return new Rect(position, size);
        }

        internal Vector2Int TextSize(string text, int fontSize)
        {
            Vector2Int characterSize = SelectedFont.CharacterSize(fontSize);
            Vector2Int result = new(0, characterSize.y);

            for (int i = 0; i < text.Length; i++)
            { result.x += characterSize.x; }

            return result;
        }

        internal void RegenerateMaterials()
        {
            if (MaterialSolid != null)
            { Material.Destroy(MaterialSolid); }

            if (MaterialTexture != null)
            { Material.Destroy(MaterialTexture); }

            if (MaterialFont != null)
            { Material.Destroy(MaterialFont); }

            MaterialSolid = new Material(Shader.Find("Hidden/Internal-Colored"));

            MaterialTexture = new Material(Shader.Find("Unlit/Texture"));

            MaterialFont = new Material(Shader.Find("Unlit/Texture"))
            { mainTexture = SelectedFont.Bitmap, };
        }

        internal ComputerVideoCardRenderer Render()
        {
            _selectedMaterial = SelectedMaterialID.None;
            return new ComputerVideoCardRenderer(Output);
        }

        internal void Clear() => GL.Clear(false, true, Color.black);

        [Flags]
        internal enum TextDecorations : byte
        {
            None = 0,
            Underline = 1 << 0,
            Strikeout = 1 << 1,
        }

        internal Vector2Int DrawText(string text, Vector2Int position, int fontSize, Color color, TextDecorations decorations = TextDecorations.None)
        {
            if (text is null) return Vector2Int.zero;

            int width = 0;
            Vector2Int currentPosition = position;

            for (int i = 0; i < text.Length; i++)
            {
                Vector2Int characterSize = DrawCharacter(text[i], currentPosition, fontSize, color);
                currentPosition.x += characterSize.x;
                width += characterSize.x;
            }

            DrawTextDecorations(new RectInt(position.x, position.y, width, fontSize), color, decorations);

            return new Vector2Int(width, fontSize);
        }

        internal void DrawText(string text, RectInt rect, int fontSize, Color color, TextDecorations decorations = TextDecorations.None)
        {
            Vector2Int position = rect.TopLeft();
            int width = 0;
            int height = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (position.x >= rect.xMax || position.y >= rect.yMax)
                { break; }

                Vector2Int characterSize = DrawCharacter(text[i], position, fontSize, color);
                position.x += characterSize.x;

                width += characterSize.x;
                height = Maths.Max(characterSize.y);
            }

            DrawTextDecorations(new RectInt(rect.xMin, rect.yMin, width, height), color, decorations);
        }

        void DrawTextDecorations(RectInt rect, Color color, TextDecorations decorations)
        {
            if (decorations.HasFlag(TextDecorations.Underline))
            {
                int y = rect.yMax - 1;
                DrawLine(new Vector2Int(rect.xMin, y), new Vector2Int(rect.xMax, y), color);
            }

            if (decorations.HasFlag(TextDecorations.Strikeout))
            {
                int y = rect.yMax - (rect.height / 2);
                DrawLine(new Vector2Int(rect.xMin, y), new Vector2Int(rect.xMax, y), color);
            }
        }

        internal Vector2Int DrawCharacter(char character, Vector2Int position, int fontSize, Color color)
        {
            RectInt characterRect = CharacterRect(character);

            Rect characterUv = ConvertToUV(characterRect, SelectedFont.BitmapSize);

            characterRect.size = ComputerFont.ResizeCharacter(characterRect.size, fontSize);

            DrawTexture(new RectInt(position, characterRect.size), SelectedFont.Bitmap, characterUv, color);

            return characterRect.size;
        }

        internal void DrawLine(Vector2Int pointA, Vector2Int pointB, Color color)
        {
            SelectedMaterial = SelectedMaterialID.Solid;

            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(pointA.x, pointA.y, 0);
            GL.Vertex3(pointB.x, pointB.y, 0);
            GL.End();
        }

        internal void DrawRectangle(RectInt rect, Color color)
        {
            SelectedMaterial = SelectedMaterialID.Solid;

            var corners = rect.ToFloat().Corners();

            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex(corners.TopLeft);
            GL.Vertex(corners.TopRight);
            GL.Vertex(corners.BottomRight);
            GL.Vertex(corners.BottomLeft);
            GL.End();
        }

        internal void DrawRectangleOutline(RectInt rect, Color color)
        {
            SelectedMaterial = SelectedMaterialID.Solid;

            var corners = rect.ToFloat().Corners();

            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);
            GL.Vertex(corners.TopLeft);
            GL.Vertex(corners.TopRight);
            GL.Vertex(corners.BottomRight);
            GL.Vertex(corners.BottomLeft);
            GL.Vertex(corners.TopLeft);
            GL.End();
        }

        internal bool DrawButton(RectInt rect, string label, int fontSize)
        {
            try
            {
                Vector2Int labelSize = TextSize(label, fontSize);

                RectInt labelRect = AlignText(rect, label, fontSize, Align.Middle, Align.Middle);

                bool isPointerOver = OperatingSystem.IsPointerOnRect(rect);

                if (!isPointerOver)
                {
                    DrawRectangleOutline(rect, Color.gray);

                    DrawText(label, labelRect, fontSize, Color.white);
                    return false;
                }

                if (Hardware.MouseEventSystem.IsButtonHold(0))
                {
                    DrawRectangle(rect, Color.white);

                    DrawText(label, labelRect, fontSize, Color.black);
                }
                else
                {
                    DrawRectangleOutline(rect, Color.white);

                    DrawText(label, labelRect, fontSize, Color.white);

                    if (Hardware.MouseEventSystem.IsButtonUp(0))
                    { return true; }
                }

                return false;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                DrawRectangle(rect, Color.magenta);
                return false;
            }
        }

        internal bool DrawButton(RectInt rect, string label, RectInt labelRect, int fontSize)
        {
            try
            {
                Vector2Int labelSize = TextSize(label, fontSize);

                labelRect = AlignText(labelRect, label, fontSize, Align.Middle, Align.Middle);

                bool isPointerOver = OperatingSystem.IsPointerOnRect(rect);

                if (!isPointerOver)
                {
                    DrawRectangleOutline(rect, Color.gray);

                    DrawText(label, labelRect, fontSize, Color.white);
                    return false;
                }

                if (rect.Contains(OperatingSystem.TransformPoint(Hardware.MouseEventSystem.PressedAt(0))))
                {
                    DrawRectangle(rect, Color.white);

                    DrawText(label, labelRect, fontSize, Color.black);

                    if (Hardware.MouseEventSystem.IsButtonUp(0))
                    { return true; }
                }
                else
                {
                    DrawRectangleOutline(rect, Color.white);

                    DrawText(label, labelRect, fontSize, Color.white);
                }

                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                DrawRectangle(rect, Color.magenta);
                return false;
            }
        }

        public enum Align
        {
            Start,
            Middle,
            End,
        }

        internal RectInt AlignText(RectInt rect, string text, int fontSize, Align horizontalAlign = Align.Start, Align verticalAlign = Align.Start)
        {
            Vector2Int labelSize = TextSize(text, fontSize);

            int overflowX = labelSize.x - rect.width;
            int newX = horizontalAlign switch
            {
                Align.Start => rect.xMin,
                Align.Middle => rect.xMin - (overflowX / 2),
                Align.End => rect.xMin - overflowX,
                _ => rect.xMin,
            };

            int overflowY = labelSize.y - rect.height;
            int newY = verticalAlign switch
            {
                Align.Start => rect.yMin,
                Align.Middle => rect.yMin - (overflowY / 2),
                Align.End => rect.yMin - overflowY,
                _ => rect.yMin,
            };

            return new RectInt(newX, newY, rect.width, rect.height);
        }

        internal void DrawTriangle(RectInt rect, Color color)
        {
            Vector2Int pointA;
            Vector2Int pointB;
            Vector2Int pointC;

            pointA = rect.TopLeft();
            pointB = rect.TopRight();
            pointC = new Vector2Int(rect.xMin + (rect.width / 2), rect.yMax);

            DrawTriangle(pointA, pointB, pointC, color);
        }

        internal void DrawTriangle(Vector2Int pointA, Vector2Int pointB, Vector2Int pointC, Color color)
        {
            SelectedMaterial = SelectedMaterialID.Solid;

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex(pointA.ToFloat());
            GL.Vertex(pointB.ToFloat());
            GL.Vertex(pointC.ToFloat());
            GL.End();
        }

        internal void DrawTexture(RectInt rect, Texture texture, Color color)
            => DrawTexture(rect, texture, new Rect(0, 0, 1, 1), color);

        internal void DrawTexture(RectInt rect, Texture texture, Rect uv, Color color)
        {
            if (texture == null)
            {
                DrawRectangle(rect, Color.magenta);
                return;
            }

            /*
            MaterialTexture.mainTexture = texture;
            MaterialTexture.SetPass(0);
            _selectedMaterial = SelectedMaterialID.None;

            GL.Begin(GL.QUADS);

            GL.TexCoord(uv.TopLeft());
            GL.Vertex(rect.TopLeft());

            GL.TexCoord(uv.BottomLeft());
            GL.Vertex(rect.BottomLeft());

            GL.TexCoord(uv.BottomRight());
            GL.Vertex(rect.BottomRight());

            GL.TexCoord(uv.TopRight());
            GL.Vertex(rect.TopRight());

            GL.End();

            return;
            */

            Graphics.DrawTexture(rect.ToFloat(), texture, uv, 0, 0, 0, 0, color);
            _selectedMaterial = SelectedMaterialID.None;
        }

        internal void DrawTextbox(RectInt rect, string text)
        {
            try
            {
                RectInt innerRect = rect.Padding(-2);
                RectInt labelRect = innerRect.Padding(-1);

                int fontSize = labelRect.height;

                bool isPointerOver = OperatingSystem.IsPointerOnRect(rect);

                if (!isPointerOver)
                {
                    DrawRectangle(rect, Color.gray);
                    DrawRectangle(innerRect, Color.black);

                    DrawText(text, labelRect, fontSize, Color.white);
                }
                else
                {
                    DrawRectangle(rect, Color.white);
                    DrawRectangle(innerRect, Color.black);

                    DrawText(text, labelRect, fontSize, Color.white);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                DrawRectangle(rect, Color.magenta);
            }
        }

        internal void DrawPixel(Vector2Int position, Color color)
        {
            SelectedMaterial = SelectedMaterialID.Solid;

            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(position.x, position.y, 0);
            GL.Vertex3(position.x, position.y + 1, 0);
            GL.End();
        }
    }

    public class ComputerVideoCardRenderer : IDisposable
    {
        readonly RenderTexture savedRenderTexture;

        public ComputerVideoCardRenderer(RenderTexture newRenderTexture)
        {
            savedRenderTexture = RenderTexture.active;
            RenderTexture.active = newRenderTexture;

            GL.PushMatrix();

            GL.LoadPixelMatrix(0, newRenderTexture.width, newRenderTexture.height, 0);
        }

        public void Dispose()
        {
            GL.PopMatrix();
            RenderTexture.active = savedRenderTexture;
        }
    }
}