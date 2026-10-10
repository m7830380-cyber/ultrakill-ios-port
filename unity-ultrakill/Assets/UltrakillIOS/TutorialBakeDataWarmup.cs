#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// AssetRipper BakedData ScriptableObjects load as Missing Script (type=NULL in Editor pack log).
    /// Unity/AssetRipper docs: SO fields need matching script assemblies — DLL fileID refs often fail.
    /// Web ports (Cake Logic Prelude) hit the same class of problem with ripped shaders/scripts.
    /// Workaround: load mesh+atlases from companion bundle + JSON index lists; CreateInstance StaticSceneData.
    /// </summary>
    internal static class TutorialBakeDataWarmup
    {
        private const string Area = "BakeWarmup";
        private static AssetBundle _bundle;
        private static object _createdData;
        private static bool _tried;

        [Serializable]
        private class BakeJson
        {
            public string meshNameContains;
            public string mainAtlasName;
            public string blendAtlasName;
            public int[] backingMeshHashes;
            public int[] mrLightIndices;
            public int[] mrMeshIndices;
            public int[] firstSubMesh;
        }

        public static void EnsureLoaded()
        {
            if (_tried)
            {
                return;
            }

            _tried = true;
            var root = ExternalContentBootstrap.ContentStreamingAssetsPath;
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            var path = Path.Combine(root, "aa", "iOS", "specialscenes_scenes_tutorial_bakedata.bundle");
            if (!File.Exists(path))
            {
                UltrakillLog.Warn(Area, "Missing bake companion: " + path);
                return;
            }

            _bundle = AssetBundle.LoadFromFile(path);
            if (_bundle == null)
            {
                UltrakillLog.Warn(Area, "LoadFromFile failed: " + path);
                return;
            }

            var assets = _bundle.LoadAllAssets();
            UltrakillLog.Info(Area, "Loaded bake companion assets=" + (assets != null ? assets.Length : 0)
                + " sizeMB=" + (new FileInfo(path).Length / (1024f * 1024f)).ToString("F2"));

            _createdData = BuildStaticSceneData(assets);
            if (_createdData != null)
            {
                UltrakillLog.Info(Area, "Created StaticSceneData from JSON+mesh (bypassed Missing Script SO)");
            }
            else
            {
                UltrakillLog.Warn(Area, "Failed to assemble StaticSceneData from companion (need tutorial_bake.json.txt + mesh)");
            }
        }

        public static object GetCreatedStaticSceneData()
        {
            EnsureLoaded();
            return _createdData;
        }

        private static object BuildStaticSceneData(UnityEngine.Object[] assets)
        {
            var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
            if (dataType == null)
            {
                return null;
            }

            var json = LoadBakeJson(assets);
            if (json == null || json.firstSubMesh == null || json.firstSubMesh.Length == 0)
            {
                return null;
            }

            Mesh bakeMesh = null;
            Texture2D main = null;
            Texture2D blend = null;
            var meshKey = string.IsNullOrEmpty(json.meshNameContains) ? "StaticSceneOptimizer" : json.meshNameContains;
            var mainKey = string.IsNullOrEmpty(json.mainAtlasName) ? "Texture2D_2" : json.mainAtlasName;
            var blendKey = string.IsNullOrEmpty(json.blendAtlasName) ? "Texture2D_1" : json.blendAtlasName;

            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is Mesh m && bakeMesh == null
                        && m.name.IndexOf(meshKey, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bakeMesh = m;
                    }
                    else if (a is Texture2D t)
                    {
                        if (main == null && string.Equals(t.name, mainKey, StringComparison.Ordinal))
                        {
                            main = t;
                        }
                        else if (blend == null && string.Equals(t.name, blendKey, StringComparison.Ordinal))
                        {
                            blend = t;
                        }
                    }
                }
            }

            if (bakeMesh == null)
            {
                foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>())
                {
                    if (m != null && m.name.IndexOf(meshKey, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bakeMesh = m;
                        break;
                    }
                }
            }

            if (main == null || blend == null)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (t == null)
                    {
                        continue;
                    }

                    if (main == null && string.Equals(t.name, mainKey, StringComparison.Ordinal))
                    {
                        main = t;
                    }
                    else if (blend == null && string.Equals(t.name, blendKey, StringComparison.Ordinal))
                    {
                        blend = t;
                    }
                }
            }

            if (bakeMesh == null || main == null)
            {
                UltrakillLog.Warn(Area, "mesh=" + (bakeMesh != null ? bakeMesh.name : "NULL")
                    + " main=" + (main != null ? main.name : "NULL")
                    + " blend=" + (blend != null ? blend.name : "NULL"));
                return null;
            }

            var data = ScriptableObject.CreateInstance(dataType);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            dataType.GetField("mainTexAtlas", flags)?.SetValue(data, main);
            dataType.GetField("blendTexAtlas", flags)?.SetValue(data, blend ?? main);

            var meshes = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(Mesh)));
            meshes.Add(bakeMesh);
            dataType.GetField("bakedMeshes", flags)?.SetValue(data, meshes);

            SetIntList(dataType, data, "backingMeshHashes", json.backingMeshHashes, flags);
            SetIntList(dataType, data, "mrLightIndices", json.mrLightIndices, flags);
            SetUShortList(dataType, data, "mrMeshIndices", json.mrMeshIndices, flags);
            SetUShortList(dataType, data, "firstSubMesh", json.firstSubMesh, flags);

            UltrakillLog.Info(Area, "Assembled bake mesh='" + bakeMesh.name + "' submeshes=" + bakeMesh.subMeshCount
                + " firstSubMeshN=" + json.firstSubMesh.Length + " atlas=" + main.name);
            return data;
        }

        private static BakeJson LoadBakeJson(UnityEngine.Object[] assets)
        {
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is TextAsset ta && a.name.IndexOf("tutorial_bake", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            return JsonUtility.FromJson<BakeJson>(ta.text);
                        }
                        catch (Exception ex)
                        {
                            UltrakillLog.Warn(Area, "JSON parse TextAsset failed: " + ex.Message);
                        }
                    }
                }
            }

            var root = ExternalContentBootstrap.ContentStreamingAssetsPath;
            var loose = Path.Combine(root, "aa", "iOS", "tutorial_bake.json");
            if (File.Exists(loose))
            {
                try
                {
                    return JsonUtility.FromJson<BakeJson>(File.ReadAllText(loose));
                }
                catch (Exception ex)
                {
                    UltrakillLog.Warn(Area, "JSON parse file failed: " + ex.Message);
                }
            }

            return null;
        }

        private static void SetIntList(Type dataType, object data, string field, int[] values, BindingFlags flags)
        {
            if (values == null)
            {
                return;
            }

            var list = new List<int>(values.Length);
            list.AddRange(values);
            dataType.GetField(field, flags)?.SetValue(data, list);
        }

        private static void SetUShortList(Type dataType, object data, string field, int[] values, BindingFlags flags)
        {
            if (values == null)
            {
                return;
            }

            var list = new List<ushort>(values.Length);
            foreach (var v in values)
            {
                list.Add((ushort)v);
            }

            dataType.GetField(field, flags)?.SetValue(data, list);
        }
    }
}
#endif
