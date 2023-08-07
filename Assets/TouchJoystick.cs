using System;
using InputUtils;
using UnityEngine;

namespace Game.Managers
{
    public class TouchJoystick : SingleInstance<TouchJoystick>
    {
        AdvancedTouch Touch;

        Texture2D FilledCircle;
        Texture2D Circle;

        Vector2Int Origin;
        Vector2Int Pivot;

        Vector2Int Size;

        Vector2 RelativePosition;

        [SerializeField, ReadOnly] internal Vector2 NormalizedInput;
        internal bool IsActiveAndCaptured => Touch.IsActiveAndCaptured;

        void Start() => Initialize();

        void OnTouchDown(AdvancedTouch sender)
        {
            sender.IsCaptured = true;

            RelativePosition = GUIUtils.TransformPoint(sender.Position) - Origin;
            RelativePosition = Vector2.ClampMagnitude(RelativePosition, Size.x / 2);

            NormalizedInput = RelativePosition / (Size.x / 2);
            NormalizedInput = new Vector2(-NormalizedInput.y, NormalizedInput.x);
        }

        void OnTouchMove(AdvancedTouch sender)
        {
            RelativePosition = GUIUtils.TransformPoint(sender.Position) - Origin;
            RelativePosition = Vector2.ClampMagnitude(RelativePosition, Size.x / 2);

            NormalizedInput = RelativePosition / (Size.x / 2);
            NormalizedInput = new Vector2(-NormalizedInput.y, NormalizedInput.x);
        }

        void OnTouchUp(AdvancedTouch sender)
        {
            RelativePosition = Vector2.zero;
            NormalizedInput = Vector2.zero;
        }
        void OnTouchCancelled(AdvancedTouch sender)
        {
            RelativePosition = Vector2.zero;
            NormalizedInput = Vector2.zero;
        }

        void Initialize()
        {
            FilledCircle = GUIUtils.GenerateCircleFilled(Vector2Int.one * 128);
            Circle = GUIUtils.GenerateCircle(Vector2Int.one * 128);

            NormalizedInput = Vector2.zero;

            Refresh();

            Touch = new AdvancedTouch(55, InputCondition, new RectInt(Vector2Int.zero, Size));
            Touch.OnDown += OnTouchDown;
            Touch.OnMove += OnTouchMove;
            Touch.OnUp += OnTouchUp;
            Touch.OnCancelled += OnTouchCancelled;
        }

        void Refresh()
        {
            Size = Vector2Int.one * (Mathf.Min(Screen.width, Screen.height) / 4);

            Pivot = new(0, Screen.height - Size.y);
            Origin = Pivot + new Vector2Int(Size.x / 2, Size.y / 2);
        }

        void FixedUpdate() => Refresh();

        bool InputCondition()
        {
            return true;
        }

        void OnGUI()
        {
            GUI.DrawTexture(new Rect(Pivot, Size), Circle, ScaleMode.StretchToFill, true, 0f, Color.white, 0, 0);
            GUI.DrawTexture(new Rect(Origin + RelativePosition - (Size / 4), Size / 2), FilledCircle, ScaleMode.StretchToFill, true, 0f, Color.gray, 0, 0);
        }
    }
}
