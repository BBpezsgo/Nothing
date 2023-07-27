using System;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace Game.Blueprints
{
    public class NetworkBlueprintManager : NetworkBehaviour
    {
        static NetworkBlueprintManager instance;

        [SerializeField, ReadOnly, NonReorderable] List<Blueprint> blueprints = new();
        [SerializeField] string blueprintId;
        [SerializeField, Button(nameof(Test), false, true, "Sync")] string btnSync;
        [SerializeField, ReadOnly, NonReorderable] List<Request> Requests = new();

        [Serializable]
        struct Request
        {
            [SerializeField] public string BlueprintID;
            public Action<Blueprint> OnDone;

            public Request(string blueprintID, Action<Blueprint> onDone)
            {
                BlueprintID = blueprintID;
                OnDone = onDone;
            }
        }

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning($"[{nameof(NetworkBlueprintManager)}]: Instance already registered, destroying self");
                UnityEngine.Object.Destroy(this);
                return;
            }
            instance = this;
        }

        void Test() => GetBlueprint_(blueprintId, null);

        void RefreshRequests()
        {
            for (int i = Requests.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < blueprints.Count; j++)
                {
                    if (blueprints[j].Name == Requests[i].BlueprintID)
                    {
                        Requests[i].OnDone?.Invoke(blueprints[j]);
                        Requests.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static void GetBlueprint(string blueprintID, Action<Blueprint> onDone)
            => instance.GetBlueprint_(blueprintID, onDone);

        void GetBlueprint_(string blueprintID, Action<Blueprint> onDone)
        {
            if (NetworkManager.IsServer)
            {
                Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Get blueprint \"{blueprintID}\" from any client ...", this);

                Requests.Add(new Request(blueprintID, onDone));
                GetBlueprint_ClientRpc(blueprintID);
            }
            else if (NetworkManager.IsClient)
            {
                Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Get blueprint \"{blueprintID}\" from server ...", this);

                Requests.Add(new Request(blueprintID, onDone));
                GetBlueprint_ServerRpc(blueprintID);
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void Sync_ServerRpc(Blueprint blueprint)
        {
            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i].Name == blueprint.Name)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Blueprint \"{blueprint.Name}\" already exists", this);
                    RefreshRequests(); 
                    return;
                }
            }
            blueprints.Add(blueprint);
            RefreshRequests();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        void Sync_ClientRpc(Blueprint blueprint)
        {
            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i].Name == blueprint.Name)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Blueprint \"{blueprint.Name}\" already exists", this);
                    RefreshRequests();
                    return;
                }
            }
            blueprints.Add(blueprint);
            RefreshRequests();
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void GetBlueprint_ServerRpc(string blueprintID)
        {
            Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Someone needs the blueprint \"{blueprintID}\". Searching and sending to clients ...", this);
            
            Blueprint[] localBlueprints = BlueprintManager.LoadBlueprints();
            for (int i = 0; i < localBlueprints.Length; i++)
            {
                if (localBlueprints[i].Name == blueprintID)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Local blueprint \"{blueprintID}\" found, sending to clients ...", this);
                    Sync_ClientRpc(localBlueprints[i]);
                    return;
                }
            }

            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i].Name == blueprintID)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Networked blueprint \"{blueprintID}\" found, sending to clients ...", this);
                    Sync_ClientRpc(blueprints[i]);
                    return;
                }
            }

            Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Blueprint \"{blueprintID}\" not found", this);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        void GetBlueprint_ClientRpc(string blueprintID)
        {
            Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Server needs the blueprint \"{blueprintID}\". Searching and sending ...", this);

            Blueprint[] localBlueprints = BlueprintManager.LoadBlueprints();
            for (int i = 0; i < localBlueprints.Length; i++)
            {
                if (localBlueprints[i].Name == blueprintID)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Local blueprint \"{blueprintID}\" found, sending to server ...", this);
                    Sync_ServerRpc(localBlueprints[i]);
                    return;
                }
            }

            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i].Name == blueprintID)
                {
                    Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Networked blueprint \"{blueprintID}\" found, sending to server ...", this);
                    Sync_ServerRpc(blueprints[i]);
                    return;
                }
            }

            Debug.Log($"[{nameof(NetworkBlueprintManager)}]: Blueprint \"{blueprintID}\" not found", this);
        }
    }
}
