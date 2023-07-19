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
                { MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.None; }
                else if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.None)
                { MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_BlueprintManager; }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (MenuManager.Instance.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.Scenes)
                { MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None; }
                else if (MenuManager.Instance.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.None)
                { MenuManager.Instance.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.Scenes; }
            }
        }
    }
}
