using UnityEngine;

namespace GameServices
{
    public sealed class GameServiceSettingsAsset : ScriptableObject
    {
        private const string FileName = "GameServicesSettings";
        
        private static GameServiceSettingsAsset _asset;
        internal static GameServiceSettingsAsset Asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = Resources.Load<GameServiceSettingsAsset>(FileName);
                    if (_asset == null)
                    {
                        Debug.LogWarning("Game Services settings file does not exist. Using fallback...");
                        _asset = CreateInstance<GameServiceSettingsAsset>();
                        
                        #if UNITY_EDITOR
                        if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                        {
                            UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                        }
                        UnityEditor.AssetDatabase.CreateAsset(_asset, $"Assets/Resources/{FileName}.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                        #endif
                    }
                }

                return _asset;
            }
        }

        [SerializeField] private Settings settings = new();
        internal static Settings Settings => Asset.settings;
    }
}