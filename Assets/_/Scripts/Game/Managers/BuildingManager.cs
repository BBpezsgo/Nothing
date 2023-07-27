using Game.Components;

using System;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.UIElements;

using Utilities;

namespace Game.Managers
{
    public class BuildingManager : NetworkBehaviour
    {
        static BuildingManager instance;

        internal static BuildingManager Instance => instance;

        [SerializeField] string Team;

        [SerializeField] PlayerData.ConstructableBuilding SelectedBuilding;
        [SerializeField] GameObject BuildingHologramPrefab;
        [SerializeField, ReadOnly] GameObject BuildingHologram;

        [SerializeField] GameObject BuildableBuildingPrefab;

        [SerializeField] internal Material BuildableMaterial;
        [SerializeField] internal Material HologramMaterial;

        [SerializeField, ReadOnly] bool IsValidPosition = false;
        [SerializeField, ReadOnly] bool CheckValidity = false;

        [SerializeField] Color ValidHologramColor = Color.white;
        [SerializeField] Color InvalidHologramColor = Color.red;
        [SerializeField, Range(-10f, 10f)] float HologramEmission = 1.1f;

        public bool IsBuilding => SelectedBuilding != null && SelectedBuilding.Building != null;

        InputUtils.AdvancedMouse LeftMouse;

        [Header("Buildings")]
        [SerializeField, ReadOnly, NonReorderable] PlayerData.ConstructableBuilding[] Buildings;

        [Header("UI")]
        [SerializeField] UIDocument BuildingUI;

        InputUtils.PriorityKey KeyEsc;

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
            var container = BuildingUI.rootVisualElement.Q<VisualElement>("unity-content-container");
            container.Clear();
            for (int i = 0; i < Buildings.Length; i++)
            {
                Button button = new()
                {
                    name = $"btn-{i}",
                    text = $"{Buildings[i].Building.name}",
                };
                button.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;
                container.Add(button);
            }
        }

        bool MouseCondition()
            => IsBuilding;

        void LeftMouse_OnClick(Vector2 position, float holdTime)
        {
            if (SelectedBuilding == null || SelectedBuilding.Building == null) return;
            if (!MouseManager.MouseOnWindow) return;
            if (MenuManager.AnyMenuVisible) return;
            if (!IsValidPosition) return;

            Vector3 worldPosition = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition);

            worldPosition.y = TheTerrain.Height(worldPosition);

            if (Input.GetKey(KeyCode.LeftControl))
            { worldPosition = new Vector3(Mathf.Round(worldPosition.x), worldPosition.y, Mathf.Round(worldPosition.z)); }

            PlaceBuilding(worldPosition - SelectedBuilding.GroundOrigin, SelectedBuilding);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void PlaceBuildingRequest_ServerRpc(Vector3 position, uint buildingHash)
        {
            PlayerData playerData = PlayerData.GetPlayerData(Team);
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
                ApplyBuildableHologram(hologram, this.Team, building);

                newObject.SpawnOverNetwork();
            }
            else if (NetcodeUtils.IsClient)
            {
                PlaceBuildingRequest_ServerRpc(fixedWorldPosition, building.Hash);
            }
        }

        void Clickable_clickedWithEventInfo(EventBase e)
        {
            if (e.target is not Button button) return;
            int i = int.Parse(button.name.Split('-')[1]);
            PlayerData.ConstructableBuilding building = Buildings[i];
            SelectedBuilding = building;
            if (BuildingHologram != null) { }
            { ApplyHologram(BuildingHologram, SelectedBuilding); }
        }

        void Show()
        {
            PlayerData playerData = PlayerData.GetPlayerData(Team);
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

            Buildings = null;
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

            if (Input.GetMouseButtonDown(MouseButton.Right) && (IsBuilding || BuildingUI.gameObject.activeSelf))
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
                    { position = new Vector3(Mathf.Round(position.x), position.y, Mathf.Round(position.z)); }

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
            if (CheckValidity)
            {
                if (MouseManager.MouseOnWindow)
                {
                    Vector3 position = MainCamera.Camera.ScreenToWorldPosition(Input.mousePosition);

                    position.y = TheTerrain.Height(position);

                    if (Input.GetKey(KeyCode.LeftControl))
                    { position = new Vector3(Mathf.Round(position.x), position.y, Mathf.Round(position.z)); }

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
        }

        internal static void ApplyBuildableHologram(BuildableBuilding hologram, string team, PlayerData.ConstructableBuilding building)
        {
            hologram.Team = team;
            hologram.Init(building.Hash, building.ProgressRequied);

            {
                Transform hologramColliders = hologram.transform.Find("Collider");
                if (hologramColliders != null)
                { hologramColliders.gameObject.Destroy(); }
            }

            GameObject hologramModels = GetHologramModelGroup(hologram.gameObject);
            hologramModels.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            CopyModel(building.Building, hologramModels);

            GameObject colliders = building.Building.transform.Find("Collider").gameObject;
            GameObject.Instantiate(colliders, hologram.transform);

            hologram.SetMaterial(instance.BuildableMaterial);
        }

        internal static void ApplyHologram(GameObject hologram, PlayerData.ConstructableBuilding buildingPrefab)
        {
            if (hologram == null) return;

            GameObject hologramModels = GetHologramModelGroup(hologram);
            hologramModels.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            CopyModel(buildingPrefab.Building, hologramModels);

            List<MeshRenderer> renderers = new();
            renderers.AddRange(hologram.GetComponentsInChildren<MeshRenderer>());

            for (int i = 0; i < renderers.Count; i++)
            { renderers[i].materials = new Material[] { Material.Instantiate(instance.HologramMaterial) }; }
        }

        static GameObject GetHologramModelGroup(GameObject hologram)
        {
            if (hologram == null) return null;
            Transform hologramModels = hologram.transform.Find("Model");
            if (hologramModels != null)
            { hologramModels.gameObject.Destroy(); }

            hologramModels = new GameObject("Model").transform;
            hologramModels.SetParent(hologram.transform);
            hologramModels.localPosition = Vector3.zero;
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
