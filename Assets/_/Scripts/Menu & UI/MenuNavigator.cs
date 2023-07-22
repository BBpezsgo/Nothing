using Game.Managers;

using UnityEngine;

namespace Game.UI
{
    public class MenuNavigator : SingleInstance<MenuNavigator>
    {
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
        }
    }
}
