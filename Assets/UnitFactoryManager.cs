using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

public class UnitFactoryManager : SingleInstance<UnitFactoryManager>
{
    [SerializeField] string Team;

    [SerializeField, ReadOnly] internal UnitFactory SelectedFactory;

    [Header("UI")]
    [SerializeField] UIDocument FactoryUI;
    [SerializeField] VisualTreeAsset QueueElement;
    VisualElement BarProgress;

    [SerializeField, ReadOnly, NonReorderable] ProducableUnit[] Units;

    [Serializable]
    internal class ProducableUnit
    {
        [SerializeField, ReadOnly] internal string PrefabID;
        [SerializeField, ReadOnly] internal float ProgressRequied;

        public ProducableUnit()
        { }

        public ProducableUnit(PlayerData.ProducableUnit other) : this()
        {
            PrefabID = other.Unit.name;
            ProgressRequied = other.ProgressRequied;
        }
    }

    PriorityKey KeyEsc;

    void ListBuildings()
    {
        var container = FactoryUI.rootVisualElement.Q<VisualElement>("unity-content-container");
        container.Clear();
        for (int i = 0; i < Units.Length; i++)
        {
            Button button = new()
            {
                name = $"btn-{i}",
                text = $"{Units[i].PrefabID}",
            };
            button.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;
            container.Add(button);
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
        Units = GetUnits();

        SelectedFactory = factory;
        FactoryUI.gameObject.SetActive(true);
        ListBuildings();

        BarProgress = FactoryUI.rootVisualElement.Q<VisualElement>("bar-progress");
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

        var blueprints = Blueprints.BlueprintManager.LoadBlueprints();

        for (int i = 0; i < blueprints.Length; i++)
        {
            units.Add(new ProducableUnit()
            {
                PrefabID = blueprints[i].Name,
                ProgressRequied = 2f,
            });
        }

        return units.ToArray();
    }

    private void Start()
    {
        KeyEsc = new PriorityKey(KeyCode.Escape, 5, () => FactoryUI.gameObject.activeSelf);
        KeyEsc.OnDown += OnKeyEsc;
    }

    void OnKeyEsc()
    {
        SelectedFactory = null;
        FactoryUI.gameObject.SetActive(false);
    }

    internal void RefreshQueue()
    {
        ScrollView container = FactoryUI.rootVisualElement.Q<ScrollView>("scrollview-queue");
        container.Clear();

        if (SelectedFactory == null) return;

        UnitFactory.QueuedUnit[] queue = SelectedFactory.Queue.ToArray();

        for (int i = 0; i < queue.Length; i++)
        {
            TemplateContainer newElement = QueueElement.Instantiate();

            newElement.Q<Label>().text = queue[i].PrefabID;

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
        {
            BarProgress.style.width = new StyleLength(new Length(SelectedFactory.Progress * 100f, LengthUnit.Percent));
        }
    }
}
