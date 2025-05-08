using System.Collections.Generic;
using System.Reflection;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Models.Helpers
{

    /// <summary>Provides access to the default ASM scenes.</summary>
    public class DefaultScenes : ScriptableObject
    {

        #region Setup

#if UNITY_EDITOR && ASM_DEV

        void OnEnable() =>
            SceneManager.OnInitialized(FindScenes);

        void FindScenes()
        {

            var needsSave = false;
            foreach (var (folder, sceneField, assetField) in names)
                if (Find(folder, out var scene, out var asset))
                {
                    if (Set(scene, asset, sceneField, assetField))
                        needsSave = true;
                }
                else
                    Debug.LogError($"Could not find '{folder}' ASM scene.");

            if (needsSave)
                SceneManager.settings.project.m_defaultScenes.Save();

        }

        bool Find(string name, out Scene scene, out SceneAsset asset)
        {

            var folder = $"{SceneManager.package.folder}/Default scenes/{name}";
            scene = AssetDatabase.LoadAssetAtPath<Scene>($"{folder}/{name}.asset");
            asset = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{folder}/{name}.unity");

            return scene && asset;

        }

        bool Set(Scene scene, SceneAsset asset, string sceneField, string assetField)
        {

            var needsSaving = false;

            var path = AssetDatabase.GetAssetPath(asset);
            if (scene.path != path)
            {
                scene.path = path;
                EditorApplication.delayCall += scene.Save;
            }

            var t = GetType();

            var field = t.GetField(sceneField, BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(this) as Scene != scene)
            {
                field.SetValue(this, scene);
                needsSaving = true;
            }

            field = t.GetField(assetField, BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(this) as SceneAsset != asset)
            {
                field.SetValue(this, asset);
                needsSaving = true;
            }

            return needsSaving;

        }

        static readonly (string folder, string scene, string asset)[] names =
        {
            ("Fade", nameof(m_fadeScene), nameof(m_fadeAsset)),
            ("IconBounce", nameof(m_iconBounceScene), nameof(m_iconBounceAsset)),
            ("PressAnyKey", nameof(m_pressAnyKeyScene), nameof(m_pressAnyKeyAsset)),
            ("ProgressBar", nameof(m_progressBarScene), nameof(m_progressBarAsset)),
            ("Quote", nameof(m_quoteScene), nameof(m_quoteAsset)),
            ("Pause", nameof(m_pauseScene), nameof(m_pauseAsset)),
            ("InGameToolbar", nameof(m_inGameToolbarScene), nameof(m_inGameToolbarAsset)),
            ("Splash Fade", nameof(m_splashFadeScene), nameof(m_splashFadeAsset)),
            ("Splash ASM", nameof(m_splashASMScene), nameof(m_splashASMAsset)),
            ("TotalProgressBar", nameof(m_totalProgressBarScene), nameof(m_totalProgressBarAsset)),
        };

#endif

        #endregion

        [Header("Scenes")]
        [SerializeField] private Scene m_splashASMScene;
        [SerializeField] private Scene m_splashFadeScene;
        [SerializeField] private Scene m_fadeScene;
        [SerializeField] private Scene m_progressBarScene;
        [SerializeField] private Scene m_totalProgressBarScene;
        [SerializeField] private Scene m_iconBounceScene;
        [SerializeField] private Scene m_pressAnyKeyScene;
        [SerializeField] private Scene m_quoteScene;
        [SerializeField] private Scene m_pauseScene;
        [SerializeField] private Scene m_inGameToolbarScene;

        [Header("Assets")]
        [SerializeField] private Object m_splashASMAsset;
        [SerializeField] private Object m_splashFadeAsset;
        [SerializeField] private Object m_fadeAsset;
        [SerializeField] private Object m_progressBarAsset;
        [SerializeField] private Object m_totalProgressBarAsset;
        [SerializeField] private Object m_iconBounceAsset;
        [SerializeField] private Object m_pressAnyKeyAsset;
        [SerializeField] private Object m_quoteAsset;
        [SerializeField] private Object m_pauseAsset;
        [SerializeField] private Object m_inGameToolbarAsset;

        /// <summary>Gets the default ASM splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene splashASMScene => m_splashASMScene;

        /// <summary>Gets the default fade splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene splashFadeScene => m_splashFadeScene;

        /// <summary>Gets the default fade loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene fadeScene => m_fadeScene;

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene progressBarScene => m_progressBarScene;

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene totalProgressBarScene => m_totalProgressBarScene;

        /// <summary>Gets the default icon bounce loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene iconBounceScene => m_iconBounceScene;

        /// <summary>Gets the default press any button loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene pressAnyKeyScene => m_pressAnyKeyScene;

        /// <summary>Gets the default quote loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene quoteScene => m_quoteScene;

        /// <summary>Gets the default pause scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene pauseScene => m_pauseScene;

        /// <summary>Gets the default in-game-toolbar scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene inGameToolbarScene => m_inGameToolbarScene;

        /// <summary>Enumerates all default scenes.</summary>
        /// <param name="listNulls">Specifies whatever <see langword="null"/> will be returned for scenes that could not be found.</param>
        public IEnumerable<Scene> Enumerate(bool listNulls = false)
        {
            IEnumerable<Scene> list = new[] { splashASMScene, splashFadeScene, fadeScene, progressBarScene, totalProgressBarScene, iconBounceScene, pressAnyKeyScene, quoteScene, pauseScene, inGameToolbarScene };
            if (!listNulls)
                list = list.NonNull();
            return list;
        }

    }

}
