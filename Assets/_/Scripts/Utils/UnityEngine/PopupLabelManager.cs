using System.Collections.Generic;
using UnityEngine;

namespace Game.Managers
{
    public readonly struct PopupLabel
    {
        public readonly GUIContent Content;
        public readonly Color Color;

        public readonly Vector3 WorldPosition;

        public readonly float MadeAt;
        public readonly float LifeTime;
        public bool IsDestroyed => Time.unscaledTime - MadeAt >= LifeTime;

        const float FontSize = 12f;
        const float GrowTime = .2f;
        const float FadeOutTime = .3f;
        const float Offset = 10f;

        public PopupLabel(string text, Color color, float lifeTime, Vector3 worldPosition)
        {
            Content = new GUIContent(text);
            Color = color;
            MadeAt = Time.unscaledTime;
            LifeTime = lifeTime;
            WorldPosition = worldPosition;
        }

        public void OnGUI()
        {
            if (IsDestroyed) return;
            float t = Maths.Clamp01((Time.unscaledTime - MadeAt) / LifeTime);

            Vector3 worldPosition = WorldPosition;
            worldPosition.y += t * Offset;

            Vector3 screenPos = MainCamera.Camera.WorldToScreenPoint(worldPosition);

            if (screenPos.x < 0f || screenPos.y < 0f ||
                screenPos.x > Screen.width || screenPos.y > Screen.height ||
                screenPos.z <= 0f)
            { return; }

            Color color = Color;

            color.a = Maths.Clamp01((1f - t) * (1f / FadeOutTime));

            GUIStyle labelStyle = new(GUI.skin.label)
            {
                fontSize = Maths.RoundToInt(Maths.Clamp(t * (FontSize / GrowTime), 1f, FontSize))
            };

            Vector2 size = labelStyle.CalcSize(Content);

            screenPos.x -= size.x / 2;
            screenPos.y = Screen.height - screenPos.y - size.y;

            Rect rect = new(screenPos, size);
            Rect shadowRect = rect;
            shadowRect.x += 1f;
            shadowRect.y += 1f;

            Color savedColor = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, color.a);
            GUI.Label(shadowRect, Content, labelStyle);

            GUI.color = color;
            GUI.Label(rect, Content, labelStyle);

            GUI.color = savedColor;
        }
    }

    public class PopupLabelManager : PrivateSingleInstance<PopupLabelManager>
    {
        readonly List<PopupLabel> PopupLabels = new();

        void OnGUI()
        {
            for (int i = PopupLabels.Count - 1; i >= 0; i--)
            {
                if (PopupLabels[i].IsDestroyed) { PopupLabels.RemoveAt(i); continue; }
                PopupLabels[i].OnGUI();
            }
        }

        public static void ShowLabel(PopupLabel label)
            => instance.PopupLabels.Add(label);
        public static void ShowLabel(string text, Vector3 worldPosition, Color color, float time = 1f)
            => instance.PopupLabels.Add(new PopupLabel(text, color, time, worldPosition));
    }
}
