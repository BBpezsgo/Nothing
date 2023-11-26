using UI;

using Unity.Netcode;

using UnityEngine;

#nullable enable

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

                    if (NetcodeUtils.IsServer)
                    { NetworkedVisible.Value = value; }
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

                    if (NetcodeUtils.IsServer)
                    { NetworkedText.Value = value ?? string.Empty; }
                }
            }
            get => GUIContent.text;
        }

        bool _visible = false;
        [Min(1f)] public float MaxDistance = 500f;
        public float Offset = 1f;

        GUIContent GUIContent = GUIContent.none;

        readonly NetworkVariable<Unity.Collections.FixedString32Bytes> NetworkedText = new(new Unity.Collections.FixedString32Bytes());
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

        void OnNetworkedTextChanged(Unity.Collections.FixedString32Bytes previousValue, Unity.Collections.FixedString32Bytes newValue)
        {
            GUIContent = string.IsNullOrEmpty(newValue.ToString()) ? GUIContent.none : new GUIContent(newValue.ToString());
        }

        public void Show(string text)
        {
            Visible = true;
            Text = text;
        }

        public void Hide()
        {
            Visible = false;
            Text = string.Empty;
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
