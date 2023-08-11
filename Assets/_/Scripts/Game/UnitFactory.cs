using System;
using System.Collections.Generic;
using AssetManager;
using Game.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    internal class UnitFactory : Building, INeedDirectWorldCursor
    {
        [Serializable]
        internal class QueuedUnit : INetworkSerializable
        {
            [SerializeField, ReadOnly] internal string PrefabID;
            [SerializeField, ReadOnly] internal float RequiedProgress;
            [SerializeField, ReadOnly] internal string ThumbnailID;

            [SerializeField, ReadOnly] internal float Progress;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PrefabID);
                serializer.SerializeValue(ref RequiedProgress);
                serializer.SerializeValue(ref Progress);
            }
        }

        internal Queuev2<QueuedUnit> Queue;
        public int CursorPriority => 0;

        public float Progress
        {
            get
            {
                if (Queue.Count == 0) return 0f;
                var producing = Queue.First;
                return Mathf.Clamp01(producing.Progress / producing.RequiedProgress);
            }
        }

        [SerializeField, AssetField] Transform DepotSpawn;

        void OnEnable()
        { WorldCursorManager.Instance.Register(this); }
        void OnDisable()
        { WorldCursorManager.Instance.Deregister(this); }

        protected override void Start()
        {
            base.Start();

            Queue = new Queuev2<QueuedUnit>();
            UpdateTeam();
        }

        void FixedUpdate()
        {
            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            if (Queue.Count <= 0)
            { return; }

            Queue.First.Progress += Time.fixedDeltaTime;

            if (Queue.First.Progress < Queue.First.RequiedProgress)
            { return; }

            OnUnitDone(Queue.Dequeue());

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }
        }

        void OnUnitDone(QueuedUnit unit)
        {
            Vector3 spawnAt = DepotSpawn.position;
            spawnAt.y = TheTerrain.Height(spawnAt);
            AssetManager.AssetManager.InstantiatePrefab(unit.PrefabID, true, spawnAt, DepotSpawn.rotation, instance =>
            {
                instance.transform.SetParent(transform.parent);

                if (instance.TryGetComponent(out Collider collider))
                {
                    instance.transform.position = new Vector3(
                        instance.transform.position.x,
                        instance.transform.position.y + collider.bounds.size.y,
                        instance.transform.position.z);
                }

                if (instance.TryGetComponent(out BaseObject baseObject))
                { baseObject.Team = Team; }

                instance.SetActive(true);

                if (NetworkManager.IsListening && instance.TryGetComponent(out NetworkObject networkObject))
                { networkObject.Spawn(true); }
            });
        }

        public bool OnWorldCursor(Vector3 worldPosition)
        {
            UnitFactoryManager.Instance.Show(this);
            return true;
        }

        internal void QueueUnit(UnitFactoryManager.ProducableUnit unit)
        {
            if (NetcodeUtils.IsOfflineOrServer)
            {
                Queue.Enqueue(new QueuedUnit()
                {
                    Progress = 0f,
                    RequiedProgress = unit.ProgressRequied,
                    PrefabID = unit.PrefabID,
                    ThumbnailID = unit.ThumbnailID,
                });

                if (UnitFactoryManager.Instance.SelectedFactory == this)
                { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }

                RefreshRequest_ClientRpc();
            }
            else
            {
                QueueUnitRequest_ServerRpc(unit);
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void QueueUnitRequest_ServerRpc(UnitFactoryManager.ProducableUnit unit)
        {
            if (NetcodeUtils.IsOfflineOrServer)
            { QueueUnit(unit); }
        }

        [ClientRpc]
        void RefreshRequest_ClientRpc()
        {
            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(Queue.ToArray()); }
        }
    }
}
