using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UIElements;

public class MenuHotkeys : SingleInstance<MenuHotkeys>
{
    [Serializable]
    internal struct Hotkey
    {
        public string Key;
        public string Tooltip;

        public Hotkey(string key, string tooltip)
        {
            Key = key;
            Tooltip = tooltip;
        }
    }

    [SerializeField, ReadOnly] UIDocument UI;
    [SerializeField] VisualTreeAsset HotkeyButton;
    [SerializeField] internal Hotkey[] Hotkeys = new Hotkey[0]; 

    protected override void Awake()
    {
        base.Awake();
        UI = GetComponent<UIDocument>();
    }

    void Start()
    {
        StartCoroutine(Refresh());
    }

    internal IEnumerator Refresh()
    {
        VisualElement content = UI.rootVisualElement.Q<VisualElement>("root");
        content.Clear();

        for (int i = 0; i < Hotkeys.Length; i++)
        {
            Hotkey hotkey = Hotkeys[i];
            TemplateContainer newButton = HotkeyButton.Instantiate();
            content.Add(newButton);
            Label labelKey = newButton.Q<Label>("label-key");
            Label labelTooltip = newButton.Q<Label>("label-tooltip");

            labelKey.text = hotkey.Key;
            labelTooltip.text = hotkey.Tooltip;

            yield return new WaitForFixedUpdate();

            Vector2 size = labelTooltip.MeasureTextSize(hotkey.Tooltip, 100f, VisualElement.MeasureMode.Undefined, 18f, VisualElement.MeasureMode.Exactly);

            labelTooltip.style.width = size.x;
        }

        yield break;
    }
}
