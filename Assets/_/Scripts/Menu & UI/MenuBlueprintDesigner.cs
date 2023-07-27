using Game.Blueprints;

using Game.Managers;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class MenuBlueprintDesigner : SingleInstance<MenuBlueprintDesigner>
    {
        [SerializeField, ReadOnly] UIDocument UI;
        [SerializeField] VisualTreeAsset PartButton;

        [SerializeField, ReadOnly, NonReorderable] BlueprintPart[] Parts = new BlueprintPart[0];

        [SerializeField, ReadOnly] internal Blueprint CurrentBlueprint = null;
        [SerializeField, ReadOnly] ScrollView ScrollViewAvaliableParts;
        [SerializeField, ReadOnly] ScrollView ScrollViewUsedParts;
        [SerializeField, ReadOnly] TextField TextFieldName;

        [SerializeField, ReadOnly] internal bool OnStartCreateNewBlueprint = false;
        [SerializeField, ReadOnly] internal bool OnStartLoadBlueprint = false;

        InputUtils.PriorityKey KeyEsc;

        void OnEnable()
        {
            UI = GetComponent<UIDocument>();

            UI.rootVisualElement.Q<Button>("button-close").clicked += ButtonClose;
            UI.rootVisualElement.Q<Button>("button-save").clicked += ButtonSave;

            ScrollViewAvaliableParts = UI.rootVisualElement.Q<ScrollView>("scrollview-parts");
            ScrollViewUsedParts = UI.rootVisualElement.Q<ScrollView>("scrollview-usedparts");
            TextFieldName = UI.rootVisualElement.Q<TextField>("textfield-name");

            List<BlueprintPart> parts = new();
            parts.AddRange(BlueprintManager.BuiltinParts.Bodies);
            parts.AddRange(BlueprintManager.BuiltinParts.Turrets);
            parts.AddRange(BlueprintManager.BuiltinParts.Controllers);
            parts.AddRange(BlueprintManager.LoadParts());
            Parts = parts.ToArray();

            if (OnStartCreateNewBlueprint)
            {
                OnStartCreateNewBlueprint = false;
                OnStartLoadBlueprint = false;
                CreateNewBlueprint();
            }

            if (OnStartLoadBlueprint)
            {
                OnStartCreateNewBlueprint = false;
                OnStartLoadBlueprint = false;
                TextFieldName.value = CurrentBlueprint.Name;
                RefreshUsedParts();
            }

            RefreshAvaliableParts();
        }

        void Start()
        {
            KeyEsc = new InputUtils.PriorityKey(KeyCode.Escape, 4, () => MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintDesigner);
            KeyEsc.OnDown += OnKeyEsc;
        }

        void OnKeyEsc()
        {
            if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintDesigner)
            { MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None; }
        }

        void OnDisable()
        {
            if (UI.rootVisualElement != null)
            {
                UI.rootVisualElement.Q<Button>("button-close").clicked -= ButtonClose;
                UI.rootVisualElement.Q<Button>("button-save").clicked -= ButtonSave;
            }
        }

        private void ButtonSave()
        {
            CurrentBlueprint.Name = TextFieldName.value;
            BlueprintManager.SaveBlueprint(CurrentBlueprint);
        }

        private void ButtonClose()
        {
            MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
        }

        BlueprintPart FindPart(string id)
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i].ID == id) return Parts[i];
            }
            return null;
        }

        internal void RefreshAvaliableParts()
        {
            ScrollViewAvaliableParts.contentContainer.Clear();
            for (int i = 0; i < Parts.Length; i++)
            {
                TemplateContainer newElement = PartButton.Instantiate();

                newElement.Q<Label>("label-name").text = Parts[i].Name;

                if (Parts[i].Image != null)
                { newElement.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(Parts[i].Image); }

                newElement.Q<Button>().text = "Add";
                newElement.Q<Button>().name = $"part-{i}";
                newElement.Q<Button>().clickable.clickedWithEventInfo += ButtonAvaliablePart;

                ScrollViewAvaliableParts.Add(newElement);
            }
        }

        private void ButtonAvaliablePart(EventBase obj)
        {
            int i = int.Parse(((VisualElement)obj.target).name.Split('-')[1]);
            CurrentBlueprint.Parts.Add(Parts[i].ID);
            RefreshAvaliableParts();
            RefreshUsedParts();
        }

        internal void RefreshUsedParts()
        {
            ScrollViewUsedParts.contentContainer.Clear();
            for (int i = 0; i < CurrentBlueprint.Parts.Count; i++)
            {
                TemplateContainer newElement = PartButton.Instantiate();
                BlueprintPart part = FindPart(CurrentBlueprint.Parts[i]);

                if (part == null)
                { newElement.Q<Label>("label-name").text = $"NOT FOUND"; }
                else
                {
                    newElement.Q<Label>("label-name").text = part.Name;

                    if (part.Image != null)
                    { newElement.Q<VisualElement>("image-icon").style.backgroundImage = new StyleBackground(part.Image); }
                }

                newElement.Q<Button>().text = "Remove";
                newElement.Q<Button>().name = $"part-{i}";
                newElement.Q<Button>().clickable.clickedWithEventInfo += ButtonUsedPart;

                ScrollViewUsedParts.Add(newElement);
            }
        }

        private void ButtonUsedPart(EventBase obj)
        {
            int partIndex = int.Parse(((VisualElement)obj.target).name.Split('-')[1]);
            CurrentBlueprint.Parts.RemoveAt(partIndex);
            RefreshAvaliableParts();
            RefreshUsedParts();
        }

        internal void CreateNewBlueprint()
        {
            CurrentBlueprint = new Blueprint();
            TextFieldName.value = CurrentBlueprint.Name;
            RefreshUsedParts();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintDesigner)
            { MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None; }
        }
    }
}
