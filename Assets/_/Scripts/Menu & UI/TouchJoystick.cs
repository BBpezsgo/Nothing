using System;
using InputUtils;
using UnityEngine;

namespace Game.Managers
{
    public class TouchJoystick : SingleInstance<TouchJoystick>
    {
        AdvancedTouch Touch;

        [SerializeField] Color BackgroundFillColor;

        [SerializeField] Color ForegroundFillColor;
        [SerializeField] Color ForegroundOutlineColor;

        [SerializeField] Color ForegroundFillColorActive;
        [SerializeField] Color ForegroundOutlineColorActive;

        Texture2D FilledCircle;
        Texture2D Circle;
        Texture2D Circle2;

        Vector2Int Origin;
        Vector2Int Pivot;

        Vector2Int Size;

        Vector2 RelativePosition;
        Vector2 LerpedRelativePosition;

        [SerializeField, ReadOnly] public Vector2 RawInput;
        [SerializeField, ReadOnly] public Vector2 WorldSpaceInput;
        public bool IsActiveAndCaptured => Touch.IsActiveAndCaptured;

        void Start() => Initialize();

        void OnTouchDown(AdvancedTouch sender)
        {
            sender.IsCaptured = true;

            RelativePosition = GUIUtils.TransformPoint(sender.Position) - Origin;
            RelativePosition = Vector2.ClampMagnitude(RelativePosition, Size.x / 2);

            CalculateInput();
        }

        void OnTouchMove(AdvancedTouch sender)
        {
            RelativePosition = GUIUtils.TransformPoint(sender.Position) - Origin;
            RelativePosition = Vector2.ClampMagnitude(RelativePosition, Size.x / 2);

            CalculateInput();
        }

        void CalculateInput()
        {
            Vector2 normalizedInput = RelativePosition / (Size.x / 2);
            normalizedInput *= new Vector2(1, -1);
            normalizedInput.Rotate(-45f);

            RawInput = normalizedInput;
            WorldSpaceInput = MainCamera.Camera.transform.TransformVector(normalizedInput.To3D()).To2D();

            // NormalizedInput = new Vector2(normalizedInput.x, -normalizedInput.y);
            // NormalizedInput2 = new Vector2(-normalizedInput.y, -normalizedInput.x);
        }

        void OnTouchUp(AdvancedTouch sender)
        {
            RelativePosition = default;
            RawInput = default;
            WorldSpaceInput = default;
        }
        void OnTouchCancelled(AdvancedTouch sender)
        {
            RelativePosition = default;
            RawInput = default;
            WorldSpaceInput = default;
        }

        void Initialize()
        {
            FilledCircle = GUIUtils.GenerateCircleFilled(Vector2Int.one * 128);
            Circle = GUIUtils.GenerateCircle(Vector2Int.one * 128);
            Circle2 = GUIUtils.GenerateCircle(Vector2Int.one * 128, 0.05f);

            RawInput = default;
            WorldSpaceInput = default;

            Refresh();

            Touch = new AdvancedTouch(55, InputCondition, new RectInt(default, Size));
            Touch.OnDown += OnTouchDown;
            Touch.OnMove += OnTouchMove;
            Touch.OnUp += OnTouchUp;
            Touch.OnCancelled += OnTouchCancelled;
        }

        void Refresh()
        {
            Size = Vector2Int.one * (Maths.Min(Screen.width, Screen.height) / 4);

            Pivot = new(0, Screen.height - Size.y);
            Origin = Pivot + new Vector2Int(Size.x / 2, Size.y / 2);
        }

        void FixedUpdate()
        {
            Refresh();
            LerpedRelativePosition = Vector2.Lerp(LerpedRelativePosition, RelativePosition, 30f * Time.fixedDeltaTime);
        }

        bool InputCondition()
        {
            return true;
        }

        void OnGUI()
        {
            GUI.DrawTexture(new Rect(Pivot, Size), Circle, ScaleMode.StretchToFill, true, 0f, BackgroundFillColor, 0, 0);

            if (IsActiveAndCaptured)
            {
                GUI.DrawTexture(new Rect(Origin + LerpedRelativePosition - (Size / 4), Size / 2), FilledCircle, ScaleMode.StretchToFill, true, 0f, ForegroundFillColorActive, 0, 0);
                GUI.DrawTexture(new Rect(Origin + LerpedRelativePosition - (Size / 4), Size / 2), Circle2, ScaleMode.StretchToFill, true, 0f, ForegroundOutlineColorActive, 0, 0);
            }
            else
            {
                GUI.DrawTexture(new Rect(Origin + LerpedRelativePosition - (Size / 4), Size / 2), FilledCircle, ScaleMode.StretchToFill, true, 0f, ForegroundFillColor, 0, 0);
                GUI.DrawTexture(new Rect(Origin + LerpedRelativePosition - (Size / 4), Size / 2), Circle2, ScaleMode.StretchToFill, true, 0f, ForegroundOutlineColor, 0, 0);
            }
        }
    }
}
