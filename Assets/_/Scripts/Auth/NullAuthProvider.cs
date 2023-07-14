using System;

using UnityEngine;
using UnityEngine.UIElements;

public class NullAuthProvider : MonoBehaviour, IAccountMenuProvider, IAuthProvider
{
    [SerializeField] UIDocument loginMenu;
    [SerializeField] MenuManager menuManager;
    [SerializeField, ReadOnly] string id;

    public string DisplayName => AuthManager.USERNAME_NULL;
    public string AvatarUrl => null;
    public string ID => id;
    public bool IsAuthorized => false;

    void Start()
    {
        loginMenu.OnEnabled().onEnable += () =>
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
