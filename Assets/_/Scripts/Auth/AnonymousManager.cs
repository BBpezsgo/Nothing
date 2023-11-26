using DataUtilities.ReadableFileFormat;
using DataUtilities.Serializer;

using Game.Components;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Authentication.Providers
{
    public class AnonymousManager : SingleInstance<AnonymousManager>, IModifiableAuthProvider, IFriendsProvider, IAccountMenuProvider
    {
        const string fileName = "anonymous";

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

            loginMenu.OnEnabled()!.onEnable += () =>
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

        }

        #region UI Callback

        public void OnButtonUpdateAccount()
        {
            DisplayName = accountMenu?.rootVisualElement.Q<TextField>("inp-name").value;
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
            if (accountMenu?.rootVisualElement != null)
            { accountMenu.rootVisualElement.Q<TextField>("inp-name").value = AccountData?.DisplayName ?? string.Empty; }
        }

        [Serializable]
        class AnonymousUser : ISerializable<AnonymousUser>, ISerializableText, IDeserializableText
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

            public void Deserialize(Deserializer deserializer)
            {
                ID = deserializer.DeserializeString() ?? string.Empty;
                DisplayName = deserializer.DeserializeString()!;
                Friends = deserializer.DeserializeArray<string>().ToList();
            }

            public void DeserializeText(Value data)
            {
                ID = data["ID"].String ?? throw new System.Exception($"Field ID not found");
                DisplayName = data["DisplayName"].String ?? string.Empty;
                Friends = data["Friends"].Array!.ConvertPrimitive<string>().ToList();
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
