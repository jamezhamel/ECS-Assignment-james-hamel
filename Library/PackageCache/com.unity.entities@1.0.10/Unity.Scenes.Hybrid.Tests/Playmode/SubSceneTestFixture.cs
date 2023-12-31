using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using Unity.Entities.Build;
using UnityEditor.SceneManagement;
#if USING_PLATFORMS_PACKAGE
using Unity.Build;
using Unity.Build.Common;
#endif
#endif
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Scenes.Hybrid.Tests
{
    public abstract class SubSceneTestFixture
#if UNITY_EDITOR
        : IPrebuildSetup, IPostBuildCleanup
#endif
    {
        string m_PlayModeScenePath; // The scene/prefab to load in play mode tests
        string m_BuildScenePath; // The scene to include in the build in player build tests
        Hash128 m_PlayModeSceneGUID;
        Hash128 m_BuildSceneGUID;

        public string PlayModeScenePath
        {
            get { return m_PlayModeScenePath; }
            set { m_PlayModeScenePath = value; }
        }

        public string BuildScenePath
        {
            get { return m_BuildScenePath; }
            set { m_BuildScenePath = value; }
        }

        public Hash128 PlayModeSceneGUID => m_PlayModeSceneGUID;

        public Hash128 BuildSceneGUID
        {
            get { return m_BuildSceneGUID; }
            set { m_BuildSceneGUID = value; }
        }

#if UNITY_EDITOR
        GUID m_DotsSettingsGUID;
        string m_SceneWithBuildSettingsPath;
        static string m_TempPath = "Assets/Temp";
#endif

        public static World CreateEntityWorld(string name)
        {
            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default, true);
            var world = new World(name, WorldFlags.Game);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            return world;
        }

        public void SetUpOnce()
        {
#if UNITY_EDITOR
            try
            {
                m_DotsSettingsGUID = DotsGlobalSettings.Instance.GetClientGUID();
                m_PlayModeSceneGUID = new GUID(AssetDatabase.AssetPathToGUID(PlayModeScenePath));

                var guid = SceneWithBuildConfigurationGUIDs.EnsureExistsFor(m_PlayModeSceneGUID, m_DotsSettingsGUID, true,
                    out var requestRefresh);
                if (requestRefresh)
                    AssetDatabase.Refresh();
                m_SceneWithBuildSettingsPath = SceneWithBuildConfigurationGUIDs.GetSceneWithBuildSettingsPath(guid);
                EntityScenesPaths.GetSubSceneArtifactHash(m_PlayModeSceneGUID, m_DotsSettingsGUID, true,
                    ImportMode.Synchronous);
            }
            catch
            {
                AssetDatabase.DeleteAsset(m_TempPath);
                AssetDatabase.DeleteAsset(m_SceneWithBuildSettingsPath);
                throw;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public void TearDownOnce()
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(m_TempPath);
            AssetDatabase.DeleteAsset(m_SceneWithBuildSettingsPath);
#endif
        }

#if UNITY_EDITOR
        //IPrebuildSetup.Setup
        public void Setup()
        {
            if (string.IsNullOrEmpty(m_BuildScenePath))
                return;

            EntitiesCacheUtility.UpdateEntitySceneGlobalDependency();
            AssetDatabase.Refresh();
            var settingsScene = new EditorBuildSettingsScene(m_BuildScenePath, true);
            List<EditorBuildSettingsScene> newEditorSceneList = EditorBuildSettings.scenes.ToList();
            newEditorSceneList.Add(settingsScene);
            EditorBuildSettings.scenes =  newEditorSceneList.ToArray();
        }

        //IPostBuildCleanup.Cleanup
        public void Cleanup()
        {
            if (string.IsNullOrEmpty(m_BuildScenePath))
                return;

            List<EditorBuildSettingsScene> editorSceneList = EditorBuildSettings.scenes.ToList();
            editorSceneList = editorSceneList.Where(x => x.path != m_BuildScenePath).ToList();
            EditorBuildSettings.scenes =  editorSceneList.ToArray();

            if (!EditorApplication.isPlaying)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
#endif
    }
}
