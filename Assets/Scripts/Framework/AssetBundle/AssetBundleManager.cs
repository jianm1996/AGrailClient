﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Framework.AssetBundle
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class AssetBundleManager : MonoBehaviour
    {
        private static AssetBundleManager instance = null;
        public static AssetBundleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AssetBundleMgr");
                    instance = go.AddComponent<AssetBundleManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public static bool SimulationMode = false;
        static AssetBundleManager()
        {
#if !UNITY_EDITOR
            SimulationMode = false;
#endif
        }

        private string manifestFileName
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#else
                return Application.platform.ToString();
#endif
            }
        }

        private const string remoteSrv = "http://10.0.2.114:7888/";
        private AssetBundleManifest localManifest = null;
        private AssetBundleManifest remoteManifest = null;
        private Dictionary<string, UnityEngine.AssetBundle> bundles = new Dictionary<string, UnityEngine.AssetBundle>();


        void Init()
        {
            Debug.LogFormat("SimulationMode = {0}", SimulationMode);
            Caching.maximumAvailableDiskSpace = 200 * 1024 * 1024;
        }

        public void LoadManifest()
        {
            StartCoroutine(LoadManifestCoro());
        }

        public IEnumerator LoadManifestCoro()
        {
            UnityWebRequest oldWww = UnityWebRequest.GetAssetBundle(Application.streamingAssetsPath + "/" + manifestFileName + "/" + manifestFileName);
            yield return oldWww.Send();
            if (oldWww.isError)
            {

            }
            localManifest = (oldWww.downloadHandler as DownloadHandlerAssetBundle).assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

            UnityWebRequest newWww = UnityWebRequest.GetAssetBundle(remoteSrv + manifestFileName + "/" + manifestFileName);
            yield return newWww.Send();
            remoteManifest = (newWww.downloadHandler as DownloadHandlerAssetBundle).assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (newWww.isError)
            {

            }
            if (SimulationMode)
                yield break;

            //两个Manifest进行比较
            //若有不同则针对不同进行下载
            //这里相当于直接加载及解压了所有AssetBundles
            foreach (var v in remoteManifest.GetAllAssetBundles())
                yield return StartCoroutine(downloadAssetBundle(v));
        }

        public GameObject LoadAsset(string assetbundleName, string assetName)
        {
#if UNITY_EDITOR
            if (SimulationMode)
            {
                var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetbundleName, assetName);
                if (assetPaths.Length == 0)
                {
                    Debug.LogErrorFormat("There is no asset with name {0}/{1}", assetbundleName, assetName);
                    return null;
                }
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[0]);
            }
            else if (bundles.ContainsKey(assetbundleName))
                return bundles[assetbundleName].LoadAsset<GameObject>(assetName);
            else
            {
                Debug.LogErrorFormat("There is no asset with name {0}/{1}", assetbundleName, assetName);
                return null;
            }
#else
            if (bundles.ContainsKey(assetbundleName))
                return bundles[assetbundleName].LoadAsset<GameObject>(assetName);
            else
            {
                Debug.LogErrorFormat("There is no asset with name {0}/{1}", assetbundleName, assetName);
                return null;
            }
#endif
        }

        public void LoadAssetAsyn(string assetbundleName, string assetName, LoadAssetCB cb)
        {
#if UNITY_EDITOR
            if (SimulationMode)
                LoadAsset(assetbundleName, assetbundleName);
            else
                StartCoroutine(LoadAssetAsynCoro(assetbundleName, assetName, cb));
#else
            StartCoroutine(loadAssetAsyn(assetbundleName, assetName, cb));
#endif
        }

        public IEnumerator LoadAssetAsynCoro(string assetbundleName, string assetName, LoadAssetCB cb)
        {
            if (bundles.ContainsKey(assetbundleName))
            {
                var req = bundles[assetbundleName].LoadAssetAsync<GameObject>(assetName);
                yield return req;
                cb.Cb(req.asset as GameObject);
            }
            else
                Debug.LogErrorFormat("There is no asset with name {0}/{1}", assetbundleName, assetName);
        }

        private IEnumerator downloadAssetBundle(string bundleName)
        {
            string uri = "";
            UnityWebRequest www = null;
            if (localManifest.GetAllAssetBundles().Contains(bundleName) &&
                remoteManifest.GetAssetBundleHash(bundleName) == localManifest.GetAssetBundleHash(bundleName))
                uri = Application.streamingAssetsPath + "/" + manifestFileName + "/" + bundleName;
            else
                uri = remoteSrv + "/" + manifestFileName + "/" + bundleName;

            www = UnityWebRequest.GetAssetBundle(uri, remoteManifest.GetAssetBundleHash(bundleName), 0);
            yield return www.Send();
            if (www.isError)
                Debug.LogErrorFormat("Download bundle {0} from {1} failed.", bundleName, uri);
            else
            {
                Debug.LogFormat("Download bundle {0} from {1} succeed.", bundleName, uri);
                bundles.Add(bundleName, DownloadHandlerAssetBundle.GetContent(www));
            }
        }
    }

    public class LoadAssetCB
    {
        public Action<GameObject> Cb = null;
    }
}


