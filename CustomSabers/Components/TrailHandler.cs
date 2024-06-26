﻿using CustomSabersLite.Configuration;
using CustomSaber;
using IPA.Utilities;
using System;
using UnityEngine;
using Zenject;
using CustomSabersLite.Data;

namespace CustomSabersLite.Utilities
{
    internal class CustomTrailHandler
    {
        private readonly CSLConfig config;
        private readonly TrailUtils trailUtils;
        private readonly GameplayCoreSceneSetupData gameplaySetupData;

        public CustomTrailHandler(CSLConfig config, TrailUtils trailUtils, GameplayCoreSceneSetupData gameplaySetupData)
        {
            this.config = config;
            this.trailUtils = trailUtils;
            this.gameplaySetupData = gameplaySetupData;
        }

        public CSLSaberTrail TrailInstance
        {
            get => trailInstance ?? null;
        }

        private CSLSaberTrail trailInstance;

        private SaberTrailRenderer defaultTrailRendererPrefab;
        private MeshRenderer defaultMeshRenderer;
        private int defaultSamplingFrequency;
        private int defaultGranularity;
        private SaberTrailRenderer defaultSaberTrailRenderer;
        private TrailElementCollection defaultTrailElementCollection;

        public bool CreateTrail(SaberTrail defaultTrail, Color saberTrailColor, GameObject customSaber)
        {
            switch (config.TrailType)
            {
                case TrailType.Custom:
                    return CreateCustomTrail(defaultTrail, saberTrailColor, customSaber);

                case TrailType.Vanilla:
                    SetupDefaultTrail(defaultTrail);
                    return true;

                default: return true;
            }
        }

        private bool CreateCustomTrail(SaberTrail defaultTrail, Color saberTrailColor, GameObject customSaber)
        {
            TryGetCustomTrail(customSaber, out CustomTrail customTrail);

            if (customTrail == null)
            {
                Logger.Warn("No custom trails. Defaulting to existing saber trails.");
                SetupDefaultTrail(defaultTrail);
                return true;
            }

            Logger.Debug($"Initializing custom trail to {defaultTrail.name}");
            trailInstance = customSaber.gameObject.AddComponent<CSLSaberTrail>();

            try
            {
                defaultTrailRendererPrefab = ReflectionUtil.GetField<SaberTrailRenderer, SaberTrail>(defaultTrail, "_trailRendererPrefab");
                defaultSaberTrailRenderer = ReflectionUtil.GetField<SaberTrailRenderer, SaberTrail>(defaultTrail, "_trailRenderer");
                defaultMeshRenderer = ReflectionUtil.GetField<MeshRenderer, SaberTrailRenderer>(defaultSaberTrailRenderer, "_meshRenderer");
                defaultSamplingFrequency = ReflectionUtil.GetField<int, SaberTrail>(defaultTrail, "_samplingFrequency");
                defaultGranularity = ReflectionUtil.GetField<int, SaberTrail>(defaultTrail, "_granularity");
                defaultTrailElementCollection = ReflectionUtil.GetField<TrailElementCollection, SaberTrail>(defaultTrail, "_trailElementCollection");
            }
            catch (Exception ex)
            {
                Logger.Error($"Problem encountered when getting fields from the default trail\n{ex}");
                return true;
            }

            // We will setup the trail values here
            trailInstance.Setup(customTrail.PointEnd, customTrail.PointStart);
            
            Color materialColor;
            MeshRenderer newMeshRenderer = defaultMeshRenderer;
            float trailIntensity = gameplaySetupData.playerSpecificSettings.saberTrailIntensity;

            // Swap material
            newMeshRenderer.material = customTrail.TrailMaterial;

            switch (customTrail.colorType)
            {
                case CustomSaber.ColorType.CustomColor:
                    materialColor = customTrail.TrailColor;
                    break;

                default:
                    materialColor = saberTrailColor;
                    materialColor.a = trailIntensity;
                    break;
            }

            newMeshRenderer.material.color = materialColor;

            // Adjusting the trail's meshrenderer before adding it to our trail
            ReflectionUtil.SetField(defaultSaberTrailRenderer, "_meshRenderer", newMeshRenderer);

            // Variables are null so set them
            trailInstance.TrailRendererPrefab = defaultTrailRendererPrefab;
            trailInstance.SamplingFrequency = defaultSamplingFrequency;
            trailInstance.Granularity = defaultGranularity;
            trailInstance.Color = saberTrailColor;
            trailInstance.MovementData = trailInstance.CustomTrailMovementData;
            trailInstance.TrailRenderer = defaultSaberTrailRenderer;
            trailInstance.TrailElementCollection = defaultTrailElementCollection;
            trailInstance.colorType = customTrail.colorType;
            trailUtils.SetTrailDuration(trailInstance);
            trailUtils.SetWhiteTrailDuration(trailInstance);

            return false;
        }

        private CustomTrail TryGetCustomTrail(GameObject customSaber, out CustomTrail customTrail)
        {
            try
            {
                customTrail = customSaber.GetComponent<CustomTrail>();
                Logger.Debug("Successfully got CustomTrail from custom saber.");
            }
            catch
            {
                customTrail = null;
            }
            return customTrail;
        }

        private void SetupDefaultTrail(SaberTrail defaultTrail)
        {
            trailUtils.SetTrailDuration(defaultTrail);
            trailUtils.SetWhiteTrailDuration(defaultTrail);
        }
    }
}
