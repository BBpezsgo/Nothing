using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using UnityEngine;
using Utilities;

#nullable enable

namespace Authentication
{
    public class AuthManager : SingleInstance<AuthManager>
    {
        public delegate void OnAuthorizedEvent(IAuthProvider provider);

        [SerializeField, ReadOnly] MonoBehaviour? _AuthProvider;

        [SerializeField, ReadOnly, NonReorderable] MonoBehaviour[]? _AuthProviders;
        IAuthProvider[] AuthProviders = new IAuthProvider[0];
        [SerializeField, ReadOnly] Providers.NullAuthProvider? nullAuthProvider;

        public static IAuthProvider AuthProvider
        {
            get
            {
                if (Instance == null)
                { throw new NullReferenceException($"{nameof(SingleInstance<AuthManager>)}.{nameof(Instance)} is null"); }

                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized)
                    {
                        Instance._AuthProvider = (MonoBehaviour)Instance.AuthProviders[i];
                        return Instance.AuthProviders[i];
                    }
                }
                return Instance.nullAuthProvider!;
            }
        }
        public static IFriendsProvider? Friends
        {
            get
            {
                if (Instance == null)
                { throw new NullReferenceException($"{nameof(SingleInstance<AuthManager>)}.{nameof(Instance)} is null"); }

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
        public static IAccountMenuProvider? AccountMenu
        {
            get
            {
                if (Instance == null)
                { throw new NullReferenceException($"{nameof(SingleInstance<AuthManager>)}.{nameof(Instance)} is null"); }

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
        public static IRemoteAccountProvider? RemoteAccountProvider
        {
            get
            {
                IRemoteAccountProvider[] providers = RemoteAccountProviders;
                for (int i = 0; i < providers.Length; i++)
                {
                    if (!providers[i].CanRequestRemoteAccount)
                    { continue; }
                    return providers[i];
                }
                return null;
            }
        }
        public static IRemoteAccountProvider[] RemoteAccountProviders => GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<IRemoteAccountProvider>().ToArray();
        public static bool IsAuthorized
        {
            get
            {
                if (Instance == null)
                { throw new NullReferenceException($"{nameof(SingleInstance<AuthManager>)}.{nameof(Instance)} is null"); }

                for (int i = 0; i < Instance.AuthProviders.Length; i++)
                {
                    if (Instance.AuthProviders[i].IsAuthorized) return true;
                }
                return false;
            }
        }
        public static OnAuthorizedEvent? OnAuthorized;

        void Start()
        {
            if (Instance != this) return;

            this.nullAuthProvider = FindFirstObjectByType<Providers.NullAuthProvider>();
            this.AuthProviders = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<IAuthProvider>().ToArray();
            this._AuthProviders = this.AuthProviders.Select(v => (MonoBehaviour)v).ToArray();
        }

        public static bool GetRemoteAccountAsync(string userId, Action<TaskResult<IRemoteAccountProvider.RemoteAccount, string>> callback)
        {
            if (RemoteAccountProvider == null) return false;
            if (instance == null) return false;
            IRemoteAccountProvider[] providers = RemoteAccountProviders;
            for (int i = 0; i < providers.Length; i++)
            {
                if (!providers[i].CanRequestRemoteAccount)
                { continue; }
                instance.StartCoroutine(providers[i].GetAsync(userId, callback));
                return true;
            }
            return false;
        }

        public static bool GetRemoteAccount(string userId, [NotNullWhen(true)] out IRemoteAccountProvider.RemoteAccount? remoteAccount)
        {
            remoteAccount = null;
            if (RemoteAccountProvider == null) return false;
            if (instance == null) return false;
            IRemoteAccountProvider[] providers = RemoteAccountProviders;
            for (int i = 0; i < providers.Length; i++)
            {
                if (!providers[i].CanRequestRemoteAccount)
                { continue; }
                remoteAccount = providers[i].Get(userId);
                if (remoteAccount == null) continue;
                return true;
            }
            return false;
        }
    }

    public interface IRemoteAccountProvider
    {
        public class RemoteAccount
        {
            public readonly string? DisplayName;

            public RemoteAccount(string? displayName)
            {
                DisplayName = displayName;
            }
        }

        public bool CanRequestRemoteAccount { get; }

        public RemoteAccount? Get(string userId);
        public System.Collections.IEnumerator GetAsync(string userId, Action<TaskResult<RemoteAccount, string>> callback);
    }

    public interface IRemoteAccountProviderWithCustomID<TUserId> : IRemoteAccountProvider
    {
        /// <returns><b>Possibly <see langword="null"/>!</b></returns>
        public RemoteAccount? Get(TUserId userId);
        public System.Collections.IEnumerator GetAsync(TUserId userId, Action<TaskResult<RemoteAccount, string>> callback);
    }

    public interface IAccountMenuProvider
    {
        public void Show();
    }

    public interface IAuthProvider
    {
        public string? DisplayName { get; }
        public string? AvatarUrl { get; }
        public string ID { get; }
        public bool IsAuthorized { get; }

        public void Login();
        public void Logout();
    }

    public interface IModifiableAuthProvider : IAuthProvider
    {
        public new string? DisplayName { get; set; }
        public new string? AvatarUrl { get; set; }
    }

    public interface IFriendsProvider
    {
        public string[] GetFriends();
        public void AddFriend(string userId);
        public void RemoveFriend(string userId);
    }
}
