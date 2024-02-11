using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Managers
{
    public class UnitFactoryManager : SingleInstance<UnitFactoryManager>
    {
        [SerializeField, ReadOnly] public UnitFactory SelectedFactory;

        [Header("UI")]
        [SerializeField] UIDocument FactoryUI;
        [SerializeField] VisualTreeAsset ProducableElement;
        [SerializeField] VisualTreeAsset QueueElement;
        ProgressBar BarProgress;

        [SerializeField, ReadOnly, NonReorderable] ProducableUnit[] Units;

        InputUtils.PriorityKey KeyEsc;

#nullable enable

        [Serializable]
        public class ProducableUnit : INetworkSerializable
        {
            [SerializeField, ReadOnly] public string PrefabID;
            [SerializeField, ReadOnly] public float ProgressRequied;
            [SerializeField, ReadOnly] public string ThumbnailID;

            public ProducableUnit(PlayerData.ProducableUnit other)
            {
                PrefabID = other.Unit.name;
                ProgressRequied = other.ProgressRequied;
                ThumbnailID = other.ThumbnailID;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PrefabID);
                serializer.SerializeValue(ref ProgressRequied);
            }
        }

        void ListUnits(ProducableUnit[] units)
        {
            VisualElement container = FactoryUI.rootVisualElement.Q<VisualElement>("unity-content-container");
            container.Clear();

            for (int i = 0; i < units.Length; i++)
            {
                TemplateContainer newElement = ProducableElement.Instantiate();

                newElement.Q<Label>().text = units[i].PrefabID;

                if (PlayerData.TryGetThumbnail(units[i].ThumbnailID, out Texture2D thumbnail))
                { newElement.Q<VisualElement>("image").style.backgroundImage = new StyleBackground(thumbnail); }

                newElement.Q<Button>().name = $"btn-{i}";
                newElement.Q<Button>().clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;

                container.Add(newElement);
            }
        }

        void Clickable_clickedWithEventInfo(EventBase e)
        {
            if (e.target is not Button button) return;
            if (SelectedFactory == null) return;

            int i = int.Parse(button.name.Split('-')[1]);
            ProducableUnit unit = Units[i];
            SelectedFactory.QueueUnit(unit);
        }

        public void Show(UnitFactory? factory)
        {
            if (factory == null)
            { return; }

            Units = GetUnits(factory.Team);

            SelectedFactory = factory;
            FactoryUI.gameObject.SetActive(true);
            ListUnits(Units);

            RefreshQueue(SelectedFactory.Queue.ToArray());

            BarProgress = FactoryUI.rootVisualElement.Q<ProgressBar>("progressbar-progress");
        }

        static ProducableUnit[] GetUnits(string team)
        {
            PlayerData? playerData = PlayerData.GetPlayerData(team);
            List<ProducableUnit> units = new();

            if (playerData != null)
            {
                for (int i = 0; i < playerData.ProducableUnits.Count; i++)
                {
                    units.Add(new ProducableUnit(playerData.ProducableUnits[i]));
                }
            }

            return units.ToArray();
        }

        void Start()
        {
            KeyEsc = new InputUtils.PriorityKey(KeyCode.Escape, 5, () => FactoryUI.gameObject.activeSelf);
            KeyEsc.OnDown += OnKeyEsc;
        }

        void OnKeyEsc()
        {
            SelectedFactory = null;
            FactoryUI.gameObject.SetActive(false);
        }

        internal void RefreshQueue(UnitFactory.QueuedUnit[] queue)
        {
            ScrollView container = FactoryUI.rootVisualElement.Q<ScrollView>("scrollview-queue");
            container.Clear();

            if (SelectedFactory == null) return;

            for (int i = 0; i < queue.Length; i++)
            {
                TemplateContainer newElement = QueueElement.Instantiate();

                newElement.Q<Label>().text = queue[i].PrefabID.ToString();
                if (PlayerData.TryGetThumbnail(queue[i].ThumbnailID.ToString(), out Texture2D thumbnail))
                { newElement.Q<VisualElement>("image").style.backgroundImage = new StyleBackground(thumbnail); }

                container.Add(newElement);
            }
        }

        void Update()
        {
            if (SelectedFactory == null)
            {
                FactoryUI.gameObject.SetActive(false);
                return;
            }

            if (BarProgress != null)
            { BarProgress.value = SelectedFactory.Progress; }
        }
    }
}
