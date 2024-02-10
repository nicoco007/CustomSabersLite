﻿using CustomSaber.Utilities;
using System.IO;
using UnityEngine;
using AssetBundleLoadingTools.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CustomSaber.Data
{
    public class CustomSaberData
    {
        public string FileName { get; }

        public AssetBundle AssetBundle { get; }

        public GameObject Sabers { get; }

        public SaberDescriptor Descriptor { get; }

        //Find out where to load the saber object
        //Pass the saber object to the constructor?

        public CustomSaberData(string fileName)
        {
            FileName = fileName;

            if (FileName != "DefaultSabers")
            {
                try
                {
                    AssetBundle = AssetBundle.LoadFromFile(Path.Combine(Plugin.CustomSaberAssetsPath, fileName));
                    Sabers = AssetBundle.LoadAsset<GameObject>("_CustomSaber");

                    List<Material> materials = ShaderRepair.GetMaterialsFromGameObjectRenderers(Sabers);

                    // Manually add CustomTrails to materials list
                    foreach (var customTrail in Sabers.GetComponentsInChildren<CustomTrail>(true))
                    {
                        if (!materials.Contains(customTrail.TrailMaterial))
                        {
                            materials.Add(customTrail.TrailMaterial);
                        }
                    }
                    var replacementInfo = ShaderRepair.FixShadersOnMaterials(materials);
                    if (!replacementInfo.AllShadersReplaced)
                    {
                        Plugin.Log.Warn($"Missing shader replacement data for {fileName}:");
                        foreach (var shaderName in replacementInfo.MissingShaderNames)
                        {
                            Plugin.Log.Warn($"\t- {shaderName}");
                        }
                    }

                    Descriptor = Sabers.GetComponent<SaberDescriptor>();
                    Descriptor.CoverImage = Descriptor.CoverImage ?? CustomSaberUtils.GetNullCoverImage();
                }
                catch
                {
                    Plugin.Log.Warn($"Problem encountered when getting the AssetBundle for {fileName}");

                    Descriptor = new SaberDescriptor
                    {
                        SaberName = "Invalid Saber",
                        AuthorName = fileName
                    };

                    FileName = "DefaultSabers";
                }
            }
            else
            {
                Descriptor = new SaberDescriptor
                {
                    SaberName = "Default",
                    AuthorName = "Beat Games",
                    Description = "Default Sabers",
                    CoverImage = ImageLoading.LoadSpriteFromResources("CustomSaber.Resources.defaultsabers-image.png")
                };
            }
        }

        /*private static async Task<GameObject> LoadSaberFromAssetAsync(string fileName)
        {
            string filePath = Path.Combine(Plugin.CustomSaberAssetsPath, fileName);

            Plugin.Log.Info($"1");
            //Load bundle from file
            var bundle = await AssetBundleExtensions.LoadFromFileAsync(filePath);

            Plugin.Log.Info("2");
            //Load saber object from asset bundle
            var saberObject = await AssetBundleExtensions.LoadAssetAsync<GameObject>(bundle, "_CustomSaber");
            Plugin.Log.Info("3");
            //List of materials from the saber
            List<Material> customSaberMaterials = ShaderRepair.GetMaterialsFromGameObjectRenderers(saberObject);
            Plugin.Log.Info("4");
            //Add CustomTrails to materials list
            foreach (var customTrail in saberObject.GetComponentsInChildren<CustomTrail>(true))
            {
                if (!customSaberMaterials.Contains(customTrail.TrailMaterial))
                {
                    customSaberMaterials.Add(customTrail.TrailMaterial);
                }
            }
            Plugin.Log.Info("5");
            //Fix shaders by comparing against .shaderbundle library 
            var replacementInfo = await ShaderRepair.FixShadersOnMaterialsAsync(customSaberMaterials);

            if (!replacementInfo.AllShadersReplaced)
            {
                Plugin.Log.Warn($"Missing shader replacement data for {fileName}");
            }

            *//*foreach (Material trailMaterial in customSaberMaterials)
            {
                var trailInfo = await ShaderRepair.FixShaderOnMaterialAsync(trailMaterial);

                if (!trailInfo.AllShadersReplaced)
                {
                    Plugin.Log.Warn("Missing trail shader replacement data. Will use default trails.");
                }
            }*//*

            return saberObject;
        }*/

        public void Destroy()
        {
            if (AssetBundle != null)
            {
                AssetBundle.Unload(true);
            }
            else
            {
                UnityEngine.Object.Destroy(Descriptor);
            }
        }
    }
}
