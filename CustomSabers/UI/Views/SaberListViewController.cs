﻿using System;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using CustomSaber.Data;
using CustomSaber.Utilities;
using CustomSaber.Configuration;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using BeatSaberMarkupLanguage.TypeHandlers;

namespace CustomSaber.UI
{
    internal class SaberListViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomSaber.UI.BSML.saberList.bsml";

        public static SaberListViewController Instance;

        private static readonly Sprite nullCoverImage = CustomSaberUtils.GetNullCoverImage();

        [UIComponent("saber-list")]
        public CustomListTableData customListTableData;

        [UIComponent("reload-button")]
        public Selectable reloadButtonSelectable;

        [UIComponent("delete-saber-modal")]
        public ModalView deleteSaberModal;

        [UIComponent("delete-saber-modal-text")]
        public TextMeshProUGUI deleteSaberModalText;

        [UIAction("select-saber")]
        public void Select(TableView _, int row)
        {
            Plugin.Log.Debug($"saber selected at row {row}");
            CustomSaberAssetLoader.SelectedSaberIndex = row;
            CustomSaberConfig.Instance.CurrentlySelectedSaber = CustomSaberAssetLoader.SabersMetadata[row].SaberFileName;

            // currently loading saber on game load, probably should do it on saber select instead
            // that can be used for saber previewing
        }

        [UIAction("open-in-explorer")]
        public void OpenInExplorer()
        {
            System.Diagnostics.Process.Start("explorer.exe", PluginDirs.CustomSabers.FullName);
        }

        [UIAction("show-delete-saber-modal")]
        public void ShowDeleteSaberModal()
        {
            if (CustomSaberConfig.Instance.CurrentlySelectedSaber == "Default")
            {
                Plugin.Log.Warn("You can't delete the default sabers! >:(");
                return;
            }

            deleteSaberModalText.text = $"Are you sure you want to delete\n{ Path.GetFileNameWithoutExtension(CustomSaberConfig.Instance.CurrentlySelectedSaber) }?";
            deleteSaberModal.Show(true);
        }

        [UIAction("hide-delete-saber-modal")]
        public void HideDeleteSaberModal()
        {
            deleteSaberModal.Hide(true);
        }

        [UIAction("delete-selected-saber")]
        public void DeleteSelectedSaber()
        {
            HideDeleteSaberModal();

            if (CustomSaberConfig.Instance.CurrentlySelectedSaber != "Default")
            {
                string currentSaberPath = Path.Combine(PluginDirs.CustomSabers.FullName, CustomSaberConfig.Instance.CurrentlySelectedSaber);
                string destinationPath = Path.Combine(PluginDirs.DeletedSabers.FullName, CustomSaberConfig.Instance.CurrentlySelectedSaber);
                try
                {
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);

                    if (File.Exists(currentSaberPath))
                    {
                        Plugin.Log.Debug($"Moving {currentSaberPath}\nto {PluginDirs.DeletedSabers.FullName}");

                        File.Move(currentSaberPath, destinationPath);

                        CustomSaberAssetLoader.SabersMetadata.RemoveAt(CustomSaberAssetLoader.SelectedSaberIndex);

                        Select(customListTableData.tableView, CustomSaberAssetLoader.SelectedSaberIndex - 1);

                        SetupList();
                    }
                    else
                    {
                        Plugin.Log.Warn($"Saber doesn't exist at {currentSaberPath}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error("Problem encountered when trying to delete selected saber");
                    Plugin.Log.Error(ex);
                }
            }
        }

        [UIAction("reload-sabers")]
        public async void ReloadSabers()
        {
            reloadButtonSelectable.interactable = false;
            await CustomSaberAssetLoader.ReloadAsync();
            SetupList();
            Select(customListTableData.tableView, CustomSaberAssetLoader.SelectedSaberIndex);
            reloadButtonSelectable.interactable = true;
        }

        [UIAction("#post-parse")]
        public void PostParse()
        {
            SetupList();
        }

        private void SetupList() // todo - smoother saber list refresh
        {
            customListTableData.data.Clear();

            Plugin.Log.Debug("Showing list of selectable sabers");

            for (int i = 0; i < CustomSaberAssetLoader.SabersMetadata.Count; i++)
            {
                CustomSaberMetadata metadata = CustomSaberAssetLoader.SabersMetadata[i];
                
                if (metadata.SaberFileName != null)
                {
                    if (!File.Exists(Path.Combine(PluginDirs.CustomSabers.FullName, metadata.SaberFileName))) continue;
                }

                Plugin.Log.Debug($"#{i+1} \"{metadata.SaberName}\"");

                Sprite cover = null;
                if (metadata.CoverImage != null)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(metadata.CoverImage);
                    cover = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                }
                else
                {
                    cover = nullCoverImage;
                }

                if (metadata.SaberName == "Default") cover = CustomSaberUtils.GetDefaultCoverImage(); 

                var customCellInfo = new CustomListTableData.CustomCellInfo(metadata.SaberName, metadata.AuthorName, cover);
                customListTableData.data.Add(customCellInfo);
            }

            customListTableData.tableView.ReloadData();

            int selectedSaber = CustomSaberAssetLoader.SelectedSaberIndex;
            customListTableData.tableView.SelectCellWithIdx(selectedSaber);

            if (!customListTableData.tableView.visibleCells.Where(x => x.selected).Any())
            {
                customListTableData.tableView.ScrollToCellWithIdx(selectedSaber, TableView.ScrollPositionType.Beginning, true);
            }
        }
    }
}