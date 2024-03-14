﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.GameplaySetup;
using CustomSaber.Configuration;
using CustomSaber.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
// using Zenject;
using BeatSaberMarkupLanguage.Components;
using CustomSaber.Utilities;
using System.IO;
using HMUI;
using System.Text.RegularExpressions;

namespace CustomSaber.UI
{
    internal class GameplaySetupTab /*: IInitializable, IDisposable*/
    {
        private string resourceName => "CustomSaber.UI.BSML.playerSettingsTab.bsml";
        private bool parsed;

        #region trail settings
        [UIComponent("trail-duration")]
        private GenericInteractableSetting trailDurationInteractable;

        [UIComponent("trail-duration")]
        private TextMeshProUGUI trailDurationText;

        [UIComponent("trail-type")]
        private RectTransform trailTypeRT;

        [UIValue("trail-type-list")]
        public List<object> trailType = Enum.GetNames(typeof(TrailType)).ToList<object>();

        [UIValue("disable-white-trail")]
        public bool DisableWhiteTrail
        {
            get => CustomSaberConfig.Instance.DisableWhiteTrail;
            set => CustomSaberConfig.Instance.DisableWhiteTrail = value;
        }

        [UIValue("override-trail-duration")]
        public bool OverrideTrailDuration
        {
            get => CustomSaberConfig.Instance.OverrideTrailDuration;
            set
            {
                CustomSaberConfig.Instance.OverrideTrailDuration = value;
                SetTrailDurationInteractable(value);
            }
        }

        [UIValue("trail-duration")]
        public int TrailDurationMultiplier
        {
            get => CustomSaberConfig.Instance.TrailDuration;
            set => CustomSaberConfig.Instance.TrailDuration = value;
        }

        [UIValue("trail-type")]
        public string TrailType
        {
            get => CustomSaberConfig.Instance.TrailType.ToString();
            set => CustomSaberConfig.Instance.TrailType = Enum.TryParse(value, out TrailType trailType) ? trailType : CustomSaberConfig.Instance.TrailType;
        }
        
        private void SetTrailDurationInteractable(bool value)
        {
            if (parsed)
            {
                trailDurationText.color = new Color(1f, 1f, 1f, value ? 1f : 0.5f);
                trailDurationInteractable.interactable = OverrideTrailDuration;
            }
        }
        #endregion

        #region saber settings
        [UIValue("enable-custom-events")]
        public bool CustomEventsEnabled
        {
            get => CustomSaberConfig.Instance.CustomEventsEnabled;
            set => CustomSaberConfig.Instance.CustomEventsEnabled = value;
        }
        #endregion


        [UIComponent("saber-list")]
        public CustomListTableData saberList;

        [UIAction("select-saber")]
        public void Select(TableView t, int row)
        {
            Plugin.Log.Info($"Saber selected at row {row}");
            CustomSaberAssetLoader.SelectedSaberIndex = row;
            CustomSaberConfig.Instance.CurrentlySelectedSaber = CustomSaberAssetLoader.SabersMetadata[row].SaberFileName;

            t.SelectCellWithIdx(row);
        }

        #region tabs
        [UIComponent("settings-title")]
        TextMeshProUGUI settingsTitleText;

        [UIComponent("trail-settings-panel")]
        RectTransform trailSettingsPanel;

        [UIComponent("saber-settings-panel")]
        RectTransform saberSettingsPanel;

        private enum ActiveSettingsTab
        {
            Trail,
            Saber
        }

        private ActiveSettingsTab activeSettingsTab = ActiveSettingsTab.Trail;

        [UIAction("show-trail-settings")]
        public void ShowTrailSettings()
        {
            if (!activeSettingsTab.Equals(ActiveSettingsTab.Trail))
            {
                Plugin.Log.Info("Changing tab");
                activeSettingsTab = ActiveSettingsTab.Trail;
                settingsTitleText.text = "Trail Settings";
                saberSettingsPanel.gameObject.SetActive(false);
                trailSettingsPanel.gameObject.SetActive(true);
            }
        }
        
        [UIAction("show-saber-settings")]
        public void ShowSaberSettings()
        {
            if (!activeSettingsTab.Equals(ActiveSettingsTab.Saber))
            {
                Plugin.Log.Info("Changing tab");
                activeSettingsTab = ActiveSettingsTab.Saber;
                settingsTitleText.text = "Saber Settings";
                trailSettingsPanel.gameObject.SetActive(false);
                saberSettingsPanel.gameObject.SetActive(true);
            }
        }
        #endregion

        [UIAction("#post-parse")]
        public void PostParse()
        {
            parsed = true;
            SetComponentSettings();
            SetupList();
        }

        private void SetComponentSettings()
        {
            SetTrailDurationInteractable(OverrideTrailDuration);

            // Saber Trail Type list setting 
            RectTransform trailTypePickerRect = trailTypeRT.gameObject.transform.Find("ValuePicker").GetComponent<RectTransform>();
            RectTransform trailTypeTextRect = trailTypeRT.gameObject.transform.Find("NameText").GetComponent<RectTransform>();
            trailTypePickerRect.sizeDelta = new Vector2(30, trailTypePickerRect.sizeDelta.y);
            trailTypeTextRect.sizeDelta = new Vector2(0, trailTypeTextRect.sizeDelta.y);

            // Trail Duration slider setting
            RectTransform trailDurationRect = trailDurationText.transform.parent.transform.Find("BSMLSlider").GetComponent<RectTransform>();
            trailDurationRect.sizeDelta = new Vector2(50, trailDurationRect.sizeDelta.y);
        }

        private void SetupList()
        {
            saberList.data.Clear();
            for (int i = 0; i < CustomSaberAssetLoader.SabersMetadata.Count; i++) 
            {
                CustomSaberMetadata metadata = CustomSaberAssetLoader.SabersMetadata[i];
                string saberName = metadata.SaberName;

                if (metadata.SaberFileName != null)
                {
                    if (!File.Exists(Path.Combine(PluginDirs.CustomSabers.FullName, metadata.SaberFileName))) continue;
                }

                // Remove TMPro rich text tags
                Regex regex = new Regex(@"<[^>]*>");
                if (regex.IsMatch(saberName))
                {
                    saberName = regex.Replace(saberName, string.Empty);
                    saberName.Trim();
                }

                int maxLength = 21;
                saberName = saberName.Length <= maxLength ? saberName : saberName.Substring(0, maxLength - 1).Trim() + "...";

                var customCellInfo = new CustomListTableData.CustomCellInfo(saberName);
                saberList.data.Add(customCellInfo);
            }

            saberList.tableView.ReloadData();
            
            int selectedSaber = CustomSaberAssetLoader.SelectedSaberIndex;
            saberList.tableView.SelectCellWithIdx(selectedSaber);

            if (!saberList.tableView.visibleCells.Where(x => x.selected).Any())
            {
                saberList.tableView.ScrollToCellWithIdx(selectedSaber, TableView.ScrollPositionType.Beginning, true);
            }
        }

        /*public void Initialize()
        {
            GameplaySetup.instance.AddTab("Custom Sabers", resourceName, this);
        }

        public void Dispose()
        {
            GameplaySetup.instance.RemoveTab("Custom Sabers");
        }*/
    }
}
