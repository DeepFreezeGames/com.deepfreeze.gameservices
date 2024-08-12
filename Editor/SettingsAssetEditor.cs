using System;
using UnityEditor;

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
            base.OnInspectorGUI();
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