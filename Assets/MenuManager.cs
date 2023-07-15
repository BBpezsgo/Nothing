using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : SingleInstance<MenuManager>
{
    internal enum MainMenuType
    {
        None,

        /// <summary>
        /// <b>Managed automatically</b>
        /// </summary>
        Main,

        /// <summary>
        /// <b>Managed automatically</b>
        /// </summary>
        Lobby,

        /// <summary>
        /// <b>Managed automatically</b>
        /// </summary>
        Room,

        /// <summary>
        /// <b>Managed automatically</b>
        /// </summary>
        Login,

        Game_BlueprintManager,

        Game_BlueprintDesigner,
    }

    internal enum IntermediateMenuType
    {
        None,

        Scenes,
    }

    internal enum StatusType
    {
        None,

        LoadingScene,

        /// <summary>
        /// <b>Managed automatically</b>
        /// </summary>
        LoadingNetwork,
    }

    internal enum PanelType
    {
        None,
    }

    [System.Serializable]
    public class MainMenu<T> where T : System.Enum
    {
        [Header("Info")]

        [SerializeField] internal T Type;

        [SerializeField] CanvasGroup group;
        [SerializeField] UIDocument uiDocument;

        [SerializeField, ReadOnly] internal bool Enabled = false;
        [SerializeField, ReadOnly] bool Active = false;

        [Header("Debug")]
        [SerializeField, ReadOnly] Transform transform;
        GameObject gameObject => uiDocument != null ? uiDocument.gameObject : group != null ? group.gameObject : null;
        // [SerializeField, ReadOnly] float alpha;
        // [SerializeField, ReadOnly] Vector3 startScale = Vector3.one;

        void SetTransform()
        {
            // this.transform = gameObject.GetComponent<RectTransform>();
            // if (this.transform == null)
            //{ this.transform = gameObject.GetComponent<Transform>(); }
            // this.startScale = transform.localScale;
        }

        public void Update(float deltaTime, float lerpAmmount, float minScale)
        {
            if (transform == null) SetTransform();

            if (Enabled)
            {
                if (!Active) SetActive(true);
                // this.alpha = Mathf.Lerp(this.alpha, 1f, deltaTime * lerpAmmount);
                // transform.localScale = startScale * (this.alpha * minScale + (1 - minScale));
            }
            else
            {
                if (Active) SetActive(false);
                // this.alpha = Mathf.Lerp(this.alpha, 0f, deltaTime * lerpAmmount);
                // transform.localScale = startScale * (this.alpha * minScale + (1 - minScale));
            }
            SetUI();
        }

        void SetActive(bool active)
        {
            if (this.gameObject != null) this.gameObject.SetActive(active);
            Active = active;
        }

        void SetUI()
        {
            // if (this.group != null) this.group.alpha = this.alpha;
            if (this.uiDocument != null) if (this.uiDocument.rootVisualElement != null)
                {
                    var element = this.uiDocument.rootVisualElement.Q("root");
                    if (Enabled)
                    {
                        if (!element.ClassListContains("visible"))
                        { element.AddToClassList("visible"); }
                    }
                    else
                    {
                        if (element.ClassListContains("visible"))
                        { element.RemoveFromClassList("visible"); }
                    }
                }
        }
    }

    public float lerpAmmount = 2f;
    [Range(0, 1)]
    public float minScale = .5f;

    [SerializeField] List<MainMenu<MainMenuType>> mainMenus = new();
    [SerializeField] List<MainMenu<IntermediateMenuType>> intermediateMenus = new();
    [SerializeField] List<MainMenu<StatusType>> statusPanels = new();
    [SerializeField] List<MainMenu<PanelType>> panels = new();

    MainMenu<MainMenuType>[] _mainMenus = null;
    MainMenu<IntermediateMenuType>[] _intermediateMenus = null;
    MainMenu<StatusType>[] _statusPanels = null;
    MainMenu<PanelType>[] _panels = null;

    [SerializeField] MenuStateProvider stateProvider;

    [Header("Debug")]
    [SerializeField, ReadOnly] MainMenuType currentMenu = MainMenuType.None;
    [SerializeField, ReadOnly] IntermediateMenuType currentIntermediateMenu = IntermediateMenuType.None;
    [SerializeField, ReadOnly] StatusType currentStatusType = StatusType.None;
    [SerializeField, ReadOnly] PanelType currentPanel = PanelType.None;

    [field: ReadOnly]
    [field: SerializeField] internal bool AnyPopupVisible { get; set; } = false;

    [SerializeField, ReadOnly] bool _anyMenuVisible = false;
    internal static bool AnyMenuVisible => Instance == null ? false : Instance._anyMenuVisible;

    internal MainMenuType CurrentMenu
    {
        get
        {
            if (AnyPopupVisible) return MainMenuType.None;

            if (CurrentIntermediateMenu != IntermediateMenuType.None) return MainMenuType.None;
            if (CurrentStatus != StatusType.None) return MainMenuType.None;
            if (CurrentPanel != PanelType.None) return MainMenuType.None;

            if (!stateProvider.Authorized) return MainMenuType.Login;

            if (stateProvider.InNetcodeRoom)
            {
                if (Input.GetKey(KeyCode.Alpha0))
                { return MainMenuType.Room; }
            }
            else if (!stateProvider.IsOffline)
            {
                return MainMenuType.Lobby;
            }

            return currentMenu;
        }
        set => currentMenu = value;
    }
    internal IntermediateMenuType CurrentIntermediateMenu
    {
        get
        {
            if (AnyPopupVisible) return IntermediateMenuType.None;

            if (CurrentStatus != StatusType.None) return IntermediateMenuType.None;
            if (CurrentPanel != PanelType.None) return IntermediateMenuType.None;

            return currentIntermediateMenu;
        }
        set => currentIntermediateMenu = value;
    }
    internal StatusType CurrentStatus
    {
        get
        {
            if (AnyPopupVisible) return StatusType.None;

            if (CurrentPanel != PanelType.None) return StatusType.None;

            if (stateProvider.NetcodeIsLoading) return StatusType.LoadingNetwork;

            return currentStatusType;
        }
        set
        {
            currentStatusType = value;
        }
    }
    internal PanelType CurrentPanel
    {
        private get
        {
            if (AnyPopupVisible) return PanelType.None;

            return currentPanel;
        }
        set => currentPanel = value;
    }

    void UpdateMenus<T>(MainMenu<T>[] menus, T currentMenu) where T : System.Enum
    {
        for (int i = 0; i < menus.Length; i++)
        {
            menus[i].Enabled = currentMenu.Equals(menus[i].Type);
            menus[i].Update(Time.deltaTime, lerpAmmount, minScale);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _mainMenus = mainMenus.ToArray();
        _intermediateMenus = intermediateMenus.ToArray();
        _statusPanels = statusPanels.ToArray();
        _panels = panels.ToArray();
    }

    void Update()
    {
        UpdateMenus(_mainMenus, CurrentMenu);
        UpdateMenus(_intermediateMenus, CurrentIntermediateMenu);
        UpdateMenus(_panels, CurrentPanel);
        UpdateMenus(_statusPanels, CurrentStatus);

        _anyMenuVisible =
            CurrentPanel != PanelType.None ||
            CurrentStatus != StatusType.None ||
            CurrentIntermediateMenu != IntermediateMenuType.None ||
            CurrentMenu != MainMenuType.None;
    }
}
