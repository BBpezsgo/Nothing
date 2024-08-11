using System;
using UI;

using UnityEngine;

namespace Game.UI
{
    public class UIStats : MonoBehaviour
    {
        int FrameCounter = 0;
        float TimeCounter = 0.0f;
        float LastFramerate = 0.0f;
        public float RefreshTime = 0.5f;
        // float[] drawValues = new float[10];
        // Material mat;
        // ImguiWindow Window;

        [SerializeField] GUISkin Skin;

        /*
        void Start()
        {
            mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            Window = IMGUIManager.Instance.CreateWindow(new Rect(Screen.width - 150 - 5, 5, 150, 100));
            Window.Title = "Performance";
            Window.Visible = true;
            Window.DrawContent = OnDrawWindow;
        }
        */

        void Update()
        {
            if (TimeCounter < RefreshTime)
            {
                TimeCounter += Time.deltaTime;
                FrameCounter++;
            }
            else
            {
                LastFramerate = FrameCounter / TimeCounter;
                FrameCounter = 0;
                TimeCounter = 0f;

                /*
                float[] source = drawValues;
                float[] destination = new float[source.Length];
                System.Array.Copy(source, 1, destination, 0, source.Length - 1);
                drawValues = destination;

                drawValues[^1] = LastFramerate;
                */
            }

            /*
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Window.Visible = !Window.Visible;
            }
            */
        }

        void OnGUI()
        {
            using (GUIUtils.Skin(Skin))
            {
                GUIStyle style = Skin.GetStyle("label-stats");

                string label = $"FPS: {(int)MathF.Ceiling(LastFramerate)}";
                Vector2 size = style.CalcSize(new GUIContent(label));
                Vector2 position = new Vector2(Screen.width - size.x, 0) + new Vector2(-4, 4);
                GUI.Label(new Rect(position, size), label, style);
            }
        }

        /*
        void OnDrawWindow()
        {
            GUILayout.Label($"FPS: {Maths.RoundToInt(LastFramerate)}");

            Rect graphRect = GUILayoutUtility.GetRect(80f, 100f);
            if (Event.current.type == EventType.Repaint)
            {
                DrawGraph(graphRect, 150f);
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        void DrawGraph(Rect rect, float maxValue)
        {
            GL.PushMatrix();

            GL.Clear(true, false, Color.black);
            mat.SetPass(0);

            // Draw a black back ground Quad 
            GL.Begin(GL.QUADS);
            GL.Color(new Color(40 / 255f, 40 / 255f, 40 / 255f, 0.3019608f));
            GL.Vertex3(rect.x, rect.y, 0);
            GL.Vertex3(rect.x + rect.width, rect.y, 0);
            GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0);
            GL.Vertex3(rect.x, rect.y + rect.height, 0);
            GL.End();

            // Draw the lines of the graph
            GL.Begin(GL.LINES);
            GL.Color(new Color(34.1f / 100f, 68.2f / 100f, 100f / 100f));

            float widthScale = 1f / (drawValues.Length-1) * rect.width;
            float heightScale = 1f / maxValue * rect.height;

            for (int i = 1; i < drawValues.Length; i++)
            {
                float value1 = drawValues[i - 1];
                float value2 = drawValues[i];

                value1 = Maths.Clamp(value1, 0f, maxValue);
                value2 = Maths.Clamp(value2, 0f, maxValue);

                float x1 = (i - 1) * widthScale;
                float x2 = (i) * widthScale;

                float y1 = (maxValue - value1) * heightScale;
                float y2 = (maxValue -  value2) * heightScale;

                GL.Vertex3(rect.x + x1, rect.y + y1, 0);
                GL.Vertex3(rect.x + x2, rect.y + y2, 0);
            }

            GL.End();

            GL.PopMatrix();
        }
        */
    }
}
