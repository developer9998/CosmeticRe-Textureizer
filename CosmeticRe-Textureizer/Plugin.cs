using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cosmetic_ReTextureizer.Patches;
using GorillaExtensions;
using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace Cosmetic_ReTextureizer;

[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    private string texturePath;

    private readonly Dictionary<string, CosmeticsController.CosmeticItem> cosmeticIdToItem = [];

    private readonly Dictionary<string, Texture> textures = [];

    private readonly List<GameObject[]> cosmeticCollection = [];

    public void Awake()
    {
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.GUID);

        texturePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CosmeticTetxures");

        if (!Directory.Exists(texturePath))
            Directory.CreateDirectory(texturePath);

        CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs2 += Initialize;
    }

    public void Initialize()
    {
        GetTextures();
        ReplaceTextures();
    }

    public void GetTextures()
    {
        Logger.LogInfo("GetTextures");

        string[] fileLocations = Directory.GetFiles(texturePath, "*.png", SearchOption.AllDirectories);

        for (int i = 0; i < fileLocations.Length; i++)
        {
            string path = fileLocations[i];

            string fileName = Path.GetFileNameWithoutExtension(path);

            CosmeticsController.CosmeticItem cosmeticItem = CosmeticsController.instance.GetItemFromDict(fileName);

            if (!cosmeticItem.isNullItem)
            {
                string displayName = CosmeticsController.instance.GetItemDisplayName(cosmeticItem);
                string destination = Path.Combine(Path.GetDirectoryName(path), string.Concat(displayName, ".png"));
                File.Move(path, destination);
                path = destination;
            }
            else
            {
                string itemId = CosmeticsController.instance.GetItemNameFromDisplayName(fileName);
                cosmeticItem = itemId == "null" ? CosmeticsController.instance.nullItem : CosmeticsController.instance.GetItemFromDict(itemId);
            }

            if (cosmeticItem.isNullItem || cosmeticIdToItem.ContainsValue(cosmeticItem))
                continue;

            cosmeticIdToItem.Add(cosmeticItem.itemName, cosmeticItem);

            Texture2D texture = new(2, 2)
            {
                name = CosmeticsController.instance.GetItemDisplayName(cosmeticItem),
                filterMode = FilterMode.Point
            };
            texture.LoadImage(File.ReadAllBytes(path));

            if (!textures.Keys.Contains(cosmeticItem.itemName))
            {
                Logger.LogInfo($"{cosmeticItem.itemName} \"{texture.name}\"");
                textures.Add(cosmeticItem.itemName, texture);
            }
        }
    }

    public void ReplaceTextures()
    {
        Logger.LogInfo("ReplaceTextures");

        HashSet<GameObject> localPlayerCosmetics = [];
        localPlayerCosmetics.UnionWith(GorillaTagger.Instance.offlineVRRig.cosmetics);
        localPlayerCosmetics.UnionWith(GorillaTagger.Instance.offlineVRRig.overrideCosmetics);
        cosmeticCollection.Add([.. localPlayerCosmetics]);

        foreach (RigContainer rigC in VRRigCache.freeRigs)
        {
            cosmeticCollection.Add(rigC.Rig.cosmetics);
        }

        StartTextureApply();
    }

    private void StartTextureApply()
    {
        Logger.LogInfo("StartTextureApply");

        Dictionary<GameObject, string> originalNameDict = OverrideCosmeticPatch.OverridenObjectLookup.ToDictionary(dict => dict.Value, dict => dict.Key);

        for (int i = 0; i < cosmeticCollection.Count; i++)
        {
            GameObject[] cosmeticArray = [.. cosmeticCollection[i]];

            foreach (GameObject cosG in cosmeticArray)
            {
                string objectName = originalNameDict.ContainsKey(cosG) ? originalNameDict[cosG] : cosG.name;

                if (textures.ContainsKey(objectName))
                {
                    Logger.LogInfo($"Found instance of {objectName} \"{CosmeticsController.instance.GetItemDisplayName(cosmeticIdToItem[objectName])}\"");
                    Logger.LogInfo(cosG.GetPath());

                    HashSet<Renderer> renderers = [];
                    if (cosG.GetComponent<Renderer>() is not null)
                        renderers.Add(cosG.GetComponent<Renderer>());
                    renderers.UnionWith(cosG.GetComponentsInChildren<Renderer>(true));

                    foreach (Renderer renderer in renderers)
                    {
                        Material material = renderer.material;

                        /*
                        if (material.shader.name != "GorillaTag/UberShader")
                        {
                            Logger.LogWarning($"Skipping {material.name} of shader {material.shader.name}");
                            continue;
                        }

                        Texture2DArray array = (Texture2DArray)renderer.material.GetTexture("_BaseMap_Atlas");
                        Texture2D texture = await material.ToTexture(array.width, array.height);

                        var bytes = texture.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(_currentPath, $"{CosmeticsController.instance.GetItemDisplayName(cosmeticIdToItem[cosG.name])}.png"), bytes);
                        Logger.LogInfo($"Done {cosG.name}");
                        */

                        material.EnableKeyword("_USE_TEXTURE");
                        material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                        material.mainTexture = textures[objectName];
                    }
                }
            }
        }
    }
}