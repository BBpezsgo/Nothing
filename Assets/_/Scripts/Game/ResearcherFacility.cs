using Game.Managers;

using UnityEngine;

namespace Game.Components
{
    internal class ResearcherFacility : Building, INeedDirectWorldCursor
    {
        [SerializeField] Light StatusLight;
        [SerializeField, ReadOnly] internal string ResearchingID;
        public int CursorPriority => 0;

        protected override void Start()
        {
            base.Start();
            WorldCursorManager.Instance.Register(this);
            ResearchingID = null;
            UpdateTeam();
        }

        void Update()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (string.IsNullOrEmpty(ResearchingID))
            {
                StatusLight.color = Color.green;
                return;
            }

            StatusLight.color = Color.yellow;

            bool isDone = Research.ResearchManager.Instance.AddProgress(ResearchingID, Time.deltaTime);

            if (isDone)
            { ResearchingID = null; }
        }

        public bool OnWorldCursor(Vector3 worldPosition)
        {
            return Research.ResearchManager.Instance.OnFacilitySelected(this);
        }
    }
}