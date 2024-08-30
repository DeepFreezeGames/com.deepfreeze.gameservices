using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GameServices.Editor
{
    public sealed class ServiceEditorWindow : EditorWindow
    {
        private const BindingFlags MethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        private static List<FoundService> _services;
        internal static List<FoundService> Services
        {
            get
            {
                if (_services == null)
                {
                    FetchServices();
                }

                return _services;
            }
        }

        private bool _viewingSettings;
        private Vector2 _scrollPosInspector;
        private UnityEditor.Editor _settingsEditor;

        private GUIStyle _indicatorStyle;

        private bool _boundEvents;

        [MenuItem("Window/General/Game Services")]
        public static void Initialize()
        {
            var window = GetWindow<ServiceEditorWindow>("Game Services");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            _boundEvents = true;
        }

        private static void FetchServices()
        {
            _services = new List<FoundService>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && typeof(IGameService).IsAssignableFrom(type))
                    {
                        _services.Add(new FoundService((IGameService)Activator.CreateInstance(type)));
                    }
                    
                    foreach (var method in type.GetMethods(MethodFlags))
                    {
                        if (Attribute.GetCustomAttribute(method, typeof(RegisterServiceAttribute)) is RegisterServiceAttribute attribute)
                        {
                            _services.Add(new FoundService(type, attribute.SortOrder, method.ReturnType == typeof(Task)));
                        }
                    }
                }
            }

            _services = _services.OrderBy(s => s.SortOrder).ToList();
        }

        private void OnUpdate()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void OnDisable()
        {
            if (_boundEvents)
            {
                EditorApplication.update -= OnUpdate;

                _boundEvents = false;
            }
        }

        private void OnGUI()
        {
            _indicatorStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                normal =
                {
                    background = Texture2D.whiteTexture
                }
            };
            
            DrawToolbar();
         
            if (Services == null || Services.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("No services found", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            if (_viewingSettings)
            {
                DrawSettings();
            }
            else
            {
                DrawServiceList();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                GUILayout.FlexibleSpace();

                GUI.color = _viewingSettings ? Color.grey : Color.white;
                if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    GUI.FocusControl(null);
                    _scrollPosInspector = Vector2.zero;
                    _viewingSettings = !_viewingSettings;
                }
                GUI.color = Color.white;
                
                if (GUILayout.Button("|", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Refresh"), false, FetchServices);
                    
                    menu.ShowAsContext();
                }
            }
        }

        private void DrawServiceList()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                _scrollPosInspector = EditorGUILayout.BeginScrollView(_scrollPosInspector);
                {
                    foreach (var service in Services)
                    {
                        var enabled = !GameServiceSettingsAsset.Settings.disabledServices.Contains(service.Type.FullName);
                        GUI.color = enabled ? Color.white : Color.gray;
                        GUI.contentColor = enabled ? Color.white : Color.gray;
                        using (new EditorGUILayout.HorizontalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false)))
                        {
                            GUI.color = GetServiceIndicatorColor(service.Type);
                            using (new EditorGUILayout.VerticalScope(_indicatorStyle, GUILayout.Width(2), GUILayout.ExpandHeight(true))) { }
                            GUI.color = Color.white;
                            
                            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false)))
                            {
                                using(new EditorGUILayout.HorizontalScope())
                                {
                                    GUILayout.Label($"{service.SortOrder}\t{service.Type.Name}", EditorStyles.boldLabel);
                                    var oldColor = GUI.contentColor;
                                    GUI.contentColor = Color.white;
                                    if(GUILayout.Button(enabled ? "Disable" : "Enable", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                    {
                                        if(enabled)
                                        {
                                            GameServiceSettingsAsset.Settings.disabledServices.Add(service.Type.FullName);
                                        }   
                                        else
                                        {
                                            GameServiceSettingsAsset.Settings.disabledServices.Remove(service.Type.FullName);
                                        }
                                    }
                                    GUI.contentColor = oldColor;
                                }
                                EditorGUILayout.LabelField("\tFull Class Name", service.Type.FullName);
                                EditorGUILayout.LabelField("\tService Type", service.StaticService ? "Static": "Instanced");
                                EditorGUILayout.LabelField("\tAsync", service.IsAsync.ToString());
                            }
                        }
                        GUI.color = Color.white;
                        GUI.contentColor = Color.white;
                    }
                }
                EditorGUILayout.EndScrollView();
                
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawSettings()
        {
            if (_settingsEditor == null)
            {
                _settingsEditor = UnityEditor.Editor.CreateEditor(GameServiceSettingsAsset.Asset);
            }
            
            using (new EditorGUILayout.VerticalScope())
            {
                _scrollPosInspector = EditorGUILayout.BeginScrollView(_scrollPosInspector);
                {
                    _settingsEditor.serializedObject.Update();
                    _settingsEditor.OnInspectorGUI();
                    _settingsEditor.serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private Color GetServiceIndicatorColor(Type serviceType)
        {
            if (!Application.isPlaying)
            {
                return Color.grey;
            }

            return ServiceLoader.GetServiceState(serviceType) switch
            {
                ServiceState.Inactive => Color.grey,
                ServiceState.Starting => Color.yellow,
                ServiceState.Running => Color.green,
                ServiceState.Stopping => new Color(1, 0.5f, 0, 1),
                ServiceState.Error => Color.red,
                _ => Color.grey
            };
        }
    }
}