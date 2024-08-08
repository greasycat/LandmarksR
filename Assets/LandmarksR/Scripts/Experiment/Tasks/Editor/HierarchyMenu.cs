#if UNITY_EDITOR
using LandmarksR.Scripts.Editor;
using LandmarksR.Scripts.Experiment.Tasks.Cognitive;
using LandmarksR.Scripts.Experiment.Tasks.Interactive;
using LandmarksR.Scripts.Experiment.Tasks.Primitives;
using LandmarksR.Scripts.Experiment.Tasks.Structural;
using UnityEditor;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Editor
{
    public class HierarchyMenu : MonoBehaviour
    {
        [MenuItem("GameObject/Experiment/Tasks/1. RootTask", false, 1)]
        private static void CreateRootTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<RootTask>(menuCommand, "RootTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/2. CollectionTask", false, 1)]
        private static void CreateCollectionTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<CollectionTask>(menuCommand, "CollectionTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/2. InstructionTask", false, 1)]
        private static void CreateInstructionTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<InstructionTask>(menuCommand, "InstructionTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/3. SubjectRegistryTask", false, 1)]
        private static void CreateSubjectRegistryTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<SubjectRegistryTask>(menuCommand, "SubjectRegistryTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/4. RepeatTask", false, 1)]
        private static void CreateRepeatTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<RepeatTask>(menuCommand, "RepeatTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/5. ExploreTask", false, 1)]
        private static void CreateExploreTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<ExploreTask>(menuCommand, "ExploreTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/6. NBackTask", false, 1)]
        private static void CreateNBackTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<NBackTask>(menuCommand, "NBackTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/7. StroopTask", false, 1)]
        private static void CreateStroopTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<StroopTask>(menuCommand, "StroopTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/8. FlankerTask", false, 1)]
        private static void CreateFlankerTask(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<FlankerTask>(menuCommand, "FlankerTask");
        }

        [MenuItem("GameObject/Experiment/Tasks/9. PrimitiveSequenceTask2D", false, 1)]
        private static void CreatePrimitiveSequenceTask2D(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<PrimitiveSequenceTask2D>(menuCommand, "PrimitiveSequenceTask2D");
        }

        [MenuItem("GameObject/Experiment/Tasks/10. PrimitiveSequenceTask3D", false, 1)]
        private static void CreatePrimitiveSequenceTask3D(MenuCommand menuCommand)
        {
            CreateMenuItem.CreateGameObjectContextMenu<PrimitiveSequenceTask3D>(menuCommand, "PrimitiveSequenceTask3D");
        }
    }
}
#endif
