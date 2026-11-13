using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace LSDK.Mediation.EditorTools
{
    public static class MediationConfigMenu
    {
        private const string Dir = "Assets/Resources/LSDK/Mediation/SO";
        private const string AssetPath = Dir + "/MediationConfig.asset";

        private static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS
        };

        [MenuItem("Tools/LSDK/Mediation/Create Config")]
        private static void CreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MediationConfig>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Directory.CreateDirectory(Dir);
            var config = ScriptableObject.CreateInstance<MediationConfig>();
            config.ResetToDefaults();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Tools/LSDK/Mediation/Define Symbols")]
        public static void DefineSymbols()
        {
            var assemblies = CollectAssemblies();
            var packages = CollectPackages();
            var networks = FindNetworkAsmdefs();

            var added = new List<string>();
            var already = new List<string>();
            var missing = new List<string>();

            foreach (var network in networks)
            {
                string symbols = string.Join(", ", network.Symbols);
                if (!SdkPresent(network, assemblies, packages))
                {
                    missing.Add($"{network.Label} ({symbols})");
                    continue;
                }

                bool anyAdded = false;
                foreach (var symbol in network.Symbols)
                {
                    if (EnsureSymbol(symbol)) anyAdded = true;
                }

                if (anyAdded) added.Add($"{network.Label}: {symbols}");
                else already.Add($"{network.Label}: {symbols}");
            }

            string report = BuildReport(added, already, missing);
            if (added.Count > 0)
                DebugUtils.Success("Define Symbols — đã thêm: " + string.Join("; ", added));
            else if (already.Count > 0)
                DebugUtils.Success("Define Symbols — đã có sẵn: " + string.Join("; ", already));
            else
                DebugUtils.Warning("Define Symbols — chưa thấy network SDK nào.");

            EditorUtility.DisplayDialog("LSDK Mediation", report, "OK");
        }

        private static List<NetworkAsmdef> FindNetworkAsmdefs()
        {
            var result = new List<NetworkAsmdef>();
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AsmdefFile file;
                try
                {
                    file = JsonUtility.FromJson<AsmdefFile>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    DebugUtils.Warning($"Không đọc được {path}: {e.Message}");
                    continue;
                }

                if (file == null) continue;
                var symbols = SymbolsOf(file);
                if (symbols.Count == 0) continue;
                if (!ReferencesMediation(file)) continue;

                result.Add(new NetworkAsmdef
                {
                    Label = NetworkLabel(file.name),
                    Symbols = symbols,
                    References = ExternalReferences(file),
                    Packages = PackageNames(file)
                });
            }

            result.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));
            return result;
        }

        private static bool SdkPresent(NetworkAsmdef network, HashSet<string> assemblies, HashSet<string> packages)
        {
            foreach (var package in network.Packages)
            {
                if (packages.Contains(package)) return true;
            }

            foreach (var reference in network.References)
            {
                if (IsAssemblyPresent(reference, assemblies)) return true;
            }

            return false;
        }

        private static bool EnsureSymbol(string symbol)
        {
            bool added = false;
            foreach (var target in DefineTargets())
            {
                if (AddSymbol(target, symbol)) added = true;
            }
            return added;
        }

        private static IEnumerable<NamedBuildTarget> DefineTargets()
        {
            var seen = new HashSet<string>();
            foreach (var target in Targets)
            {
                if (seen.Add(target.TargetName)) yield return target;
            }

            var activeGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var active = NamedBuildTarget.FromBuildTargetGroup(activeGroup);
            if (active != NamedBuildTarget.Unknown && seen.Add(active.TargetName))
                yield return active;
        }

        private static bool AddSymbol(NamedBuildTarget target, string symbol)
        {
            var current = PlayerSettings.GetScriptingDefineSymbols(target);
            var list = new List<string>();
            if (!string.IsNullOrEmpty(current))
            {
                var parts = current.Split(';');
                foreach (var part in parts)
                {
                    var item = part.Trim();
                    if (item.Length > 0 && !list.Contains(item)) list.Add(item);
                }
            }

            if (list.Contains(symbol)) return false;
            list.Add(symbol);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
            return true;
        }

        private static HashSet<string> CollectAssemblies()
        {
            var names = new HashSet<string>();
            foreach (var name in CompilationPipeline.GetPrecompiledAssemblyNames())
            {
                if (!string.IsNullOrEmpty(name)) names.Add(name);
            }

            AddCompiledAssemblies(names, AssembliesType.Player);
            AddCompiledAssemblies(names, AssembliesType.Editor);
            return names;
        }

        private static void AddCompiledAssemblies(HashSet<string> names, AssembliesType type)
        {
            foreach (var asm in CompilationPipeline.GetAssemblies(type))
            {
                if (asm != null && !string.IsNullOrEmpty(asm.name)) names.Add(asm.name);
            }
        }

        private static HashSet<string> CollectPackages()
        {
            var names = new HashSet<string>();
            try
            {
                foreach (var info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                {
                    if (info != null && !string.IsNullOrEmpty(info.name)) names.Add(info.name);
                }
            }
            catch (Exception e)
            {
                DebugUtils.Warning($"Package Manager: {e.Message}");
            }
            return names;
        }

        private static bool IsAssemblyPresent(string assemblyName, HashSet<string> known)
        {
            if (known.Contains(assemblyName)) return true;

            var guids = AssetDatabase.FindAssets(assemblyName);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != assemblyName) continue;
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool ReferencesMediation(AsmdefFile file)
        {
            if (file.references == null) return false;
            foreach (var reference in file.references)
            {
                if (reference == "LSDK.Mediation") return true;
            }
            return false;
        }

        private static List<string> ExternalReferences(AsmdefFile file)
        {
            var list = new List<string>();
            if (file.references == null) return list;
            foreach (var reference in file.references)
            {
                if (!IsExternalReference(reference) || list.Contains(reference)) continue;
                list.Add(reference);
            }
            return list;
        }

        private static bool IsExternalReference(string reference)
        {
            if (string.IsNullOrEmpty(reference) || reference.StartsWith("GUID:")) return false;
            if (reference == "LSDK.Mediation" || reference.StartsWith("LSDK.Mediation.")) return false;
            if (reference == "UnityEngine" || reference.StartsWith("UnityEngine.")) return false;
            if (reference == "UnityEditor" || reference.StartsWith("UnityEditor.")) return false;
            return true;
        }

        private static List<string> PackageNames(AsmdefFile file)
        {
            var list = new List<string>();
            if (file.versionDefines == null) return list;
            foreach (var entry in file.versionDefines)
            {
                if (entry == null || string.IsNullOrEmpty(entry.name) || list.Contains(entry.name)) continue;
                list.Add(entry.name);
            }
            return list;
        }

        private static List<string> SymbolsOf(AsmdefFile file)
        {
            var list = new List<string>();
            AddSymbols(list, file.defineConstraints);
            if (file.versionDefines != null)
            {
                foreach (var entry in file.versionDefines)
                {
                    if (entry != null) AddSymbolName(list, entry.define);
                }
            }
            return list;
        }

        private static void AddSymbols(List<string> list, string[] symbols)
        {
            if (symbols == null) return;
            foreach (var symbol in symbols) AddSymbolName(list, symbol);
        }

        private static void AddSymbolName(List<string> list, string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || symbol[0] == '!') return;
            if (!list.Contains(symbol)) list.Add(symbol);
        }

        private static string NetworkLabel(string asmdefName)
        {
            const string prefix = "LSDK.Mediation.";
            if (!string.IsNullOrEmpty(asmdefName) && asmdefName.StartsWith(prefix))
                return asmdefName.Substring(prefix.Length);
            return string.IsNullOrEmpty(asmdefName) ? "Network" : asmdefName;
        }

        private static string BuildReport(List<string> added, List<string> already, List<string> missing)
        {
            var sb = new StringBuilder();
            AppendSection(sb, "Đã thêm", added);
            AppendSection(sb, "Đã có sẵn", already);
            AppendSection(sb, "Chưa thấy SDK", missing);
            if (sb.Length == 0)
                sb.Append("Không có network adapter nào khai báo scripting define.");
            return sb.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder sb, string title, List<string> items)
        {
            if (items.Count == 0) return;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(title).Append(":\n");
            foreach (var item in items) sb.Append("• ").Append(item).Append('\n');
        }

        private sealed class NetworkAsmdef
        {
            public string Label;
            public List<string> Symbols;
            public List<string> References;
            public List<string> Packages;
        }

        [Serializable]
        private sealed class AsmdefFile
        {
            public string name;
            public string[] references;
            public string[] defineConstraints;
            public PackageDefine[] versionDefines;
        }

        [Serializable]
        private sealed class PackageDefine
        {
            public string name;
            public string define;
        }
    }
}
