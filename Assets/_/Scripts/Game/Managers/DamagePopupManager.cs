using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum DamageKind
    {
        Physical,
        Explosive,
    }
}

namespace Game.Managers
{
    public struct DamagePopupLabel
    {
        public const float MaxLifeTime = 2f;
        public const float MaxOffsetTime = .5f;

        public Vector3 WorldPosition;
        public readonly float OffsetTime;
        public float LifeTime;
        public float Amount;
        public DamageKind DamageKind;

        public DamagePopupLabel(Vector3 worldPosition, float time, float amount, DamageKind damageKind)
        {
            WorldPosition = worldPosition;
            LifeTime = time;
            OffsetTime = time;
            Amount = amount;
            DamageKind = damageKind;
        }
    }

    public class DamagePopupManager : SingleInstance<DamagePopupManager>
    {
        [SerializeField] Vector2 LabelOffset;

        [SerializeField] Color PhysicalDamageColor;
        [SerializeField] Color ExplosiveDamageColor;

        readonly List<DamagePopupLabel> DamagePopupLabels = new();

        void Reset()
        {
            PhysicalDamageColor = Color.yellow;
            ExplosiveDamageColor = Color.red;
        }

        void OnGUI()
        {
            List<Rect> rects = new(DamagePopupLabels.Count);

            for (int i = 0; i < DamagePopupLabels.Count; i++)
            {
                if (Time.time - DamagePopupLabels[i].LifeTime >= DamagePopupLabel.MaxLifeTime) continue;
                Vector3 screenPoint = MainCamera.Camera.WorldToScreenPoint(DamagePopupLabels[i].WorldPosition);

                if (screenPoint.z < 0f) continue;

                screenPoint = GUIUtils.TransformPoint(screenPoint);

                screenPoint += (Vector3)(Math.Clamp((Time.time - DamagePopupLabels[i].OffsetTime) / DamagePopupLabel.MaxOffsetTime, 0f, 1f) * LabelOffset);

                GUIContent content = new(DamagePopupLabels[i].Amount.ToString());

                Vector2 contentSize = GUI.skin.label.CalcSize(content);

                float opacity = 1f - Math.Clamp((Time.time - DamagePopupLabels[i].LifeTime) / DamagePopupLabel.MaxLifeTime, 0f, 1f);

                opacity = Math.Clamp(opacity * 2f, 0f, 1f);

                Rect rect = new(screenPoint, contentSize);

                for (int j = 0; j < rects.Count; j++)
                {
                    Rect otherRect = rects[j];
                    if (rect.Overlaps(otherRect))
                    {
                        rect = new Rect(rect.xMin, otherRect.yMin - rect.height + 7f, rect.width, rect.height);
                    }
                }

                using (GUIUtils.ContentColor(Color.black.Opacity(opacity)))
                {
                    GUI.Label(new Rect(rect.position + new Vector2(1, 0), rect.size), content);
                    GUI.Label(new Rect(rect.position + new Vector2(-1, 0), rect.size), content);
                    GUI.Label(new Rect(rect.position + new Vector2(0, 1), rect.size), content);
                    GUI.Label(new Rect(rect.position + new Vector2(0, -1), rect.size), content);
                }

                using (GUIUtils.ContentColor((DamagePopupLabels[i].DamageKind switch
                {
                    DamageKind.Physical => PhysicalDamageColor,
                    DamageKind.Explosive => ExplosiveDamageColor,
                    _ => Color.white,
                }).Opacity(opacity)))
                {
                    rects.Add(rect);
                    GUI.Label(rect, content);
                }
            }
        }

        public void Add(Vector3 worldPosition, float amount, DamageKind damageKind)
        {
            (int Index, float DistanceSqr) closest = (-1, float.MaxValue);
            for (int i = 0; i < DamagePopupLabels.Count; i++)
            {
                if (DamagePopupLabels[i].DamageKind != damageKind) continue;

                float distSqr = (worldPosition - DamagePopupLabels[i].WorldPosition).sqrMagnitude;
                if (closest.DistanceSqr > distSqr)
                { closest = (i, distSqr); }
            }

            if (closest.DistanceSqr < 3f)
            {
                DamagePopupLabel closestBruh = DamagePopupLabels[closest.Index];
                closestBruh.LifeTime = Time.time;
                closestBruh.Amount += amount;
                closestBruh.WorldPosition = (closestBruh.WorldPosition + worldPosition) / 2;
                DamagePopupLabels[closest.Index] = closestBruh;
                return;
            }

            DamagePopupLabels.Add(new DamagePopupLabel(worldPosition, Time.time, amount, damageKind));
        }

        void FixedUpdate()
        {
            for (int i = DamagePopupLabels.Count - 1; i >= 0; i--)
            {
                if (Time.time - DamagePopupLabels[i].LifeTime >= DamagePopupLabel.MaxLifeTime)
                { DamagePopupLabels.RemoveAt(i); }
            }
        }
    }
}
