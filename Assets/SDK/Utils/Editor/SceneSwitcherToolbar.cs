using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LSDK.Utils.EditorTools
{
    /// <summary>
    /// Scene dropdown on the left of the main toolbar, just after the built-in items.
    /// Unity reserves dock indices through 100 for built-in elements.
    /// ElementPath is the toolbar id. Keep it stable so saved layouts still find this control.
    /// </summary>
    [InitializeOnLoad]
    static class SceneSwitcherToolbar
    {
        private const string ElementPath = "LSDK/Scene Switcher";
        private const int DockIndex = 101;
        private const string AssetsPrefix = "Assets/";
        private const string PackagesPrefix = "Packages/";

        static SceneSwitcherToolbar()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = DockIndex)]
        private static MainToolbarElement CreateSceneSwitcher()
        {
            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            var content = new MainToolbarContent(GetActiveSceneName(), icon, "Switch scene");
            return new MainToolbarDropdown(content, ShowSceneMenu);
        }

        private static void ShowSceneMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            var scenes = CollectScenes();
            if (scenes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes"));
                menu.DropDown(dropDownRect);
                return;
            }

            var activePath = GetActiveScenePath();
            var duplicateBuildNames = FindDuplicateBuildNames(scenes);
            var hasBuildScene = false;
            var insertedSeparator = false;

            for (var i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                if (scene.IsInBuild)
                    hasBuildScene = true;

                if (hasBuildScene && !scene.IsInBuild && !insertedSeparator)
                {
                    menu.AddSeparator(string.Empty);
                    insertedSeparator = true;
                }

                var label = GetMenuLabel(scene, duplicateBuildNames);
                var isActive = scene.Path == activePath;
                if (Application.isPlaying && !scene.IsInBuild)
                {
                    menu.AddDisabledItem(new GUIContent(label));
                    continue;
                }

                var scenePath = scene.Path;
                menu.AddItem(new GUIContent(label), isActive, () => OpenScene(scenePath));
            }

            menu.DropDown(dropDownRect);
        }

        private static void OpenScene(string scenePath)
        {
            if (scenePath == GetActiveScenePath())
                return;

            if (Application.isPlaying)
            {
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                    return;

                SceneManager.LoadScene(sceneName);
                return;
            }

            if (!File.Exists(scenePath))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath);
        }

        private static List<SceneChoice> CollectScenes()
        {
            var scenes = new List<SceneChoice>();
            var seenPaths = new HashSet<string>();
            var buildScenes = EditorBuildSettings.scenes;
            for (var i = 0; i < buildScenes.Length; i++)
            {
                var buildScene = buildScenes[i];
                if (!buildScene.enabled || !IsLoadableScene(buildScene.path) || !seenPaths.Add(buildScene.path))
                    continue;

                scenes.Add(new SceneChoice(buildScene.path, true));
            }

            var extraPaths = new List<string>();
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (var i = 0; i < sceneGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                if (!IsAssetScene(path) || seenPaths.Contains(path))
                    continue;

                extraPaths.Add(path);
            }

            extraPaths.Sort(StringComparer.Ordinal);
            for (var i = 0; i < extraPaths.Count; i++)
                scenes.Add(new SceneChoice(extraPaths[i], false));

            return scenes;
        }

        private static HashSet<string> FindDuplicateBuildNames(List<SceneChoice> scenes)
        {
            var seen = new HashSet<string>();
            var duplicates = new HashSet<string>();
            for (var i = 0; i < scenes.Count; i++)
            {
                if (!scenes[i].IsInBuild)
                    continue;

                var sceneName = Path.GetFileNameWithoutExtension(scenes[i].Path);
                if (!seen.Add(sceneName))
                    duplicates.Add(sceneName);
            }

            return duplicates;
        }

        private static string GetMenuLabel(SceneChoice scene, HashSet<string> duplicateBuildNames)
        {
            var sceneName = Path.GetFileNameWithoutExtension(scene.Path);
            if (scene.IsInBuild && !duplicateBuildNames.Contains(sceneName))
                return sceneName;

            return ToMenuPath(scene.Path);
        }

        private static string ToMenuPath(string scenePath)
        {
            var relativePath = scenePath;
            if (relativePath.StartsWith(AssetsPrefix, StringComparison.Ordinal))
                relativePath = relativePath.Substring(AssetsPrefix.Length);

            return Path.ChangeExtension(relativePath, null).Replace('\\', '/');
        }

        private static bool IsLoadableScene(string scenePath)
        {
            return HasSceneExtension(scenePath)
                && (scenePath.StartsWith(AssetsPrefix, StringComparison.Ordinal)
                    || scenePath.StartsWith(PackagesPrefix, StringComparison.Ordinal));
        }

        private static bool IsAssetScene(string scenePath)
        {
            return HasSceneExtension(scenePath)
                && scenePath.StartsWith(AssetsPrefix, StringComparison.Ordinal);
        }

        private static bool HasSceneExtension(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath)
                && scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetActiveSceneName()
        {
            var sceneName = GetActiveScene().name;
            return string.IsNullOrEmpty(sceneName) ? "Untitled" : sceneName;
        }

        private static string GetActiveScenePath()
        {
            return GetActiveScene().path;
        }

        private static Scene GetActiveScene()
        {
            return Application.isPlaying
                ? SceneManager.GetActiveScene()
                : EditorSceneManager.GetActiveScene();
        }

        private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            MainToolbar.Refresh(ElementPath);
        }

        private static void OnSceneSaved(Scene scene)
        {
            MainToolbar.Refresh(ElementPath);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            MainToolbar.Refresh(ElementPath);
        }

        private readonly struct SceneChoice
        {
            public SceneChoice(string path, bool isInBuild)
            {
                Path = path;
                IsInBuild = isInBuild;
            }

            public string Path { get; }
            public bool IsInBuild { get; }
        }
    }
}
