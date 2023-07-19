using System;

using UI;

using Unity.Netcode;

using UnityEngine;

namespace Game.UI.Components
{
    public class HoveringLabel : NetworkBehaviour
    {
        public bool Visible
        {
            set
            {
                if (NetcodeUtils.IsOfflineOrServer)
                {
                    _visible = value;
                    NetworkedVisible.Value = value;
                }
            }
            get => _visible;
        }
        public string Text
        {
            set
            {
                if (NetcodeUtils.IsOfflineOrServer)
                {
                    GUIContent = (value == null) ? GUIContent.none : new GUIContent(value);
                    NetworkedText.Value = value ?? "";
                }
            }
            get => GUIContent.text ?? null;
        }

        bool _visible = false;
        [Min(1f)] public float MaxDistance = 500f;
        public float Offset = 1f;

        GUIContent GUIContent = GUIContent.none;

        readonly NetworkVariable<NetworkString> NetworkedText = new(new NetworkString());
        readonly NetworkVariable<bool> NetworkedVisible = new(false);

        void Start()
        {
            NetworkedText.OnValueChanged += OnNetworkedTextChanged;
            NetworkedVisible.OnValueChanged += OnNetworkedVisibleChanged;
        }

        void OnNetworkedVisibleChanged(bool previousValue, bool newValue)
        {
            _visible = newValue;
        }

        void OnNetworkedTextChanged(NetworkString previousValue, NetworkString newValue)
        {
            GUIContent = string.IsNullOrEmpty(newValue.ToString()) ? GUIContent.none : new GUIContent(newValue);
        }

        public void Show(string text)
        {
            Visible = true;
            Text = text;
        }

        public void Hide()
        {
            Visible = false;
            Text = null;
        }

        void OnGUI()
        {
            if (!Visible || string.IsNullOrWhiteSpace(Text))
            { return; }

            Vector3 point = MainCamera.Camera.WorldToScreenPoint(transform.position + new Vector3(0f, Offset, 0f));

            if (point.x < 0 || point.y < 0 ||
                point.x > Screen.width || point.y > Screen.height ||
                point.z < 0)
            { return; }

            float distance = 1f - (point.z / MaxDistance);

            if (distance >= 1f)
            { return; }

            GUIStyle style = IMGUIManager.Instance.Skin.GetStyle("ingame-name-label");

            GUI.color = new Color(1f, 1f, 1f, distance);

            Vector2 size = style.CalcSize(GUIContent);

            point.x -= size.x / 2;
            point.y = Screen.height - point.y - size.y;

            Rect rect = new(point, size);

            GUI.Label(rect, GUIContent, style);

            GUI.color = Color.white;
        }
    }
}
