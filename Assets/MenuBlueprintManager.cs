using Blueprints;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

public class MenuBlueprintManager : MonoBehaviour
{
    [SerializeField, ReadOnly] UIDocument UI;
    [SerializeField, ReadOnly] MenuBlueprintDesigner BlueprintDesigner;
    [SerializeField] VisualTreeAsset BlueprintButton;
    [SerializeField, ReadOnly] ScrollView ScrollViewBlueprints;
    Blueprint[] Blueprints;

    PriorityKey KeyEsc;

    void OnEnable()
    {
        BlueprintDesigner = FindObjectOfType<MenuBlueprintDesigner>(true);
        UI = GetComponent<UIDocument>();
        UI.rootVisualElement.Q<Button>("button-close").clicked += ButtonClose;
        UI.rootVisualElement.Q<Button>("button-new").clicked += ButtonNew;
        ScrollViewBlueprints = UI.rootVisualElement.Q<ScrollView>("scrollview-blueprints");

        Blueprints = BlueprintManager.LoadBlueprints();
        RefreshBlueprints();
    }

    private void OnKeyEsc()
    {
        if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintManager)
        { MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None; }
    }

    void OnDisable()
    {
        if (UI.rootVisualElement != null)
        {
            UI.rootVisualElement.Q<Button>("button-close").clicked -= ButtonClose;
            UI.rootVisualElement.Q<Button>("button-new").clicked -= ButtonNew;
        }
    }

    void Start()
    {
        KeyEsc = new PriorityKey(KeyCode.Escape, 5, () => MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintManager);
        KeyEsc.OnDown += OnKeyEsc;
    }

    void RefreshBlueprints()
    {
        ScrollViewBlueprints.contentContainer.Clear();
        for (int i = 0; i < Blueprints.Length; i++)
        {
            TemplateContainer newElement = BlueprintButton.Instantiate();
            newElement.Q<Label>("label-name").text = Blueprints[i].Name;

            Button buttonEdit = newElement.Q<Button>("button-edit");
            buttonEdit.name = $"blueprint-{i}";
            buttonEdit.clickable.clickedWithEventInfo += ButtonEditBlueprint;

            Button buttonDelete = newElement.Q<Button>("button-delete");
            buttonDelete.name = $"blueprint-{i}";
            buttonDelete.clickable.clickedWithEventInfo += ButtonDeleteBlueprint;

            ScrollViewBlueprints.Add(newElement);
        }
    }

    void ButtonDeleteBlueprint(EventBase obj)
    {
        int i = int.Parse(((VisualElement)obj.target).name.Split('-')[1]);

    }

    void ButtonEditBlueprint(EventBase obj)
    {
        int i = int.Parse(((VisualElement)obj.target).name.Split('-')[1]);
        MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_BlueprintDesigner;
        BlueprintDesigner.OnStartLoadBlueprint = true;
        BlueprintDesigner.CurrentBlueprint = Blueprints[i];
    }

    void ButtonNew()
    {
        MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_BlueprintDesigner;
        BlueprintDesigner.OnStartCreateNewBlueprint = true;
    }

    void ButtonClose()
    {
        MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
    }
}
