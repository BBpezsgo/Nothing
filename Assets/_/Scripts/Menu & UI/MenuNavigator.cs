using Game.Managers;

using System;

using UnityEngine;

using Utilities;

namespace Game.UI
{
    public class MenuNavigator : SingleInstance<MenuNavigator>
    {
        [ReadOnly] public bool IsPaused;

        PriorityKey KeyEsc;

        void Start()
        {
            KeyEsc = new PriorityKey(KeyCode.Escape, -10, EscKeyCondition);
            KeyEsc.OnDown += OnKeyEsc;
        }

        bool EscKeyCondition()
        {
            if (!MenuManager.AnyMenuVisible)
            { return true; }
            return MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Pause;
        }

        void OnKeyEsc()
        {
            IsPaused = !IsPaused;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_BlueprintManager)
                {
                    MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
                    return;
                }

                if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.None)
                {
                    MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_BlueprintManager;
                    return;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (MenuManager.Instance.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.Scenes)
                {
                    MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None;
                    return;
                }

                if (MenuManager.Instance.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.None &&
                    MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.None)
                {
                    MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.Scenes;
                    return;
                }
            }
            /*
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.Game_Research)
                {
                    MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None;
                    Research.ResearchManager.Instance.DeselectFacility();
                    return;
                }

                if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.None)
                {
                    MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_Research;
                    return;
                }
            }
            */
        }
    }
}
