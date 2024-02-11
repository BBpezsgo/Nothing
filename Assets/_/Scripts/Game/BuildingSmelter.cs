using System;
using UnityEngine;

namespace Game.Components
{
    internal class BuildingSmelter : Building, INeedItems
    {
        [SerializeField] string rawItemID;
        [SerializeField, Min(0)] float processingSpeed = 1f;
        [SerializeField, ReadOnly] float CurrentRawMaterials;
        [SerializeField, ReadOnly] float CurrentProcessedMaterials;
        [SerializeField] ParticleSystem[] ParticleSystems = new ParticleSystem[0];
        [SerializeField, ReadOnly, NonReorderable] float[] RateOverTimes = new float[0];

        public bool NeedItems => CurrentRawMaterials <= 0f;
        public string ItemID => rawItemID;
        string INeedItems.Team => Team;

        public void GiveItem(float ammount)
        {
            CurrentRawMaterials += ammount;
        }

        protected override void Start()
        {
            base.Start();

            RateOverTimes = new float[ParticleSystems.Length];
            for (int i = 0; i < ParticleSystems.Length; i++)
            {
                ParticleSystem.EmissionModule emission = ParticleSystems[i].emission;
                RateOverTimes[i] = emission.rateOverTimeMultiplier;
            }
        }

        void SetParticleSystemsEnabled(bool enabled)
        {
            for (int i = 0; i < ParticleSystems.Length; i++)
            {
                ParticleSystem.EmissionModule emission = ParticleSystems[i].emission;
                emission.rateOverTimeMultiplier = enabled ? RateOverTimes[i] : 0f;
            }
        }

        void Update()
        {
            if (CurrentRawMaterials > 0f)
            {
                float processed = CurrentRawMaterials;

                CurrentRawMaterials = Math.Max(CurrentRawMaterials - (Time.deltaTime * processingSpeed), 0f);
                processed -= CurrentRawMaterials;

                CurrentProcessedMaterials += processed;
                SetParticleSystemsEnabled(true && QualityHandler.EnableParticles);
            }
            else
            {
                SetParticleSystemsEnabled(false);
            }
        }
    }

    public interface INeedItems
    {
        public string Team { get; }
        public bool NeedItems { get; }
        public string ItemID { get; }

        public void GiveItem(float ammount);
    }
}
