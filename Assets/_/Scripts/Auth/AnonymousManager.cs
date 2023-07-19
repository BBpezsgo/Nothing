using DataUtilities.ReadableFileFormat;
using DataUtilities.Serializer;

using Game.Components;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace Authentication.Providers
{
    public class AnonymousManager : SingleInstance<AnonymousManager>, IModifiableAuthProvider, IFriendsProvider, IAccountMenuProvider
    {
        const string fileName = "anonymous";

        public bool IsAuthorized { get; private set; } = false;
        public Sprite Avatar => null;
        public string DisplayName
        {
            get => AccountData.DisplayName;
            set
            {
                AccountData.DisplayName = value;
                // Save();
            }
        }
        public string AvatarUrl
        {
            get => null;
            set { }
        }
        public string ID => AccountData.ID;

        [SerializeField, ReadOnly] string dataFilePath;
        string DataFilePath
        {
            get
            {
                dataFilePath = $"{AssetManager.Storage.Path}/users/{fileName}.bin";
                return dataFilePath;
            }
        }

        [SerializeField, ReadOnly] AnonymousUser AccountData;

        [SerializeField] bool AutoAuthorize = false;

        [Header("UI")]
        [SerializeField] UIDocument loginMenu;
        [SerializeField] UIDocument accountMenu;

        bool HaveAccount => System.IO.File.Exists(dataFilePath);

        void Start()
        {
            ResetHaveAccount();

            loginMenu.OnEnabled().onEnable += () =>
            {
                loginMenu.rootVisualElement.Q<Button>("button-anonymous").clicked += Login;
                ResetHaveAccount();
            };

            accountMenu.OnEnabled().onEnable += () =>
            {
                /*
                accountMenu.rootVisualElement.Q<Button>("btn-save").clicked += OnButtonUpdateAccount;
                // accountMenu.rootVisualElement.Q<Button>("btn-close").clicked += MenuNavigator.Instance.OnButtonAccountCloseClick;
                accountMenu.rootVisualElement.Q<TextField>("inp-name").value = AuthManager.AuthProvider.DisplayName;
                accountMenu.rootVisualElement.Q<TextField>("inp-name").RegisterValueChangedCallback(e =>
                {
                    // accountMenu.rootVisualElement.Q<ButtonThatCanBeDisabled>("btn-save").enabled = !string.IsNullOrEmpty(e.newValue);
                });
                // accountMenu.rootVisualElement.Q<ButtonThatCanBeDisabled>("btn-save").enabled = false;
                */
            };

            if (AutoAuthorize)
            {
                Login();
            }
        }

        float timeToNextResetHaveAccount = 0f;
        void FixedUpdate()
        {
            timeToNextResetHaveAccount += Time.fixedDeltaTime;
            if (timeToNextResetHaveAccount >= 1f)
            {
                timeToNextResetHaveAccount = 0f;
                ResetHaveAccount();
            }
        }

        void ResetHaveAccount()
        {
            // if (loginMenu.rootVisualElement != null) loginMenu.rootVisualElement.Q<VisualElement>("ico-anonymous").visible = HaveAccount;
        }

        #region UI Callback

        public void OnButtonUpdateAccount()
        {
            DisplayName = accountMenu.rootVisualElement.Q<TextField>("inp-name").value;
        }

        public void OnButtonLogout() { }

        public void Login()
        {
            /*
            if (HaveAccount)
            {
                AccountData = AssetManager.Storage.ReadObject<AnonymousUser>(DataFilePath);
            }
            else
            {
            */
            AccountData = new AnonymousUser()
            {
                ID = "anonymous-" + System.Guid.NewGuid().ToString(),
            };
            // Save();
            // }

            // Debug.Log($"[{nameof(AnonymousManager)}]: Login anonymous {{ UserID: {AccountData.ID} }}");

            IsAuthorized = true;
            AuthManager.OnAuthorized?.Invoke(this);
        }

        void Save() => AssetManager.Storage.Write(AccountData, DataFilePath);

        public void Logout()
        {

        }

        #endregion

        public string[] GetFriends() => AccountData.Friends.ToArray();
        public void AddFriend(string newFriendID)
        {
            throw new System.NotImplementedException();
            // AccountData.Friends.Add(newFriendID);
            // Save();
        }
        public void RemoveFriend(string userId)
        {
            throw new System.NotImplementedException();
            // AccountData.Friends.Remove(userId);
            // Save();
        }

        public void Show()
        {
            // MenuManager.Singleton.CurrentPanel = MenuManager.PanelType.AccountAnonymous;
            if (accountMenu.rootVisualElement != null)
            { accountMenu.rootVisualElement.Q<TextField>("inp-name").value = AccountData.DisplayName ?? AuthManager.USERNAME_NULL; }
        }

        [Serializable]
        class AnonymousUser : ISerializable<AnonymousUser>, ISerializableText, IDeserializableText
        {
            [ReadOnly] public string ID;
            [ReadOnly] public string DisplayName;
            [ReadOnly, NonReorderable] public List<string> Friends;

            public AnonymousUser()
            {
                ID = "";
                DisplayName = AuthManager.USERNAME_NULL;
                Friends = new List<string>();
            }

            public void Deserialize(Deserializer deserializer)
            {
                ID = deserializer.DeserializeString();
                DisplayName = deserializer.DeserializeString();
                Friends = deserializer.DeserializeArray<string>().ToList();
            }

            public void DeserializeText(Value data)
            {
                ID = data["ID"].String ?? throw new System.Exception($"Field ID not found");
                DisplayName = data["DisplayName"].String ?? string.Empty;
                Friends = data["Friends"].Array.ConvertPrimitive<string>().ToList();
            }

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ID);
                serializer.Serialize(DisplayName);
                serializer.Serialize(Friends.ToArray());
            }

            public Value SerializeText()
            {
                Value result = Value.Object();

                result["ID"] = Value.Literal(ID);
                result["DisplayName"] = Value.Literal(DisplayName);
                result["Array"] = Value.Object(Friends.ToArray());

                return result;
            }
        }
    }
}
