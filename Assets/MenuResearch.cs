using Game.Managers;
using Game.Research;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

namespace Game.UI
{
    public class MenuResearch : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;
        [SerializeField] VisualTreeAsset ResearchablePanel;

        ScrollView ScrollviewResearchables;
        Researchable[] researchables;

        List<ResearchableProgressbar> ResearchableProgressbars = new();

        PriorityKey KeyEsc;

        class ResearchableProgressbar
        {
            readonly ProgressBar ProgressBar;
            readonly Researchable Researchable;

            public ResearchableProgressbar(ProgressBar progressBar, Researchable researchable)
            {
                ProgressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
                Researchable = researchable ?? throw new ArgumentNullException(nameof(researchable));
            }

            internal void Update()
            {
                if (ProgressBar == null) return;
                if (Researchable == null) return;
                if (!Researchable.IsResearching) return;
                if (Researchable.IsResearched) return;
                ProgressBar.value = Researchable.ProgressPercent;
            }
        }

        void FixedUpdate()
        {
            for (int i = 0; i < ResearchableProgressbars.Count; i++)
            {
                ResearchableProgressbars[i].Update();
            }
        }

        void Start()
        {
            KeyEsc = new PriorityKey(KeyCode.Escape, 3, () => gameObject != null && gameObject.activeSelf);
            KeyEsc.OnDown += OnKeyEsc;
        }

        void OnKeyEsc()
        {
            MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
            ResearchManager.Instance.DeselectFacility();
        }

        void OnEnable()
        {
            UI = GetComponent<UIDocument>();

            UI.rootVisualElement.Q<Button>("button-close").clicked += ButtonClose;

            ScrollviewResearchables = UI.rootVisualElement.Q<ScrollView>("scrollview");
            ScrollviewResearchables.contentContainer.Clear();

            RefreshResearchables();
        }

        void OnDisable()
        {
            ResearchManager.Instance.DeselectFacility();
        }

        void ButtonClose()
        {
            MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
            ResearchManager.Instance.DeselectFacility();
        }

        internal void RefreshResearchables()
        {
            ResearchableProgressbars.Clear();

            ScrollviewResearchables.contentContainer.Clear();

            this.researchables = ResearchManager.Instance.Researchables;
            for (int i = 0; i < researchables.Length; i++)
            {
                TemplateContainer newElement = ResearchablePanel.Instantiate();

                newElement.Q<Label>("label-name").text = researchables[i].Name;

                if (researchables[i].IsResearching)
                {
                    newElement.Q<ProgressBar>("progressbar-progress").value = researchables[i].ProgressPercent;
                    newElement.Q<Button>().name = $"researchable-{i}";
                    newElement.Q<Button>().style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                    ResearchableProgressbars.Add(new ResearchableProgressbar(newElement.Q<ProgressBar>("progressbar-progress"), researchables[i]));
                }
                else
                {
                    newElement.Q<ProgressBar>("progressbar-progress").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                    newElement.Q<Button>().name = $"researchable-{i}";
                    newElement.Q<Button>().clickable.clickedWithEventInfo += ButtonResearchable;
                }

                ScrollviewResearchables.Add(newElement);
            }
        }

        void ButtonResearchable(EventBase e)
        {
            int i = int.Parse(((VisualElement)e.target).name.Split('-')[1]);

            bool success = ResearchManager.Instance.Research(researchables[i].ID);

            if (success)
            { RefreshResearchables(); }
        }
    }
}
