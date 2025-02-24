using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using GorillaNetworking;
using HarmonyLib;
using OVR.OpenVR;
using UnityEngine;
using Utilla;

namespace CosmeticRe_Textureizer
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
	public class Plugin : BaseUnityPlugin
	{
		public static Dictionary<string, Texture> TextureList = new Dictionary<string, Texture>();
        static List<GameObject> ReTexturedGameObjects = new List<GameObject>();
		string CurrentPath, TexturePath ,DumpPath;
        bool ran;
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
            foreach (var texture in Directory.GetFiles(TexturePath))
			{
                Texture2D tex = new Texture2D(2, 2);
                var imgdata = File.ReadAllBytes(texture);
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

        void LateUpdate()
        {
            if (CosmeticsController.instance.allCosmeticsDict_isInitialized && !ran)
            {
                FindTexturesToApply(GorillaTagger.Instance.offlineVRRig.cosmetics);
                FindTexturesToApply(GorillaTagger.Instance.offlineVRRig.overrideCosmetics);
                foreach (RigContainer rigC in VRRigCache.freeRigs)
                {
                    FindTexturesToApply(rigC.Rig.cosmetics);
                }
                ran = true;
            }
        }
        void FindTexturesToApply(GameObject[] cos)
        {
            foreach (GameObject cosG in cos)
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
        void ApplyTetxure(GameObject g, Texture t)
        {
            if (g.GetComponent<Renderer>() != null)
            {
                g.GetComponent<Renderer>().material.EnableKeyword("_USE_TEXTURE");
                g.GetComponent<Renderer>().material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                g.GetComponent<Renderer>().material.mainTexture = t;
            }
            foreach (var rend in g.GetComponentsInChildren<Renderer>())
            {
                g.GetComponent<Renderer>().material.EnableKeyword("_USE_TEXTURE");
                g.GetComponent<Renderer>().material.DisableKeyword("_USE_TEX_ARRAY_ATLAS");
                g.GetComponent<Renderer>().material.mainTexture = t;
            }
            ReTexturedGameObjects.Add(g);
        }
	}
}
