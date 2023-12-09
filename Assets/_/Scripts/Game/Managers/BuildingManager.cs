using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Game.Components;
using InputUtils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities;

namespace Game.Managers
{
    public class BuildingManager : NetworkBehaviour
    {
        static BuildingManager instance;

        public static BuildingManager Instance => instance;

        [SerializeField] PlayerData.ConstructableBuilding SelectedBuilding;
        [SerializeField] GameObject BuildingHologramPrefab;
        [SerializeField, ReadOnly] GameObject BuildingHologram;

        [SerializeField] GameObject BuildableBuildingPrefab;

        [SerializeField] public Material BuildableMaterial;
        [SerializeField] public Material HologramMaterial;

        [SerializeField, ReadOnly] bool IsValidPosition = false;
        [SerializeField, ReadOnly] bool CheckValidity = false;

        [SerializeField] Color ValidHologramColor = Color.white;
        [SerializeField] Color InvalidHologramColor = Color.red;
        [SerializeField, Range(-10f, 10f)] float HologramEmission = 1.1f;

        public bool IsBuilding => SelectedBuilding != null && SelectedBuilding.Building != null;

        AdvancedMouse LeftMouse;

        [Header("Buildings")]
        [SerializeField, ReadOnly, NonReorderable] PlayerData.ConstructableBuilding[] Buildings;

        [Header("UI")]
        [SerializeField] VisualTreeAsset BuildingButton;
        [SerializeField] UIDocument BuildingUI;

        PriorityKey KeyEsc;

#nullable enable

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning($"[{nameof(BuildingManager)}]: Instance already registered, destroying self");
                GameObject.Destroy(this);
                return;
            }
            instance = this;
        }

        void Start()
        {
            LeftMouse = new InputUtils.AdvancedMouse(0, 12, MouseCondition);
            LeftMouse.OnClick += LeftMouse_OnClick;

            KeyEsc = new InputUtils.PriorityKey(KeyCode.Escape, 3, () => IsBuilding || BuildingUI.gameObject.activeSelf);
            KeyEsc.OnDown += OnKeyEsc;
        }

        private void OnKeyEsc()
        {
            SelectedBuilding = null;
            if (BuildingHologram != null)
            { BuildingHologram.SetActive(false); }

            if (BuildingUI.gameObject.activeSelf)
            {
                IsValidPosition = false;
                Hide();
            }
        }

        void ListBuildings()
        {
            VisualElement container = BuildingUI.rootVisualElement.Q<VisualElement>("unity-content-container");
            container.Clear();

            for (int i = 0; i < Buildings.Length; i++)
            {
                TemplateContainer newElement = BuildingButton.Instantiate();

                Button button = newElement.Q<Button>();
                button.name = $"btn-{i}";
                button.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;

                if (PlayerData.TryGetThumbnail(Buildings[i].ThumbnailID, out Texture2D thumbnail))
                {
                    newElement.Q<VisualElement>("image").style.backgroundImage = new StyleBackground(thumbnail);
                    button.text = string.Empty;
                }
                else
                {
                    newElement.Q<VisualElement>("image").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                    button.text = $"{Buildings[i].Building.name}";
                }

                container.Add(newElement);
            }
        }

        bool MouseCondition()
            => IsBuilding;

        void LeftMouse_OnClick(AdvancedMouse sender)
        {
            if (SelectedBuilding == null || SelectedBuilding.Building == null) return;
            if (!MouseManager.MouseOnWindow) return;
            if (MenuManager.AnyMenuVisible) return;
            if (!IsValidPosition) return;

            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(AdvancedMouse.Position);

            worldPosition.y = TheTerrain.Height(worldPosition);

            if (Input.GetKey(KeyCode.LeftControl))
            { worldPosition = new Vector3(Maths.Round(worldPosition.x), worldPosition.y, Maths.Round(worldPosition.z)); }

            PlaceBuilding(worldPosition - SelectedBuilding.GroundOrigin, SelectedBuilding);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void PlaceBuildingRequest_ServerRpc(Vector3 position, uint buildingHash, string team)
        {
            PlayerData? playerData = PlayerData.GetPlayerData(team);
            Buildings = (playerData == null) ? new PlayerData.ConstructableBuilding[0] : playerData.ConstructableBuildings.ToArray();
            for (int i = 0; i < Buildings.Length; i++)
            {
                if (Buildings[i].Hash != buildingHash) continue;

                PlaceBuilding(position, Buildings[i]);
                return;
            }
            Debug.LogError($"[{nameof(BuildingManager)}]: Building \"{buildingHash}\" not found", this);
        }

        void PlaceBuilding(Vector3 fixedWorldPosition, PlayerData.ConstructableBuilding building)
        {
            if (NetcodeUtils.IsOfflineOrServer)
            {
                GameObject newObject = GameObject.Instantiate(BuildableBuildingPrefab, fixedWorldPosition, Quaternion.identity, transform);

                BuildableBuilding hologram = newObject.GetComponent<BuildableBuilding>();
                ApplyBuildableHologram(hologram, PlayerData.Instance.Team, building);

                newObject.SpawnOverNetwork();
            }
            else if (NetcodeUtils.IsClient)
            {
                PlaceBuildingRequest_ServerRpc(fixedWorldPosition, building.Hash, PlayerData.Instance.Team);
            }
        }

        void Clickable_clickedWithEventInfo(EventBase e)
        {
            if (e.target is not Button button) return;
            int i = int.Parse(button.name.Split('-')[1]);
            PlayerData.ConstructableBuilding building = Buildings[i];
            SelectedBuilding = building;
            if (BuildingHologram != null)
            { ApplyHologram(BuildingHologram, SelectedBuilding); }
        }

        void Show()
        {
            PlayerData? playerData = PlayerData.GetCurrentPlayerData();
            Buildings = (playerData == null) ? new PlayerData.ConstructableBuilding[0] : playerData.ConstructableBuildings.ToArray();

            BuildingUI.gameObject.SetActive(true);
            ListBuildings();
        }

        void Hide()
        {
            SelectedBuilding = null;

            BuildingUI.gameObject.SetActive(false);

            if (BuildingHologram != null)
            { BuildingHologram.SetActive(false); }

            Buildings = Array.Empty<PlayerData.ConstructableBuilding>();
        }

        void Update()
        {
            CheckValidity = false;
            if (Input.GetKeyDown(KeyCode.Home) &&
                !MenuManager.AnyMenuVisible)
            {
                SelectedBuilding = null;
                if (BuildingHologram != null)
                { BuildingHologram.SetActive(false); }

                if (BuildingUI.gameObject.activeSelf)
                {
                    IsValidPosition = false;
                    Hide();
                    return;
                }
                else
                {
                    IsValidPosition = false;
                    Show();
                }
            }

            if (Input.GetMouseButtonDown(Mouse.Right) && (IsBuilding || BuildingUI.gameObject.activeSelf))
            {
                Hide();
            }

            if (SelectedBuilding == null || SelectedBuilding.Building == null)
            {
                if (BuildingHologram != null)
                { BuildingHologram.SetActive(false); }
                return;
            }

            if (BuildingHologram == null)
            {
                BuildingHologram = GameObject.Instantiate(BuildingHologramPrefab, transform);
                ApplyHologram(BuildingHologram, SelectedBuilding);
            }
            else if (!BuildingHologram.activeSelf)
            {
                BuildingHologram.SetActive(true);
                ApplyHologram(BuildingHologram, SelectedBuilding);
            }

            if (!MenuManager.AnyMenuVisible)
            {
                CheckValidity = true;

                if (MouseManager.MouseOnWindow)
                {
                    Vector3 position = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition);

                    position.y = TheTerrain.Height(position);

                    if (Input.GetKey(KeyCode.LeftControl))
                    { position = new Vector3(Maths.Round(position.x), position.y, Maths.Round(position.z)); }

                    BuildingHologram.transform.position = position - SelectedBuilding.GroundOrigin;
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SelectedBuilding = null;
                    BuildingHologram.SetActive(false);
                    CheckValidity = false;
                    IsValidPosition = false;
                }
            }
        }

        void FixedUpdate()
        {
            if (CheckValidity &&
                MouseManager.MouseOnWindow &&
                SelectedBuilding != null)
            {
                Vector3 position = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition);

                position.y = TheTerrain.Height(position);

                if (Input.GetKey(KeyCode.LeftControl))
                { position = new Vector3(Maths.Round(position.x), position.y, Maths.Round(position.z)); }

                Vector3 checkPosition = position - SelectedBuilding.GroundOrigin;

                Debug3D.DrawBox(checkPosition, SelectedBuilding.SpaceNeed, Color.white, Time.fixedDeltaTime);

                if (Physics.OverlapBox(checkPosition, SelectedBuilding.SpaceNeed / 2, Quaternion.identity, LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Water)).Length > 0)
                { IsValidPosition = false; }
                else
                { IsValidPosition = true; }

                MeshRenderer[] renderers = BuildingHologram.GetComponentsInChildren<MeshRenderer>();

                for (int i = 0; i < renderers.Length; i++)
                {
                    var material = renderers[i].material;
                    material.color = IsValidPosition ? ValidHologramColor : InvalidHologramColor;
                    material.SetEmissionColor(IsValidPosition ? ValidHologramColor : InvalidHologramColor, HologramEmission);
                }
            }
        }

        public static void ApplyBuildableHologram(BuildableBuilding hologram, string team, PlayerData.ConstructableBuilding building)
        {
            hologram.Team = team;
            hologram.Init(building.Hash, building.ProgressRequied);

            {
                Transform hologramColliders = hologram.transform.Find("Collider");
                if (hologramColliders != null)
                { Destroy(hologramColliders.gameObject); }
            }

            GameObject hologramModels = GetHologramModelGroup(hologram.gameObject);
            hologramModels.transform.SetPositionAndRotation(default, Quaternion.identity);
            CopyModel(building.Building, hologramModels);

            GameObject colliders = building.Building.transform.Find("Collider").gameObject;
            GameObject.Instantiate(colliders, hologram.transform);

            hologram.SetMaterial(instance.BuildableMaterial);
        }

        public static void ApplyHologram(GameObject? hologram, PlayerData.ConstructableBuilding buildingPrefab)
        {
            if (hologram == null) return;

            GameObject hologramModels = GetHologramModelGroup(hologram);
            hologramModels.transform.SetPositionAndRotation(default, Quaternion.identity);
            CopyModel(buildingPrefab.Building, hologramModels);

            List<MeshRenderer> renderers = new();
            renderers.AddRange(hologram.GetComponentsInChildren<MeshRenderer>());

            for (int i = 0; i < renderers.Count; i++)
            { renderers[i].materials = new Material[] { Material.Instantiate(instance.HologramMaterial) }; }
        }

        [return: NotNullIfNotNull("hologram")]
        static GameObject? GetHologramModelGroup(GameObject? hologram)
        {
            if (hologram == null) return null;
            Transform hologramModels = hologram.transform.Find("Model");
            if (hologramModels != null)
            { Destroy(hologramModels.gameObject); }

            hologramModels = new GameObject("Model").transform;
            hologramModels.SetParent(hologram.transform);
            hologramModels.localPosition = default;
            return hologramModels.gameObject;
        }

        static void CopyModel(GameObject from, GameObject to)
        {
            /*
            MeshRenderer[] meshRenderers = from.GetComponentsInChildren<MeshRenderer>(false);

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                Vector3 relativePosition = from.transform.InverseTransformPoint(meshRenderer.transform.position);
                Vector3 relativeRotation = from.transform.InverseTransformDirection(meshRenderer.transform.eulerAngles);

                GameObject newObject = new(meshRenderer.gameObject.name);
                newObject.transform.SetParent(to.transform);
                newObject.transform.SetLocalPositionAndRotation(relativePosition, Quaternion.Euler(relativeRotation));
                newObject.transform.localScale = meshRenderer.transform.localScale;

                MeshRenderer.Instantiate(meshRenderer, newObject.transform);
                MeshFilter.Instantiate(meshFilter, newObject.transform);
            }
            */

            to.transform.localScale = from.transform.localScale;
            to.transform.SetLocalPositionAndRotation(from.transform.localPosition, from.transform.localRotation);

            if (from.TryGetComponent<MeshRenderer>(out var meshRenderer) &&
                !to.TryGetComponent<MeshRenderer>(out _))
            {
                MeshRenderer.Instantiate(meshRenderer, to.transform);
                MeshFilter.Instantiate(from.GetComponent<MeshFilter>(), to.transform);
            }

            int childCount = from.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject prefabChild = from.transform.GetChild(i).gameObject;
                GameObject newHologramChild = new(prefabChild.name);
                newHologramChild.transform.SetParent(to.transform);
                CopyModel(prefabChild, newHologramChild);
            }
        }
    }
}
