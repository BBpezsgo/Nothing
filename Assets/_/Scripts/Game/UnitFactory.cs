using System;
using AssetManager;
using Game.Managers;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    internal class UnitFactory : Building, INeedDirectWorldCursor
    {
        [Serializable]
        internal struct QueuedUnit : INetworkSerializable, IEquatable<QueuedUnit>
        {
            [SerializeField, ReadOnly] internal FixedString32Bytes PrefabID;
            [SerializeField, ReadOnly] internal float RequiedProgress;
            [SerializeField, ReadOnly] internal FixedString32Bytes ThumbnailID;

            [SerializeField, ReadOnly] internal float Progress;

            public readonly bool Equals(QueuedUnit other) => this.PrefabID == other.PrefabID;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PrefabID);
                serializer.SerializeValue(ref RequiedProgress);
                serializer.SerializeValue(ref Progress);
            }
        }

        internal NetworkList<QueuedUnit> Queue;

        public int CursorPriority => 0;

        public float Progress
        {
            get
            {
                if (Queue.Count == 0) return 0f;
                QueuedUnit producing = Queue[0];
                return Mathf.Clamp01(producing.Progress / producing.RequiedProgress);
            }
        }

        [SerializeField, AssetField] Transform DepotSpawn;

        void OnEnable()
        { WorldCursorManager.Instance.Register(this); }
        void OnDisable()
        { WorldCursorManager.Instance.Deregister(this); }

        void Awake()
        {
            Queue = new NetworkList<QueuedUnit>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            Queue.OnListChanged += OnQueueChanged;
        }

        void OnQueueChanged(NetworkListEvent<QueuedUnit> changeEvent)
        {
            if (NetcodeUtils.IsOfflineOrServer) return;

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }
        }

        void FixedUpdate()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (Queue.Count <= 0) return;

            QueuedUnit first = Queue[0];
            first.Progress += Time.fixedDeltaTime;
            Queue[0] = first;

            if (first.Progress < first.RequiedProgress) return;

            OnUnitDone(Queue.Dequeue());

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }
        }

        void OnUnitDone(QueuedUnit unit)
        {
            Vector3 spawnAt = DepotSpawn.position;
            spawnAt.y = TheTerrain.Height(spawnAt);
            AssetManager.AssetManager.InstantiatePrefab(unit.PrefabID.ToString(), true, spawnAt, DepotSpawn.rotation, instance =>
            {
                instance.transform.SetParent(transform.parent);

                if (instance.TryGetComponent(out Collider collider))
                {
                    instance.transform.position = new Vector3(
                        instance.transform.position.x,
                        instance.transform.position.y + collider.bounds.size.y,
                        instance.transform.position.z);
                }

                BaseObject baseObject = instance.GetComponentInChildren<BaseObject>(false);
                if (baseObject != null)
                { baseObject.Team = Team; }

                instance.SetActive(true);

                instance.SpawnOverNetwork(true);
            });
        }

        public bool OnWorldCursor(Vector3 worldPosition)
        {
            UnitFactoryManager.Instance.Show(this);
            return true;
        }

        internal void QueueUnit(UnitFactoryManager.ProducableUnit unit)
        {
            if (!NetcodeUtils.IsOfflineOrServer)
            {
                QueueUnitRequest_ServerRpc(unit);
                return;
            }

            Queue.Enqueue(new QueuedUnit()
            {
                Progress = 0f,
                RequiedProgress = unit.ProgressRequied,
                PrefabID = unit.PrefabID ?? string.Empty,
                ThumbnailID = unit.ThumbnailID ?? string.Empty,
            });

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void QueueUnitRequest_ServerRpc(UnitFactoryManager.ProducableUnit unit)
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            QueueUnit(unit);
        }
    }
}
