using AssetManager;

using Game.Managers;

using System;
using System.Collections.Generic;

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

            [SerializeField, ReadOnly] internal float Progress;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PrefabID);
                serializer.SerializeValue(ref RequiedProgress);
                serializer.SerializeValue(ref Progress);
            }
        }

        internal List<QueuedUnit> Queue;
        public int CursorPriority => 0;

        public float Progress
        {
            get
            {
                if (Queue.Count == 0) return 0f;
                var producing = Queue[0];
                return producing.Progress / producing.RequiedProgress;
            }
        }

        [SerializeField, AssetField] Transform DepotSpawn;

        void OnEnable()
        { WorldCursorManager.Instance.Register(this); }
        void OnDisable()
        { WorldCursorManager.Instance.Deregister(this); }

        void Start()
        {
            Queue = new List<QueuedUnit>();
            UpdateTeam();
        }

        void FixedUpdate()
        {
            if (!NetcodeUtils.IsOfflineOrServer)
            { return; }

            if (Queue.Count <= 0)
            { return; }

            Queue[0].Progress += Time.fixedDeltaTime;

            if (Queue[0].Progress < Queue[0].RequiedProgress)
            { return; }

            OnUnitDone(Queue[0]);
            Queue.RemoveAt(0);

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(); }
        }

        void OnUnitDone(QueuedUnit unit)
        {
            GameObject instance = AssetManager.AssetManager.InstantiatePrefab(unit.PrefabID, true, DepotSpawn.position, DepotSpawn.rotation);
            instance.transform.SetParent(transform.parent);
            if (instance.TryGetComponent(out BaseObject baseObject))
            {
                baseObject.Team = Team;
            }
            instance.SetActive(true);
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
                Queue.Add(new QueuedUnit()
                {
                    Progress = 0f,
                    RequiedProgress = unit.ProgressRequied,
                    PrefabID = unit.PrefabID,
                });

                if (UnitFactoryManager.Instance.SelectedFactory == this)
                { UnitFactoryManager.Instance.RefreshQueue(); }
            }
            else
            {
                QueueUnitRequest_ServerRpc(unit);
            }
        }

        [ServerRpc]
        void QueueUnitRequest_ServerRpc(UnitFactoryManager.ProducableUnit unit)
        {
            Queue.Add(new QueuedUnit()
            {
                Progress = 0f,
                RequiedProgress = unit.ProgressRequied,
                PrefabID = unit.PrefabID,
            });

            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(); }

            RefreshRequest_ClientRpc();
        }

        [ClientRpc]
        void RefreshRequest_ClientRpc()
        {
            if (UnitFactoryManager.Instance.SelectedFactory == this)
            { UnitFactoryManager.Instance.RefreshQueue(); }
        }
    }
}
