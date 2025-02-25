using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace CosmeticRe_Textureizer
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
	public class Plugin : BaseUnityPlugin
	{
		public static Dictionary<string, Texture> TextureList = new Dictionary<string, Texture>();
        static List<GameObject> ReTexturedGameObjects = new List<GameObject>();
        static List<GameObject[]> ThingsToSearch = new List<GameObject[]>();
		static string CurrentPath, TexturePath ,DumpPath;
        Action asyncTextureFind;
        static Action asyncApplyTextures;
		Plugin()
		{
			new Harmony(PluginInfo.GUID).PatchAll(Assembly.GetExecutingAssembly());
			CurrentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			TexturePath = Path.Combine(CurrentPath, "CosmeticTetxures");
            DumpPath = Path.Combine(CurrentPath, "DUMP");
        }

        void Start()
		{
            if (!Directory.Exists(TexturePath))
            {
                Directory.CreateDirectory(TexturePath);
            }
            asyncTextureFind += GetTextures;
            asyncApplyTextures += FindTexturesToApply;
            ThreadingHelper.Instance.StartAsyncInvoke(delegate { return asyncTextureFind; });
            CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs2 += Run;
        }
        async void GetTextures()
        {
            foreach (var texture in Directory.GetFiles(TexturePath))
            {
                Texture2D tex = new Texture2D(2, 2);
                var imgdata = await File.ReadAllBytesAsync(texture);
                tex.LoadImage(imgdata);
                string name = Path.GetFileNameWithoutExtension(texture);
                tex.name = name;
                tex.filterMode = FilterMode.Point;
                if (!TextureList.Keys.Contains(name))
                {
                    TextureList.Add(name, tex);
                }
            }
        }

        static void Run()
        {
            ThingsToSearch.Add(GorillaTagger.Instance.offlineVRRig.cosmetics);
            ThingsToSearch.Add(GorillaTagger.Instance.offlineVRRig.cosmetics);
            ThingsToSearch.Add(GorillaTagger.Instance.offlineVRRig.overrideCosmetics);
            foreach (RigContainer rigC in VRRigCache.freeRigs)
            {
                ThingsToSearch.Add(rigC.Rig.cosmetics);
            }
            ThreadingHelper.Instance.StartAsyncInvoke(delegate { return asyncApplyTextures; });
        }
        static void FindTexturesToApply()
        {
            foreach (GameObject[] things in ThingsToSearch)
            {
                foreach (GameObject cosG in things)
                {
                    if (TextureList.ContainsKey(cosG.name))
                    {
                        if (cosG.GetComponent<Renderer>() != null)
                        {
                            cosG.GetComponent<Renderer>().material.EnableKeyword("_USE_TEXTURE");
                            cosG.GetComponent<Renderer>().material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                            cosG.GetComponent<Renderer>().material.mainTexture = TextureList[cosG.name];
                        }
                        foreach (var r in cosG.GetComponentsInChildren<Renderer>())
                        {
                            r.material.EnableKeyword("_USE_TEXTURE");
                            r.material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                            r.material.mainTexture = TextureList[cosG.name];
                        }
                        ReTexturedGameObjects.Add(cosG);
                    }
                }
            }
        }
	}
}
