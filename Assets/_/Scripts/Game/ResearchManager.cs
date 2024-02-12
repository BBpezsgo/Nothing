using Game.Components;
using Game.Managers;
using Game.UI;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Research
{
    public class ResearchManager : SingleInstance<ResearchManager>
    {
        [SerializeField] Researchable[] researchables = new Researchable[0];

        [SerializeField, ReadOnly] internal ResearcherFacility SelectedFacility;

        /// <returns>
        /// <see langword="true"/> if the researchable is done.
        /// </returns>
        internal bool AddProgress(string id, float progress)
        {
            Researchable researchable = GetResearchable(id);

            if (researchable == null)
            { return false; }

            if (!researchable.IsResearching)
            { return false; }

            researchable.CurrentTime += progress;

            if (researchable.ResearchTime <= researchable.CurrentTime)
            {
                researchable.IsResearched = true;
                researchable.IsResearching = false;

                MenuResearch menu = FindFirstObjectByType<MenuResearch>(FindObjectsInactive.Exclude);
                if (menu != null)
                { menu.RefreshResearchables(); }

                return true;
            }

            return false;
        }

        internal Researchable[] Researchables
        {
            get
            {
                List<Researchable> researchables = new();
                for (int i = 0; i < this.researchables.Length; i++)
                {
                    if (CanResearchThis(this.researchables[i]) && !this.researchables[i].IsResearched)
                    {
                        if (!HasResearchable(researchables, this.researchables[i].ID))
                        { researchables.Add(this.researchables[i]); }
                    }
                }
                return researchables.ToArray();
            }
        }

        internal bool CanResearchThis(string id)
            => CanResearchThis(GetResearchable(id));
        internal bool CanResearchThis(Researchable researchable)
        {
            if (researchable is null) throw new ArgumentNullException(nameof(researchable));

            if (researchable.RequiedResearches == null || researchable.RequiedResearches.Length == 0)
            { return true; }

            for (int i = 0; i < researchable.RequiedResearches.Length; i++)
            {
                string requiedResearchID = researchable.RequiedResearches[i];
                if (requiedResearchID == researchable.ID)
                {
                    Debug.LogWarning($"[{nameof(ResearchManager)}]: Researchable \"{researchable.ID}\" referencing to itself");
                    return false;
                }
                Researchable requiedResearch = GetResearchable(requiedResearchID);
                if (requiedResearch == null)
                { continue; }
                if (!requiedResearch.IsResearched)
                { return false; }
            }
            return true;
        }

        internal Researchable GetResearchable(string id)
        {
            for (int i = 0; i < researchables.Length; i++)
            {
                if (researchables[i].ID == id)
                { return researchables[i]; }
            }
            Debug.LogWarning($"[{nameof(ResearchManager)}]: Researchable \"{id}\" not found");
            return null;
        }
        internal static Researchable GetResearchable(IEnumerable<Researchable> researchables, string id)
        {
            foreach (Researchable researchable in researchables)
            {
                if (researchable.ID == id)
                { return researchable; }
            }
            Debug.LogWarning($"[{nameof(ResearchManager)}]: Researchable \"{id}\" not found");
            return null;
        }

        internal bool HasResearchable(string id)
        {
            for (int i = 0; i < researchables.Length; i++)
            {
                if (researchables[i].ID == id)
                { return true; }
            }
            return false;
        }
        internal static bool HasResearchable(IEnumerable<Researchable> researchables, string id)
        {
            foreach (Researchable researchable in researchables)
            {
                if (researchable.ID == id)
                { return true; }
            }
            return false;
        }

        internal bool Research(string id)
        {
            Researchable researchable = GetResearchable(id);
            if (researchable == null) return false;
            if (researchable.IsResearching) return false;
            if (SelectedFacility == null) return false;

            SelectedFacility.ResearchingID = researchable.ID;

            researchable.IsResearching = true;
            researchable.CurrentTime = 0f;

            return true;
        }

        internal bool OnFacilitySelected(ResearcherFacility researcherFacility)
        {
            if (MenuManager.Instance.CurrentMenu == MenuManager.MainMenuType.None)
            {
                SelectedFacility = researcherFacility;
                MenuManager.Instance.CurrentMenu = MenuManager.MainMenuType.Game_Research;
                return true;
            }
            return false;
        }

        internal void DeselectFacility()
        { SelectedFacility = null; }
    }

    [Serializable]
    public class Researchable
    {
        [SerializeField] internal string ID;
        [SerializeField] internal string[] RequiedResearches = new string[0];
        [SerializeField] internal string Name;
        [SerializeField, Min(1), Tooltip("In sec.")] internal float ResearchTime;
        [SerializeField] internal bool IsResearched = false;
        [SerializeField, ReadOnly] internal float CurrentTime;
        [SerializeField, ReadOnly] internal bool IsResearching;

        public float ProgressPercent => CurrentTime / ResearchTime;
    }
}
