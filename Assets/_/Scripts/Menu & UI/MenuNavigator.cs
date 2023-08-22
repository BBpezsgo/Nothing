using Game.Managers;

using UnityEngine;

namespace Game.UI
{
    public class MenuNavigator : SingleInstance<MenuNavigator>
    {
        [ReadOnly] public bool IsPaused;

        InputUtils.PriorityKey KeyEsc;

        void Start()
        {
            KeyEsc = new InputUtils.PriorityKey(KeyCode.Escape, -10, EscKeyCondition);
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

        readonly struct Hotkey
        {
            public enum MenuKind
            {
                MainMenu,
                IntermediateMenu,
            }

            public readonly KeyCode Key;

            public readonly MenuKind Kind;

            public readonly MenuManager.MainMenuType MainMenuType;
            public readonly MenuManager.IntermediateMenuType IntermediateMenuType;

            Hotkey(KeyCode key) : this()
            {
                Key = key;
            }

            public Hotkey(KeyCode key, MenuManager.MainMenuType menu) : this(key)
            {
                Kind = MenuKind.MainMenu;
                MainMenuType = menu;
            }

            public Hotkey(KeyCode key, MenuManager.IntermediateMenuType menu) : this(key)
            {
                Kind = MenuKind.IntermediateMenu;
                IntermediateMenuType = menu;
            }

            public void Toggle()
            {
                var menu = MenuManager.Instance;
                switch (Kind)
                {
                    case MenuKind.MainMenu:
                        if (menu.CurrentStatus != MenuManager.StatusType.None)
                        { break; }

                        if (menu.CurrentPanel != MenuManager.PanelType.None)
                        { break; }

                        if (menu.CurrentIntermediateMenu != MenuManager.IntermediateMenuType.None)
                        { break; }

                        if (menu.CurrentMenu == MenuManager.MainMenuType.None)
                        { menu.CurrentMenu = MainMenuType; }
                        else
                        { menu.CurrentMenu = MenuManager.MainMenuType.None; }

                        break;
                    case MenuKind.IntermediateMenu:
                        if (menu.CurrentStatus != MenuManager.StatusType.None)
                        { break; }

                        if (menu.CurrentPanel != MenuManager.PanelType.None)
                        { break; }

                        if (menu.CurrentIntermediateMenu == MenuManager.IntermediateMenuType.None)
                        { menu.CurrentIntermediateMenu = IntermediateMenuType; }
                        else
                        { menu.CurrentIntermediateMenu = MenuManager.IntermediateMenuType.None; }
                        break;
                    default:
                        break;
                }
            }
        }

        readonly Hotkey[] Hotkeys = new[]
        {
            new Hotkey(KeyCode.Alpha1, MenuManager.MainMenuType.Game_BlueprintManager),
            new Hotkey(KeyCode.Alpha2, MenuManager.IntermediateMenuType.Scenes),
        };

        void Update()
        {
            for (int i = 0; i < Hotkeys.Length; i++)
            {
                if (Input.GetKeyDown(Hotkeys[i].Key))
                { Hotkeys[i].Toggle(); }
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
