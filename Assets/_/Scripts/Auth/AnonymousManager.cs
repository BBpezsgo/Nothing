using Game.Components;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using Utilities;

#nullable enable

namespace Authentication.Providers
{
    public class AnonymousManager : SingleInstance<AnonymousManager>, IModifiableAuthProvider, IFriendsProvider, IAccountMenuProvider
    {
        const string FileName = "anonymous";

        public bool IsAuthorized { get; private set; } = false;
        public Sprite? Avatar => null;
        public string? DisplayName
        {
            get => AccountData?.DisplayName;
            set => AccountData!.DisplayName = value;
        }
        public string? AvatarUrl
        {
            get => null;
            set { }
        }
        public string ID => AccountData!.ID;

        [SerializeField, ReadOnly] string? dataFilePath;
        string DataFilePath => throw new NotImplementedException();

        [SerializeField, ReadOnly] AnonymousUser? AccountData;

        [SerializeField] bool AutoAuthorize = false;

        [Header("UI")]
        [SerializeField] UIDocument? loginMenu;
        [SerializeField] UIDocument? accountMenu;

        void Start()
        {
            ResetHaveAccount();

            loginMenu.OnEnabled()!.On += () =>
            {
                loginMenu!.rootVisualElement.Q<Button>("button-anonymous").clicked += Login;
                ResetHaveAccount();
            };

            if (AutoAuthorize)
            {
                Login();
            }
        }

        float timeToNextResetHaveAccount = 0f;
        void Update()
        {
            timeToNextResetHaveAccount += Time.deltaTime;
            if (timeToNextResetHaveAccount >= 1f)
            {
                timeToNextResetHaveAccount = 0f;
                ResetHaveAccount();
            }
        }

        void ResetHaveAccount()
        {

        }

        #region UI Callback

        public void OnButtonUpdateAccount()
        {
            if (accountMenu == null)
            {
                DisplayName = null;
                return;
            }

            DisplayName = accountMenu.rootVisualElement.Q<TextField>("inp-name").value;
        }

        public void OnButtonLogout() { }

        public void Login()
        {
            AccountData = new AnonymousUser()
            {
                ID = "anonymous-" + Guid.NewGuid().ToString(),
            };

            Cookies.SetCookie(new Cookies.Cookie("Account", AccountData.ID)
            {
                MaxAge = 60 * 60 * 24 * 7,
            });

            // Debug.Log($"[{nameof(AnonymousManager)}]: Login anonymous {{ UserID: {AccountData.ID} }}");

            IsAuthorized = true;
            AuthManager.OnAuthorized?.Invoke(this);
        }

        void Save()
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {

        }

        #endregion

        public string[] GetFriends() => AccountData?.Friends.ToArray() ?? new string[0];
        public void AddFriend(string newFriendID) => throw new NotImplementedException();
        public void RemoveFriend(string userId) => throw new NotImplementedException();

        public void Show()
        {
            if (accountMenu != null && accountMenu.rootVisualElement != null)
            { accountMenu.rootVisualElement.Q<TextField>("inp-name").value = AccountData?.DisplayName ?? string.Empty; }
        }

        [Serializable]
        class AnonymousUser : ISerializable
        {
            [ReadOnly] public string ID;
            [ReadOnly] public string? DisplayName;
            [ReadOnly, NonReorderable] public List<string> Friends;

            public AnonymousUser()
            {
                ID = string.Empty;
                DisplayName = null;
                Friends = new List<string>();
            }

            public void Deserialize(BinaryReader deserializer)
            {
                ID = deserializer.ReadString() ?? string.Empty;
                DisplayName = deserializer.ReadString()!;
                Friends = deserializer.ReadArray(deserializer.ReadString).ToList();
            }

            public void Serialize(BinaryWriter serializer)
            {
                serializer.Write(ID);
                serializer.Write(DisplayName);
                serializer.Write(Friends.ToArray(), serializer.Write);
            }
        }
    }
}
