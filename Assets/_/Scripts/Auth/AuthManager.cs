using System;
using System.Linq;

using UnityEngine;

namespace Authentication
{
    public class AuthManager : SingleInstance<AuthManager>
    {
        public delegate void OnAuthorizedEvent(IAuthProvider provider);

        [SerializeField, ReadOnly] MonoBehaviour _AuthProvider;

        [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[] _AuthProviders;
        IAuthProvider[] AuthProviders;
        [SerializeField, ReadOnly] Providers.NullAuthProvider nullAuthProvider;

        public static IAuthProvider AuthProvider
        {
            get
            {
                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized)
                    {
                        Instance._AuthProvider = (MonoBehaviour)Instance.AuthProviders[i];
                        return Instance.AuthProviders[i];
                    }
                }
                return Instance.nullAuthProvider;
            }
        }
        /// <summary><b>Possibly <see langword="null"/>!</b></summary>
        public static IFriendsProvider Friends
        {
            get
            {
                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized)
                    {
                        if (Instance.AuthProviders[i] is IFriendsProvider friendsProvider)
                        { return friendsProvider; }
                        throw new NotImplementedException();
                    }
                }
                return null;
            }
        }
        public static IAccountMenuProvider AccountMenu
        {
            get
            {
                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized)
                    {
                        if (Instance.AuthProviders[i] is IAccountMenuProvider friendsProvider)
                        { return friendsProvider; }
                        throw new NotImplementedException();
                    }
                }

                return Instance.nullAuthProvider;
            }
        }
        /// <summary><b>Possibly <see langword="null"/>!</b></summary>
        public static IRemoteAccountProvider RemoteAccountProvider
        {
            get
            {
                var providers = RemoteAccountProviders;
                for (int i = 0; i < providers.Length; i++)
                {
                    if (providers[i].NeedAuthorization)
                    {
                        if (providers[i] is not IAuthProvider authProvider)
                        {
                            Debug.LogWarning($"Bruh");
                            continue;
                        }
                        if (!authProvider.IsAuthorized)
                        {
                            continue;
                        }
                    }
                    return providers[i];
                }
                return null;
            }
        }
        public static IRemoteAccountProvider[] RemoteAccountProviders => GameObject.FindObjectsOfType<MonoBehaviour>(false).OfType<IRemoteAccountProvider>().ToArray();
        public static bool IsAuthorized
        {
            get
            {
                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized) return true;
                }
                return false;
            }
        }
        public static OnAuthorizedEvent OnAuthorized;

        void Start()
        {
            if (Instance != this) return;

            this.nullAuthProvider = FindObjectOfType<Providers.NullAuthProvider>();
            this.AuthProviders = FindObjectsOfType<MonoBehaviour>(false).OfType<IAuthProvider>().ToArray();
            this._AuthProviders = this.AuthProviders.Select(v => (MonoBehaviour)v).ToArray();
        }

        public const string USERNAME_NULL = "<null>";
    }

    public interface IRemoteAccountProvider
    {
        public class RemoteAccount
        {
            public readonly string DisplayName;

            public RemoteAccount(string displayName)
            {
                DisplayName = displayName;
            }
        }

        public bool NeedAuthorization { get; }

        /// <returns><b>Possibly <see langword="null"/>!</b></returns>
        public RemoteAccount Get(string userId);
        public System.Collections.IEnumerator GetAsync(string userId, Action<RemoteAccount, object> callback);
    }

    public interface IRemoteAccountProviderWithCustomID<T> : IRemoteAccountProvider
    {
        /// <returns><b>Possibly <see langword="null"/>!</b></returns>
        public RemoteAccount Get(T userId);
        public System.Collections.IEnumerator GetAsync(T userId, Action<RemoteAccount, object> callback);
    }

    public interface IAccountMenuProvider
    {
        public void Show();
    }

    public interface IAuthProvider
    {
        public string DisplayName { get; }
        public string AvatarUrl { get; }
        public string ID { get; }
        public bool IsAuthorized { get; }

        public void Login();
        public void Logout();
    }

    public interface IModifiableAuthProvider : IAuthProvider
    {
        public new string DisplayName { get; set; }
        public new string AvatarUrl { get; set; }
    }

    public interface IFriendsProvider
    {
        public string[] GetFriends();
        public void AddFriend(string userId);
        public void RemoveFriend(string userId);
    }
}
