using Game.Components;

using System;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Managers
{
    public class UnitFactoryManager : SingleInstance<UnitFactoryManager>
    {
        [SerializeField] string Team;

        [SerializeField, ReadOnly] internal UnitFactory SelectedFactory;

        [Header("UI")]
        [SerializeField] UIDocument FactoryUI;
        [SerializeField] VisualTreeAsset ProducableElement;
        [SerializeField] VisualTreeAsset QueueElement;
        ProgressBar BarProgress;

        [SerializeField, ReadOnly, NonReorderable] ProducableUnit[] Units;

        [Serializable]
        internal class ProducableUnit : INetworkSerializable
        {
            [SerializeField, ReadOnly] internal string PrefabID;
            [SerializeField, ReadOnly] internal float ProgressRequied;
            [SerializeField, ReadOnly] internal string ThumbnailID;

            public ProducableUnit()
            { }

            public ProducableUnit(PlayerData.ProducableUnit other) : this()
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

        InputUtils.PriorityKey KeyEsc;

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

        internal void Show(UnitFactory factory)
        {
            if (factory == null)
            { return; }

            Units = GetUnits();

            SelectedFactory = factory;
            FactoryUI.gameObject.SetActive(true);
            ListUnits(Units);

            RefreshQueue(SelectedFactory.Queue.ToArray());

            BarProgress = FactoryUI.rootVisualElement.Q<ProgressBar>("progressbar-progress");
        }

        ProducableUnit[] GetUnits()
        {
            PlayerData playerData = PlayerData.GetPlayerData(Team);
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

        void FixedUpdate()
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
