using System.Collections.Generic;

using UnityEngine;

namespace Game.Managers
{
    public class UnitCommandManager : MonoBehaviour
    {
        [SerializeField, ReadOnly] SelectionManager SelectionManager;

        [SerializeField] GameObject SeekTargetPrefab;
        [SerializeField, ReadOnly, NonReorderable] List<GameObject> SeekTargetInstances;

        void OnEnable()
        {
            SelectionManager.OnSelectionChanged += OnSelectionChanged;
        }
        void OnDisable()
        {
            SelectionManager.OnSelectionChanged -= OnSelectionChanged;
        }

        void Awake()
        {
            SelectionManager = FindFirstObjectByType<SelectionManager>();
            if (SelectionManager == null)
            { Debug.LogWarning($"[{nameof(UnitCommandManager)}]: {nameof(SelectionManager)} is null"); }
        }

        internal void OnClick(Vector3 worldPosition)
        {
            if (SelectionManager == null) return;
            if (SelectionManager.Selected.Length == 0) return;

            for (int i = 0; i < SelectionManager.Selected.Length; i++)
            {
                if (((Component)SelectionManager.Selected[i]).TryGetComponent(out Components.UnitBehaviour_Goto @goto))
                {
                    @goto.Target = worldPosition;
                    RefreshGotoPositions();
                }
            }
        }

        void RefreshGotoPositions()
        {
            int endlessSafe = 20;

            while (SeekTargetInstances.Count < SelectionManager.Selected.Length)
            {
                SeekTargetInstances.AddInstance(SeekTargetPrefab);
                if (endlessSafe-- <= 0) throw new System.Exception($"Endless loop");
            }

            endlessSafe = 20;
            while (SeekTargetInstances.Count > SelectionManager.Selected.Length)
            {
                SeekTargetInstances.PopAndDestroy();
                if (endlessSafe-- <= 0) throw new System.Exception($"Endless loop");
            }

            List<Vector2> positions = new();

            for (int i = 0; i < SelectionManager.Selected.Length; i++)
            {
                if (SelectionManager.Selected[i].TryGetComponent(out Components.UnitBehaviour_Goto @goto))
                {
                    SeekTargetInstances[i].transform.position = @goto.Target + new Vector3(0f, 0.05f, 0f);
                    if (@goto.Target != default)
                    {
                        bool clustered = false;
                        for (int j = 0; j < positions.Count; j++)
                        {
                            Vector2 diff = positions[j] - @goto.Target.To2D();
                            if (diff.sqrMagnitude < 1f)
                            {
                                clustered = true;
                                break;
                            }
                        }
                        if (!clustered)
                        {
                            positions.Add(@goto.Target.To2D());
                        }
                        SeekTargetInstances[i].SetActive(!clustered);
                    }
                    else
                    {
                        SeekTargetInstances[i].SetActive(false);
                    }
                }
                else
                {
                    SeekTargetInstances[i].SetActive(false);
                }
            }
        }

        void OnSelectionChanged()
        { RefreshGotoPositions(); }
    }
}
