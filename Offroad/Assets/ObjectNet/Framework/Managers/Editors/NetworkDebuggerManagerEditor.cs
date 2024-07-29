using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.onlineobject.objectnet.editor {
#if UNITY_EDITOR
    /// <summary>
    /// Class NetworkDebuggerManagerEditor.
    /// Implements the <see cref="Editor" />
    /// </summary>
    /// <seealso cref="Editor" />
    [CustomEditor(typeof(NetworkDebuggerManager))]
    [CanEditMultipleObjects]
    public class NetworkDebuggerManagerEditor : Editor {

        /// <summary>
        /// The network manager debugger
        /// </summary>
        NetworkDebuggerManager networkManagerDebugger;

        /// <summary>
        /// The enable console log
        /// </summary>
        SerializedProperty EnableConsoleLog;

        /// <summary>
        /// Indicates whether console logging will appear on compiled vuild
        /// </summary>
        SerializedProperty EnableOnBuild;

        /// <summary>
        /// Indicated if need to capture warnings
        /// </summary>
        SerializedProperty CaptureWarnings;

        /// <summary>
        /// Indicated if need to capture errors
        /// </summary>
        SerializedProperty CaptureErrors;

        /// <summary>
        /// Indicated if need to capture logs
        /// </summary>
        SerializedProperty CaptureLogs;

        /// <summary>
        /// Indicated if need to disable gizmo
        /// </summary>
        SerializedProperty ShowGizmos;

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
            this.networkManagerDebugger = (this.target as NetworkDebuggerManager);
            // Get all serializable objects
            this.EnableConsoleLog   = serializedObject.FindProperty("EnableConsoleLog");
            this.EnableOnBuild      = serializedObject.FindProperty("EnableOnBuild");
            this.CaptureWarnings    = serializedObject.FindProperty("CaptureWarnings");
            this.CaptureErrors      = serializedObject.FindProperty("CaptureErrors");
            this.CaptureLogs        = serializedObject.FindProperty("CaptureLogs");
            this.ShowGizmos         = serializedObject.FindProperty("ShowGizmos");
        }

        /// <summary>
        /// Implement this function to make a custom inspector.
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorUtils.PrintImage("objectnet_logo", Color.blue, 0, 25);

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

            // Check if peer to peer is enabled
            EditorGUILayout.BeginHorizontal(BackgroundStyle.Get(Color.red.WithAlpha(DETAIL_BACKGROUND_OPACITY)));
            GUILayout.Space(15.0f);
            EditorGUILayout.BeginVertical();
            EditorUtils.PrintBooleanSquaredByRef(ref this.EnableConsoleLog, "Show debug trace", "oo_bug", 16, 12);
            
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("When enabled, this option will display internal engine logs on unity console", "oo_info");
            GUILayout.Space(5.0f);
            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            GUILayout.Space(5.0f);                       
            EditorUtils.PrintExplanationLabel("Disable this option on case of you need a clear console without internal engine diagnostic messages [ Log, Errors and Warning ]", "oo_note", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(10.0f);

            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            EditorUtils.PrintBooleanSquaredByRef(ref this.EnableOnBuild,    "Show log's into screen", "oo_prefab", 16, 12);
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("Log information will appear on main UI during playmode even after game build", "oo_info", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(10.0f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20.0f);
            EditorGUILayout.BeginVertical();
            EditorUtils.PrintBooleanSquaredByRef(ref this.CaptureLogs,      "Show Logs", "oo_note", 16, 12);
            EditorUtils.PrintBooleanSquaredByRef(ref this.CaptureWarnings,  "Show Warnings", "oo_info", 16, 12);
            EditorUtils.PrintBooleanSquaredByRef(ref this.CaptureErrors,    "Show Errors", "oo_error", 16, 12);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorUtils.HorizontalLine(EditorUtils.LINE_DIVISOR_COLOR, 1.0f, new Vector2(5f, 5f));
            EditorUtils.PrintBooleanSquaredByRef(ref this.ShowGizmos, "Show Gizmos", "oo_gizmo", 16, 12);
            GUILayout.Space(5.0f);
            EditorUtils.PrintExplanationLabel("Show network objects gizmos", "oo_info", EditorUtils.SIMPLE_EXPLANATION_FONT_COLOR);
            GUILayout.Space(10.0f);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
