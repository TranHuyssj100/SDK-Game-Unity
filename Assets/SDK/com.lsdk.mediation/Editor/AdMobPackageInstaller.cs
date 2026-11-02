using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace LSDK.Mediation.EditorTools
{
    /// <summary>
    /// Project khác cài mediation sẽ tự import ThirdParty/GoogleMobileAds-v11.6.0.unitypackage
    /// nếu chưa có Google Mobile Ads. File .unitypackage phải nằm trong package này.
    /// </summary>
    [InitializeOnLoad]
    public static class AdMobPackageInstaller
    {
        private const string PackageFileName = "GoogleMobileAds-v11.6.0.unitypackage";
        private const string SessionKey = "LSDK.Mediation.AdMobPackageImport";

        static AdMobPackageInstaller()
        {
            AssetDatabase.importPackageCompleted += OnPackageImported;
            AssetDatabase.importPackageFailed += OnPackageFailed;
            EditorApplication.delayCall += () => TryImport(false);
        }

        [MenuItem("Tools/LSDK/Mediation/Import AdMob Package")]
        public static void ImportFromMenu()
        {
            TryImport(true);
        }

        private static void TryImport(bool force)
        {
            if (IsAdMobInstalled()) return;
            if (!force && SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            string path = FindPackageFile();
            if (string.IsNullOrEmpty(path))
            {
                const string message = "Chưa có ThirdParty/GoogleMobileAds-v11.6.0.unitypackage trong package mediation. " +
                                        "Bỏ file .unitypackage của Google vào đó rồi cài lại SDK.";
                DebugUtils.Warning(message);
                if (force) EditorUtility.DisplayDialog("LSDK Mediation", message, "OK");
                return;
            }

            DebugUtils.Success("Đang cài Google Mobile Ads từ unitypackage.");
            AssetDatabase.ImportPackage(path, false);
        }

        private static void OnPackageImported(string packageName)
        {
            if (!IsAdMobPackage(packageName)) return;
            EditorApplication.delayCall += () =>
            {
                DebugUtils.Success("Đã cài Google Mobile Ads.");
                MediationConfigMenu.DefineSymbols();
            };
        }

        private static void OnPackageFailed(string packageName, string errorMessage)
        {
            if (!IsAdMobPackage(packageName)) return;
            SessionState.EraseBool(SessionKey);
            DebugUtils.Error($"Cài Google Mobile Ads thất bại: {errorMessage}");
        }

        private static bool IsAdMobPackage(string packageName)
        {
            return !string.IsNullOrEmpty(packageName) &&
                   packageName.IndexOf("GoogleMobileAds", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAdMobInstalled()
        {
            foreach (var name in CompilationPipeline.GetPrecompiledAssemblyNames())
            {
                if (name == "GoogleMobileAds") return true;
            }

            foreach (var asm in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                if (asm != null && asm.name == "GoogleMobileAds") return true;
            }

            var guids = AssetDatabase.FindAssets("GoogleMobileAds");
            foreach (var guid in guids)
            {
                if (Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)) == "GoogleMobileAds.dll")
                    return true;
            }

            try
            {
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info != null && info.name == "com.google.ads.mobile") return true;
                }
            }
            catch (Exception e)
            {
                DebugUtils.Warning($"Package Manager: {e.Message}");
            }

            return false;
        }

        private static string FindPackageFile()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AdMobPackageInstaller).Assembly);
            if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
            {
                string packaged = Path.Combine(info.resolvedPath, "ThirdParty", PackageFileName);
                if (File.Exists(packaged)) return packaged;
            }

            var guids = AssetDatabase.FindAssets("AdMobPackageInstaller t:Script");
            if (guids.Length == 0) return null;

            string scriptDir = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
            string root = Path.GetDirectoryName(scriptDir);
            if (string.IsNullOrEmpty(root)) return null;

            string relative = (root + "/ThirdParty/" + PackageFileName).Replace('\\', '/');
            string full = Path.GetFullPath(relative);
            return File.Exists(full) ? full : null;
        }
    }
}
