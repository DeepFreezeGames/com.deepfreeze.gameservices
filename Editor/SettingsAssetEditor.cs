using System;
using UnityEditor;
using UnityEngine;

namespace GameServices.Editor
{
    [CustomEditor(typeof(GameServiceSettingsAsset))]
    public class SettingsAssetEditor : UnityEditor.Editor
    {
        private GameServiceSettingsAsset _asset;
        
        private void OnEnable()
        {
            _asset = target as GameServiceSettingsAsset;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
           
            _asset.settings.autoInitializeServices = EditorGUILayout.Toggle("Auto-Initialize Services", _asset.settings.autoInitializeServices);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            _asset.settings.logMessages = EditorGUILayout.Toggle("Log Messages", _asset.settings.logMessages);
            _asset.settings.logWarnings = EditorGUILayout.Toggle("Log Warnings", _asset.settings.logWarnings);
            _asset.settings.logErrors = EditorGUILayout.Toggle("Log Errors", _asset.settings.logErrors);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        [SettingsProvider]
        private static SettingsProvider CreateGameSettingsProvider()
        {
            var provider = new AssetSettingsProvider("Project/Game Services", () => GameServiceSettingsAsset.Asset);
            provider.PopulateSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }

        private class Styles
        {
            
        }
    }
}