using System;
using Game;
using Game.Managers;
using InputUtils;
using UnityEngine;

public class QuickCommandManager : SingleInstance<QuickCommandManager>
{
    [SerializeField] bool IsEnabled = true;

    [SerializeField] Color CircleBackground = Color.gray;
    [SerializeField] Color CommandBackground = Color.white;
    [SerializeField] Color CommandSelectedBackground = Color.red;

    Texture2D SphereFilled;
    Texture2D Sphere;
    [SerializeField] int Size = 120;
    [SerializeField] float CircleThicknessValue = .25f;
    [SerializeField, Min(.0000001f)] float ActionScale = 1f;
    [SerializeField] float ShowSpeed = 10f;

    Vector2 Origin;
    Vector3 WorldPosition;

    [SerializeField, Button(nameof(RegenerateTextures), false, true, "Regenerate Textures")] string btnRegenerateTextures;

    int OuterRadius => Size / 2;
    float MiddleRadius => OuterRadius - (OuterRadius * CircleThicknessValue);
    float InnerRadius => OuterRadius - (OuterRadius * CircleThicknessValue * 2);

    public const float HOLD_TIME_REQUIREMENT = .35f;
    public bool IsShown;
    float ShownAt;

    AdvancedMouse LeftMouse;

    void Start()
    {
        RegenerateTextures();

        LeftMouse = new AdvancedMouse(Mouse.Left, 14, MouseCondition, HOLD_TIME_REQUIREMENT);
        LeftMouse.OnClick += LeftMouse_OnClick;
    }

    internal void RegenerateTextures()
    {
        if (SphereFilled != null)
        { Texture2D.Destroy(SphereFilled); }
        SphereFilled = GUIUtils.GenerateCircleFilled(Vector2Int.one * 128);

        if (Sphere != null)
        { Texture2D.Destroy(Sphere); }
        Sphere = GUIUtils.GenerateCircle(Vector2Int.one * 128, CircleThicknessValue);
    }

    void Update()
    {
        if (!IsEnabled) return;

        if (LeftMouse.IsDragging)
        {
            LeftMouse.Reset();
        }

        if (IsShown && Input.GetMouseButtonUp(Mouse.Left))
        {
            OnPicked();
            IsShown = false;
        }

        if (IsShown && (
            Input.GetMouseButtonDown(Mouse.Right) ||
            Input.GetKeyDown(KeyCode.Escape)
            ))
        {
            IsShown = false;
        }
    }

    void OnPicked()
    {

    }

    bool MouseCondition() =>
        IsEnabled &&
        CameraController.Instance != null &&
        (!CameraController.Instance.IsFollowing || CameraController.Instance.JustFollow) &&
        !TakeControlManager.Instance.IsControlling &&
        !BuildingManager.Instance.IsBuilding;

    void LeftMouse_OnClick(AdvancedMouse sender)
    {
        if (sender.HoldTime < HOLD_TIME_REQUIREMENT) return;
        Origin = AdvancedMouse.Position;
        ShownAt = Time.unscaledTime;
        WorldPosition = MainCamera.Camera.ScreenToWorldPosition(AdvancedMouse.Position);

        IsShown = true;
    }

    void OnGUI()
    {
        if (!IsShown) return;

        float showTime = Time.unscaledTime - ShownAt;
        float animationTime = Math.Clamp(showTime * ShowSpeed, 0f, 1f);

        Vector2 center = Origin;
        center.y = Screen.height - center.y;
        string[] commands = new string[]
        {
            "bruh 1",
            "bruh 2",
            "bruh 3",
        };

        {
            Vector2 size = animationTime * Size * Vector2.one;
            GUI.DrawTexture(RectUtils.FromCenter(center, size), Sphere, ScaleMode.StretchToFill, true, 0f, CircleBackground.Opacity(animationTime), 0f, 0f);
        }

        Vector2 mousePosition = (Vector2)Input.mousePosition;
        mousePosition.y = Screen.height - mousePosition.y;

        Vector2 mouseOffset = center - mousePosition;

        int selected = -1;

        if (Vector2.Distance(mousePosition, center) >= InnerRadius)
        {
            Vector2 mouseDirection = mouseOffset.normalized;
            float smallestDot = float.MaxValue;
            for (int i = 0; i < commands.Length; i++)
            {
                float rad = 2f * MathF.PI * (((float)i) / ((float)commands.Length));
                float x = MathF.Cos(rad);
                float y = MathF.Sin(rad);
                Vector2 direction = new(x, y);
                float dot = Vector2.Dot(direction, mouseDirection);
                if (dot < smallestDot)
                {
                    smallestDot = dot;
                    selected = i;
                }
            }
        }

        for (int i = 0; i < commands.Length; i++)
        {
            float normalizedIndex = (float)i / (float)commands.Length;
            float localAnimationTime = Math.Clamp(((showTime - (normalizedIndex / ShowSpeed)) * ShowSpeed) - .3f, 0f, 1f);

            float rad = 2 * MathF.PI * normalizedIndex;
            float x = MathF.Cos(rad);
            float y = MathF.Sin(rad);
            Vector2 direction = new(x, y);
            Vector2 point = MiddleRadius * localAnimationTime * direction;

            Vector2 size = CircleThicknessValue * Size * ActionScale * localAnimationTime * Vector2.one;

            if (selected == i)
            {
                GUI.DrawTexture(RectUtils.FromCenter(center + point, size), SphereFilled, ScaleMode.StretchToFill, true, 0f, CommandSelectedBackground, 0f, 0f);
            }
            else
            {
                GUI.DrawTexture(RectUtils.FromCenter(center + point, size), SphereFilled, ScaleMode.StretchToFill, true, 0f, CommandBackground, 0f, 0f);
            }
        }

        {
            Vector3 projectedWorldPosition = MainCamera.Camera.WorldToViewportPoint(WorldPosition);

            bool outOfScreen = false;
            if (projectedWorldPosition.x <= 0)
            {
                outOfScreen = true;
                projectedWorldPosition.x = 0;
            }
            if (projectedWorldPosition.y <= 0)
            {
                outOfScreen = true;
                projectedWorldPosition.y = 0;
            }
            if (projectedWorldPosition.x >= 1)
            {
                outOfScreen = true;
                projectedWorldPosition.x = 1;
            }
            if (projectedWorldPosition.y >= 1)
            {
                outOfScreen = true;
                projectedWorldPosition.y = 1;
            }
            if (projectedWorldPosition.z < 0)
            {
                outOfScreen = true;
                projectedWorldPosition.x = 1 - projectedWorldPosition.x;
                projectedWorldPosition.y = 1 - projectedWorldPosition.y;

                if (projectedWorldPosition.x < projectedWorldPosition.y &&
                    projectedWorldPosition.x < .5)
                {
                    projectedWorldPosition.x = 0;
                }
                else if (projectedWorldPosition.y < projectedWorldPosition.x &&
                    projectedWorldPosition.y < .5)
                {
                    projectedWorldPosition.y = 0;
                }
                else if (projectedWorldPosition.x > projectedWorldPosition.y &&
                    projectedWorldPosition.x >= .5)
                {
                    projectedWorldPosition.x = 1;
                }
                else if (projectedWorldPosition.y > projectedWorldPosition.x &&
                    projectedWorldPosition.y >= .5)
                {
                    projectedWorldPosition.y = 1;
                }
            }

            projectedWorldPosition.y = 1 - projectedWorldPosition.y;

            projectedWorldPosition.x *= Screen.width;
            projectedWorldPosition.y *= Screen.height;

            Vector2 diff = (Vector2)projectedWorldPosition - center;
            float diffD = diff.sqrMagnitude;

            if (diffD > MathF.Pow(OuterRadius, 2))
            {
                Vector2 startP = center + (diff.normalized * OuterRadius);

                GL.PushMatrix();

                if (GLUtils.SolidMaterial.SetPass(0))
                {
                    GLUtils.DrawLine(projectedWorldPosition, startP, 2f, new Color(0f, 0f, 0f, .5f));
                }

                GL.PopMatrix();
            }

            if (outOfScreen)
            {
                GUI.DrawTexture(RectUtils.FromCenter(projectedWorldPosition, Vector2.one * 32), SphereFilled, ScaleMode.StretchToFill, true, 0f, Color.red, 0f, 0f);
            }
        }
    }
}
