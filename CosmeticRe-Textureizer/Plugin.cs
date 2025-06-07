using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Cosmetic_ReTextureizer
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private static Dictionary<string, Texture> _textureList = new();
        static List<GameObject[]> _thingsToSearch = new();
        private static string? _currentPath, _texturePath;
        Plugin()
        {
            new Harmony(PluginInfo.GUID).PatchAll(Assembly.GetExecutingAssembly());
            ThreadingHelper.Instance.StartAsyncInvoke(() => GetTextures);
        }

        void Start()
        {
            _currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (_currentPath != null) _texturePath = Path.Combine(_currentPath, "CosmeticTetxures");

            if (!Directory.Exists(_texturePath))
            {
                if (_texturePath != null) Directory.CreateDirectory(_texturePath);
            }
            CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs2 += Run;
        }
        void GetTextures()
        {
            if (_texturePath != null)
                foreach (var texture in Directory.GetFiles(_texturePath))
                {
                    Texture2D tex = new Texture2D(2, 2);
                    var imgdata = File.ReadAllBytes(texture);
                    tex.LoadImage(imgdata);
                    string fileNane = Path.GetFileNameWithoutExtension(texture);
                    tex.name = fileNane;
                    tex.filterMode = FilterMode.Point;
                    if (!_textureList.Keys.Contains(fileNane))
                    {
                        _textureList.Add(fileNane, tex);
                    }
                }
        }

        void Run()
        {
            _thingsToSearch.Add(GorillaTagger.Instance.offlineVRRig.cosmetics);
            _thingsToSearch.Add(GorillaTagger.Instance.offlineVRRig.overrideCosmetics);
            foreach (RigContainer rigC in VRRigCache.freeRigs)
            {
                _thingsToSearch.Add(rigC.Rig.cosmetics);
            }
            ThreadingHelper.Instance.StartAsyncInvoke(() => StartTextureApply);
        }

        void StartTextureApply() =>
            StartCoroutine(FindTexturesToApply());

        IEnumerator FindTexturesToApply()
        {
            try
            {
                foreach (GameObject[] things in _thingsToSearch)
                {
                    foreach (GameObject cosG in things)
                    {
                        if (_textureList.ContainsKey(cosG.name))
                        {
                            if (cosG.GetComponent<Renderer>() != null)
                            {
                                cosG.GetComponent<Renderer>().material.EnableKeyword("_USE_TEXTURE");
                                cosG.GetComponent<Renderer>().material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                                cosG.GetComponent<Renderer>().material.mainTexture = _textureList[cosG.name];
                            }
                            foreach (var r in cosG.GetComponentsInChildren<Renderer>())
                            {
                                r.material.EnableKeyword("_USE_TEXTURE");
                                r.material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                                r.material.mainTexture = _textureList[cosG.name];
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError($"IE-FindTexturesToApply::{exception.Message + exception.StackTrace}");
            }

            yield return "WaWa";
        }
    }
}