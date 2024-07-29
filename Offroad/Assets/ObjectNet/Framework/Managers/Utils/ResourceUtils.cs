using UnityEditor;

namespace com.onlineobject.objectnet {
#if UNITY_EDITOR
    /// <summary>
    /// Class ResourceUtils.
    /// </summary>
    public static class ResourceUtils {
        /// <summary>
        /// Gets the editors path.
        /// </summary>
        /// <value>The editors path.</value>
        public static string EditorsPath {
            get {
                var g = AssetDatabase.FindAssets($"t:Script {nameof(ResourceUtils)}");
                return AssetDatabase.GUIDToAssetPath(g[0]);
            }
        }

        /// <summary>
        /// Gets the resources path.
        /// </summary>
        /// <value>The resources path.</value>
        public static string ResourcesPath {
            get {
                var g = AssetDatabase.FindAssets($"t:Script {nameof(ResourceUtils)}");
                string result = AssetDatabase.GUIDToAssetPath(g[0]);
                result  = result.Replace("ResourceUtils.cs", "");
                result += "Resources";
                return result;
            }
        }

        /// <summary>
        /// Gets the database path.
        /// </summary>
        /// <value>The database path.</value>
        public static string DatabasePath {
            get {
                var g = AssetDatabase.FindAssets($"t:Script {nameof(ResourceUtils)}");
                string result = AssetDatabase.GUIDToAssetPath(g[0]);
                result  = result.Replace("ResourceUtils.cs", "");
                result += "Resources/Database";
                return result;
            }
        }
    }
#endif
}