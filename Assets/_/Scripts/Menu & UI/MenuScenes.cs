using Game.Managers;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

namespace Game.UI
{
    public class MenuScenes : MonoBehaviour
    {
        [SerializeField, ReadOnly] UIDocument UI;
        [SerializeField] VisualTreeAsset SceneElement;

        PriorityKey KeyEsc;

        void Start()
        {
            KeyEsc = new PriorityKey(KeyCode.Escape, 10, InputCondition);
            KeyEsc.OnDown += OnKeyEsc;
        }

        private bool InputCondition()
        {
            return MenuManager.Instance.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.Scenes;
        }

        void OnKeyEsc()
        {
            if (MenuManager.Instance.CurrentIntermediateMenu != MenuManager.IntermediateMenuType.Scenes)
            { return; }

            MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None;
        }

        void OnEnable()
        {
            UI = GetComponent<UIDocument>();
            UI.rootVisualElement.Q<Button>("button-back").clicked += ButtonBack;

            ScrollView scrollview = UI.rootVisualElement.Q<ScrollView>();
            scrollview.contentContainer.Clear();

            for (int i = 0; i < SceneManager.Scenes.Count; i++)
            {
                TemplateContainer newElement = SceneElement.Instantiate();

                newElement.Q<Label>().text = SceneManager.Scenes[i];

                newElement.Q<Button>().name = SceneManager.Scenes[i];
                newElement.Q<Button>().clickable.clickedWithEventInfo += OnSceneClick;

                scrollview.Add(newElement);
            }
        }

        private void ButtonBack()
        {
            if (MenuManager.Instance.CurrentIntermediateMenu != MenuManager.IntermediateMenuType.Scenes)
            { return; }

            MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None;
        }

        void OnSceneClick(EventBase e)
        {
            string scene = ((VisualElement)e.target).name;

            SceneManager.LoadScene(scene);

            if (MenuManager.Instance.CurrentIntermediateMenu != MenuManager.IntermediateMenuType.Scenes)
            { return; }

            MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None;
        }
    }
}
