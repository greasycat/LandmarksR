#if UNITY_EDITOR
using UnityEditor;

namespace LandmarksR.Scripts.Editor
{
    public static class ExpandInspector
    {
        /// <summary>
        /// Expands the selected GameObject in the Hierarchy window.
        /// </summary>
        [MenuItem("LandmarksR/Shortcuts/Expand Selected Tasks &q")]
        //https://stackoverflow.com/a/66366775
        private static void ExpandTasks()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = EditorWindow.GetWindow(type);
            var expandCallback = type.GetMethod("SetExpandedRecursive");
            if (expandCallback != null)
                expandCallback.Invoke(window, new object[] {Selection.activeGameObject.GetInstanceID(), true});
        }

        [MenuItem("LandmarksR/Shortcuts/Expand Selected Tasks &q", true)]
        private static bool ValidateExpandTasks()
        {
            return Selection.activeGameObject != null;
        }
    }
}
#endif
