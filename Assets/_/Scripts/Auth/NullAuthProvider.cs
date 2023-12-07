using System;
using Game.Components;
using Game.Managers;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Authentication.Providers
{
    public class NullAuthProvider : MonoBehaviour, IAccountMenuProvider, IAuthProvider
    {
        [SerializeField] UIDocument loginMenu = null!;
        [SerializeField] MenuManager menuManager = null!;
        [SerializeField, ReadOnly] string? id;

        public string? DisplayName => null;
        public string? AvatarUrl => null;
        public string ID => id ?? string.Empty;
        public bool IsAuthorized => false;

        void Start()
        {
            loginMenu.OnEnabled().On += () =>
            {
#if UNITY_EDITOR
                loginMenu.rootVisualElement.Q<Button>("button-quit").clicked += () => UnityEditor.EditorApplication.isPlaying = false;
#else
                loginMenu.rootVisualElement.Q<Button>("button-quit").clicked += Application.Quit;
#endif
            };
            id = Guid.NewGuid().ToString();
        }

        void Close()
        { menuManager.CurrentPanel = MenuManager.PanelType.None; }

        public void Show()
        { /* MenuManager.Instance.CurrentPanel = MenuManager.PanelType.Login; */ }

        public void Login() => throw new NotImplementedException();
        public void Logout() => throw new NotImplementedException();
    }
}
