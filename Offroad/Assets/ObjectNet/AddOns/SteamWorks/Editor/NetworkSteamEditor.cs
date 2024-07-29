#if STEAMWORKS_NET
using Steamworks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#else
using UnityEngine;
#endif
#if UNITY_EDITOR
using Unity.VisualScripting;
using UnityEditor;
#endif

namespace com.onlineobject.objectnet.editor {
#if UNITY_EDITOR
    /// <summary>
    /// Class NetworkSteamEditor.
    /// Implements the <see cref="Editor" />
    /// </summary>
    /// <seealso cref="Editor" />
    [CustomEditor(typeof(NetworkSteamManager))]
    [CanEditMultipleObjects]
    public class NetworkSteamManagerEditor : Editor {
        /// <summary>
        /// The network steam manager
        /// </summary>
        NetworkSteamManager networkSteamManager;

        SerializedProperty dontDestroyOnLoad;
        
        SerializedProperty lobbyType;

        SerializedProperty lobbyDistance;

        SerializedProperty autoRefresh;
        
        SerializedProperty refreshRate;

        SerializedProperty maximumOfPlayers;

        /// <summary>
        /// The detail background opacity
        /// </summary>
        const float DETAIL_BACKGROUND_OPACITY = 0.05f;

        /// <summary>
        /// The transport background alpha
        /// </summary>
        const float TRANSPORT_BACKGROUND_ALPHA = 0.25f;

        /// <summary>
        /// Called when [enable].
        /// </summary>
        public void OnEnable() {
            this.networkSteamManager = (this.target as NetworkSteamManager);
            // Get all serializable objects
            this.dontDestroyOnLoad  = serializedObject.FindProperty("dontDestroyOnLoad");
            this.lobbyType          = serializedObject.FindProperty("lobbyType");
            this.lobbyDistance      = serializedObject.FindProperty("lobbyDistance");
            this.autoRefresh        = serializedObject.FindProperty("autoRefresh");
            this.refreshRate        = serializedObject.FindProperty("refreshRate");
            this.maximumOfPlayers   = serializedObject.FindProperty("maximumOfPlayers");
        }

        /// <summary>
        /// Implement this function to make a custom inspector.
        /// </summary>
        public override void OnInspectorGUI() {
#if STEAMWORKS_NET
            serializedObject.Update();
            EditorUtils.PrintImage("objectnet_steam_logo", Color.blue, 0, 25);

            GUILayout.Space(5.0f);

            if (Application.isPlaying) {
                EditorUtils.PrintImageButton("Changes are disabled during PlayMode", "oo_info", Color.red.WithAlpha(0.15f), EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                });
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorUtils.PrintImageButton("Documentation", "oo_document", EditorUtils.IMAGE_BUTTON_COLOR, EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                Help.BrowseURL("https://onlineobject.net/objectnet/docs/manual/ObjectNet.html");
            });
            EditorUtils.PrintImageButton("Tutorials", "oo_youtube", EditorUtils.IMAGE_BUTTON_COLOR, EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                Help.BrowseURL("https://www.youtube.com/@TheObjectNet");
            });
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            EditorUtils.PrintHeader("Steam Manager", Color.blue, Color.white, 16, "oo_steam", true, () => {
                if (this.dontDestroyOnLoad != null) {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(3.0f);
                    EditorGUILayout.BeginHorizontal();
                    EditorUtils.PrintSimpleExplanation("Don't destroy");
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(3.0f);
                    EditorUtils.PrintBooleanSquaredByRef(ref this.dontDestroyOnLoad, "", null, 14, 12, false);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            });

            // Lobby type
            EditorGUILayout.BeginHorizontal();
            int selectedLobbyType = (int)this.DrawLobbyType((ELobbyType)this.lobbyType.intValue);
            if (selectedLobbyType != this.lobbyType.intValue) {
                this.lobbyType.intValue = selectedLobbyType;
            }
            EditorGUILayout.EndHorizontal();

            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));

            // Lobby distance
            EditorGUILayout.BeginHorizontal();
            int selectedDistance = (int)this.DrawLobbyDistance((ELobbyDistanceFilter)this.lobbyDistance.intValue);
            if (selectedDistance != this.lobbyDistance.intValue) {
                this.lobbyDistance.intValue = selectedDistance;
            }
            EditorGUILayout.EndHorizontal();

            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5.0f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5.0f);
            int previousPlayers = this.maximumOfPlayers.intValue;
            int newPlayers = previousPlayers;
            GUILayout.FlexibleSpace();
            EditorUtils.DrawHorizontalIntBar(ref newPlayers, 2, 101, "Maximum of players", 500, true, " unlimited ");
            if (previousPlayers != newPlayers) {
                this.maximumOfPlayers.intValue = newPlayers;
            }
            GUILayout.Space(5.0f);
            EditorGUILayout.BeginHorizontal();
            EditorUtils.PrintExplanationLabel("The maximum of players accepted on each lobby", "oo_info", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2.0f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10.0f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10.0f);
            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));

            GUILayout.Space(5.0f);
            EditorUtils.PrintBooleanSquaredByRef(ref this.autoRefresh, "Auto Refresh Lobby", "", 16, 12);
            GUILayout.Space(2.0f);
            EditorUtils.PrintExplanationLabel("This option will refresh the lobby list according to the time defined on the refresh rate parameter", "oo_refresh", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(5.0f);
            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            GUILayout.Space(5.0f);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5.0f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5.0f);
            int previousRate = this.refreshRate.intValue;
            int newRate = previousRate;
            GUILayout.FlexibleSpace();
            EditorUtils.DrawHorizontalIntBar(ref newRate, 100, 5000, "Refresh Rate", 500);
            if (previousRate != newRate) {
                this.refreshRate.intValue = newRate;
            }
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("Time in milliseconds that manager will try to refresh the lobby list", "oo_info", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(2.0f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10.0f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20.0f);
            serializedObject.ApplyModifiedProperties();
#else
            serializedObject.Update();
            EditorUtils.PrintImage("objectnet_steam_logo", Color.blue, 0, 25);

            GUILayout.Space(5.0f);

            if (Application.isPlaying) {
                EditorUtils.PrintImageButton("Changes are disabled during PlayMode", "oo_info", Color.red.WithAlpha(0.15f), EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                });
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorUtils.PrintImageButton("Documentation", "oo_document", EditorUtils.IMAGE_BUTTON_COLOR, EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                Help.BrowseURL("https://onlineobject.net/objectnet/docs/manual/ObjectNet.html");
            });
            EditorUtils.PrintImageButton("Tutorials", "oo_youtube", EditorUtils.IMAGE_BUTTON_COLOR, EditorUtils.IMAGE_BUTTON_FONT_COLOR, () => {
                Help.BrowseURL("https://www.youtube.com/@TheObjectNet");
            });
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10.0f);

            EditorGUILayout.BeginHorizontal(BackgroundStyle.Get(Color.red.WithAlpha(0.10f)));
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10.0f);
            EditorUtils.PrintExplanationLabel("Some dependencies are missing, you need to install SteamWorks packaged to use Steam integration", "oo_info");
            GUILayout.Space(10.0f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
#endif
        }

#if STEAMWORKS_NET
        /// <summary>
        /// Draws lobby types.
        /// </summary>
        /// <param name="selectedIndex">Index of the selected.</param>
        /// <returns>ELobbyType.</returns>
        private ELobbyType DrawLobbyType(ELobbyType selectedIndex) {
            ELobbyType result = ELobbyType.k_ELobbyTypeFriendsOnly;
            EditorGUILayout.BeginHorizontal(BackgroundStyle.Get(Color.red.WithAlpha(DETAIL_BACKGROUND_OPACITY)));
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5.0f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);

            EditorUtils.PrintSimpleExplanation("Lobby Access Level");

            GUILayout.Space(10.0f);

            int selectedLobbyTypeIndex = (int)selectedIndex;
            List<string> lobbyTypesName = new List<string>();
            lobbyTypesName.Add(ELobbyType.k_ELobbyTypePrivate.ToString().Replace("k_ELobbyType", ""));
            lobbyTypesName.Add(ELobbyType.k_ELobbyTypeFriendsOnly.ToString().Replace("k_ELobbyType", ""));
            lobbyTypesName.Add(ELobbyType.k_ELobbyTypePublic.ToString().Replace("k_ELobbyType", ""));
            lobbyTypesName.Add(ELobbyType.k_ELobbyTypeInvisible.ToString().Replace("k_ELobbyType", ""));
            lobbyTypesName.Add(ELobbyType.k_ELobbyTypePrivateUnique.ToString().Replace("k_ELobbyType", ""));

            int selectedMethod = EditorGUILayout.Popup(selectedLobbyTypeIndex, lobbyTypesName.ToArray<string>(), GUILayout.Width(200));
            if (selectedMethod == (int)ELobbyType.k_ELobbyTypePrivate) {
                result = ELobbyType.k_ELobbyTypePrivate;
            } else if (selectedMethod == (int)ELobbyType.k_ELobbyTypeFriendsOnly) {
                result = ELobbyType.k_ELobbyTypeFriendsOnly;
            } else if (selectedMethod == (int)ELobbyType.k_ELobbyTypePublic) {
                result = ELobbyType.k_ELobbyTypePublic;
            } else if (selectedMethod == (int)ELobbyType.k_ELobbyTypeInvisible) {
                result = ELobbyType.k_ELobbyTypeInvisible;
            } else if (selectedMethod == (int)ELobbyType.k_ELobbyTypePrivateUnique) {
                result = ELobbyType.k_ELobbyTypePrivateUnique;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5.0f);
            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("Lobby Access Level when other players try to find your lobby to play on it", "oo_info", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(5.0f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5.0f);
            EditorGUILayout.EndHorizontal();
            
            return result;
        }

        /// <summary>
        /// Draws distances
        /// </summary>
        /// <param name="selectedIndex">Index of the selected.</param>
        /// <returns>ELobbyDistanceFilter.</returns>
        private ELobbyDistanceFilter DrawLobbyDistance(ELobbyDistanceFilter selectedIndex) {
            ELobbyDistanceFilter result = ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide;
            EditorGUILayout.BeginHorizontal(BackgroundStyle.Get(Color.red.WithAlpha(DETAIL_BACKGROUND_OPACITY)));
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5.0f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);

            EditorUtils.PrintSimpleExplanation("Lobby Distance");

            GUILayout.Space(10.0f);

            int selectedLobbyDistanceIndex = (int)selectedIndex;
            List<string> lobbyDistanceNames = new List<string>();
            
            lobbyDistanceNames.Add(ELobbyDistanceFilter.k_ELobbyDistanceFilterClose.ToString().Replace("k_ELobbyDistance", ""));
            lobbyDistanceNames.Add(ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault.ToString().Replace("k_ELobbyDistance", ""));
            lobbyDistanceNames.Add(ELobbyDistanceFilter.k_ELobbyDistanceFilterFar.ToString().Replace("k_ELobbyDistance", ""));
            lobbyDistanceNames.Add(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide.ToString().Replace("k_ELobbyDistance", ""));

            int selectedMethod = EditorGUILayout.Popup(selectedLobbyDistanceIndex, lobbyDistanceNames.ToArray<string>(), GUILayout.Width(200));
            if (selectedMethod == (int)ELobbyDistanceFilter.k_ELobbyDistanceFilterClose) {
                result = ELobbyDistanceFilter.k_ELobbyDistanceFilterClose;
            } else if (selectedMethod == (int)ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault) {
                result = ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;
            } else if (selectedMethod == (int)ELobbyDistanceFilter.k_ELobbyDistanceFilterFar) {
                result = ELobbyDistanceFilter.k_ELobbyDistanceFilterFar;
            } else if (selectedMethod == (int)ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide) {
                result = ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5.0f);
            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("Lobby Distance will filter the distance where you will find lobbies when a search is performed", "oo_network", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(5.0f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5.0f);
            EditorGUILayout.EndHorizontal();

            return result;
        }
#endif
    }
#endif
}