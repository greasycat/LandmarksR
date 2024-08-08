using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LandmarksR.Scripts.Experiment.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public abstract class PrimitiveSequenceTaskBase<TTarget> : BaseTask where TTarget : PrimitiveTargetBase
    {
        [SerializeField] private Table sequenceTable;
        [SerializeField] private bool includeInactiveTargets = true;

        private Dictionary<string, TTarget> _targets;
        private List<PrimitiveSequenceStep> _steps;

        protected override void Prepare()
        {
            SetTaskType(TaskType.Interactive);
            base.Prepare();

            Assert.IsNotNull(sequenceTable, $"{name} requires a sequence table.");

            sequenceTable.EnsurePrepared();
            _steps = PrimitiveSequenceParser.Parse(sequenceTable.Data);
            _targets = ResolveTargets(includeInactiveTargets);
            ValidateTargets();

            StartCoroutine(RunSequence());
        }

        protected abstract void ApplyRow(TTarget target, PrimitiveSequenceRow row);

        protected void DispatchCustomActions(PrimitiveTargetBase target, PrimitiveSequenceRow row)
        {
            foreach (var action in target.GetComponents<MonoBehaviour>().OfType<IPrimitiveSequenceAction>())
            {
                action.ApplyCustomAction(row);
            }
        }

        private IEnumerator RunSequence()
        {
            var currentSequence = int.MinValue;
            var previousTimestampMs = 0;

            foreach (var step in _steps)
            {
                if (step.Sequence != currentSequence)
                {
                    currentSequence = step.Sequence;
                    previousTimestampMs = 0;
                }

                var waitTimeMs = Mathf.Max(0, step.TimestampMs - previousTimestampMs);
                if (waitTimeMs > 0)
                {
                    yield return new WaitForSecondsRealtime(waitTimeMs / 1000f);
                }

                foreach (var row in step.Rows)
                {
                    var target = _targets[row.TargetId];
                    ApplyRow(target, row);
                    Logger.I("sequence", $"{row.Operation} -> {row.TargetId} @ {step.TimestampMs}ms");
                }

                previousTimestampMs = step.TimestampMs;
            }

            StopCurrentTask();
        }

        private Dictionary<string, TTarget> ResolveTargets(bool includeInactive)
        {
            IEnumerable<TTarget> sceneTargets;
            if (includeInactive)
            {
                sceneTargets = Resources.FindObjectsOfTypeAll<TTarget>()
                    .Where(target => target.gameObject.scene.IsValid());
            }
            else
            {
                sceneTargets = FindObjectsOfType<TTarget>();
            }

            var resolvedTargets = new Dictionary<string, TTarget>();
            foreach (var target in sceneTargets)
            {
                if (resolvedTargets.ContainsKey(target.TargetId))
                {
                    Logger.W("sequence", $"Duplicate primitive target id '{target.TargetId}' found. Using the first one.");
                    continue;
                }

                resolvedTargets.Add(target.TargetId, target);
            }

            return resolvedTargets;
        }

        private void ValidateTargets()
        {
            var missingTargets = _steps
                .SelectMany(step => step.Rows)
                .Select(row => row.TargetId)
                .Distinct()
                .Where(targetId => !_targets.ContainsKey(targetId))
                .ToList();

            Assert.IsTrue(missingTargets.Count == 0,
                $"{name} cannot find primitive targets: {string.Join(", ", missingTargets)}");
        }
    }
}
