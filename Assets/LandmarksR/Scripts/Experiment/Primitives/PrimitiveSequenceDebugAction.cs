using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public class PrimitiveSequenceDebugAction : MonoBehaviour, IPrimitiveSequenceAction
    {
        [SerializeField] private string lastCustomKey;
        [SerializeField] private string lastCustomValue;

        public void ApplyCustomAction(PrimitiveSequenceRow row)
        {
            lastCustomKey = row.CustomKey;
            lastCustomValue = row.CustomValue;
            UnityEngine.Debug.Log($"[PrimitiveSequenceDebugAction] {name}: {lastCustomKey}={lastCustomValue}");
        }
    }
}
