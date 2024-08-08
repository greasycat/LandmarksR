using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Player;
using LandmarksR.Scripts.Experiment.Tasks.Structural;
using LandmarksR.Scripts.Experiment.UI;
using UnityEngine;
using UnityEngine.Assertions;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    public abstract class CognitiveTrialTaskBase : BaseTask, RepeatTask.IRepeatTaskOutputProvider
    {
        [Header("Trial Columns")]
        [SerializeField] private string trialIdColumn = CognitiveTrialColumns.TrialId;
        [SerializeField] private string correctResponseColumn = CognitiveTrialColumns.CorrectResponse;
        [SerializeField] private string conditionColumn = CognitiveTrialColumns.Condition;
        [SerializeField] private string fixationDurationColumn = CognitiveTrialColumns.FixationMs;
        [SerializeField] private string stimulusDurationColumn = CognitiveTrialColumns.StimulusMs;
        [SerializeField] private string responseWindowColumn = CognitiveTrialColumns.ResponseWindowMs;
        [SerializeField] private string interTrialDurationColumn = "inter_trial_ms";

        [Header("Trial Defaults")]
        [SerializeField] private int defaultFixationMs = 500;
        [SerializeField] private int defaultStimulusMs = 1000;
        [SerializeField] private int defaultResponseWindowMs = 1500;
        [SerializeField] private int defaultInterTrialMs = 250;
        [SerializeField] private string fixationSymbol = "+";

        [Header("Response Bindings")]
        [SerializeField] private List<ResponseBinding> responseBindings = new()
        {
            new ResponseBinding { response = "left", key = KeyCode.LeftArrow },
            new ResponseBinding { response = "right", key = KeyCode.RightArrow },
            new ResponseBinding { response = "space", key = KeyCode.Space }
        };

        private readonly Dictionary<KeyCode, PlayerEventController.KeyboardEventHandler> _registeredHandlers = new();

        private bool _hasLoggedResult;
        private float _stimulusStartTime;

        protected RepeatTask ParentRepeatTask { get; private set; }
        protected DataFrame TrialData { get; private set; }
        protected CognitiveStimulusPresenter Presenter { get; private set; }

        protected override void Prepare()
        {
            SetTaskType(TaskType.Interactive);
            base.Prepare();

            ParentRepeatTask = GetComponentInParent<RepeatTask>();
            Assert.IsNotNull(ParentRepeatTask, $"{name} must be placed under a RepeatTask.");

            TrialData = ParentRepeatTask.CurrentData;
            Assert.IsNotNull(TrialData, $"{name} requires current trial data from RepeatTask.");
            _hasLoggedResult = false;

            Presenter = CognitiveStimulusPresenter.GetOrCreate();
            Presenter.ClearAll();

            RegisterBindings();
            StartCoroutine(RunTrial());
        }

        public IEnumerable<string> GetRepeatOutputColumns()
        {
            var commonColumns = new[]
            {
                CognitiveTrialColumns.TrialId,
                CognitiveTrialColumns.TaskType,
                CognitiveTrialColumns.Condition,
                CognitiveTrialColumns.Stimulus,
                CognitiveTrialColumns.SelectedResponse,
                CognitiveTrialColumns.CorrectResponse,
                CognitiveTrialColumns.IsCorrect,
                CognitiveTrialColumns.HasResponse,
                CognitiveTrialColumns.ReactionTimeMs,
                CognitiveTrialColumns.FixationMs,
                CognitiveTrialColumns.StimulusMs,
                CognitiveTrialColumns.ResponseWindowMs
            };

            return commonColumns.Concat(GetTaskSpecificOutputColumns()).Distinct();
        }

        public override void Finish()
        {
            base.Finish();
            UnregisterBindings();
            Presenter?.ClearAll();
        }

        protected abstract void PresentStimulus(CognitiveStimulusPresenter presenter, DataFrame trialData);
        protected abstract IEnumerable<KeyValuePair<string, string>> GetTaskSpecificContext(DataFrame trialData);
        protected abstract IEnumerable<string> GetTaskSpecificOutputColumns();
        protected abstract string GetStimulusLabel(DataFrame trialData);

        private IEnumerator RunTrial()
        {
            var fixationMs = CognitiveTrialDataUtilities.GetOptionalInt(TrialData, fixationDurationColumn, defaultFixationMs);
            var stimulusMs = CognitiveTrialDataUtilities.GetOptionalInt(TrialData, stimulusDurationColumn, defaultStimulusMs);
            var responseWindowMs =
                CognitiveTrialDataUtilities.GetOptionalInt(TrialData, responseWindowColumn, defaultResponseWindowMs);
            var interTrialMs =
                CognitiveTrialDataUtilities.GetOptionalInt(TrialData, interTrialDurationColumn, defaultInterTrialMs);

            if (fixationMs > 0)
            {
                Presenter.ShowFixation(fixationSymbol);
                yield return new WaitForSecondsRealtime(fixationMs / 1000f);
            }

            PresentStimulus(Presenter, TrialData);
            _stimulusStartTime = Time.realtimeSinceStartup;

            var stimulusVisible = true;
            while (!_hasLoggedResult)
            {
                var elapsedMs = (Time.realtimeSinceStartup - _stimulusStartTime) * 1000f;

                if (stimulusVisible && stimulusMs >= 0 && elapsedMs >= stimulusMs)
                {
                    Presenter.ClearStimulus();
                    stimulusVisible = false;
                }

                if (elapsedMs >= responseWindowMs)
                {
                    CommitResult(CognitiveTrialEvaluator.Evaluate(string.Empty, GetCorrectResponse(), null), fixationMs,
                        stimulusMs, responseWindowMs);
                    break;
                }

                yield return null;
            }

            Presenter.ClearStimulus();

            if (interTrialMs > 0)
            {
                yield return new WaitForSecondsRealtime(interTrialMs / 1000f);
            }

            StopCurrentTask();
        }

        private void RegisterBindings()
        {
            UnregisterBindings();

            foreach (var binding in responseBindings.Where(binding =>
                         !string.IsNullOrWhiteSpace(binding.response) && !_registeredHandlers.ContainsKey(binding.key)))
            {
                var capturedResponse = binding.response.Trim();
                PlayerEventController.KeyboardEventHandler handler = () => OnResponse(capturedResponse);

                _registeredHandlers.Add(binding.key, handler);
                PlayerEvent.RegisterKeyHandler(binding.key, handler);
            }
        }

        private void UnregisterBindings()
        {
            foreach (var pair in _registeredHandlers)
            {
                PlayerEvent.UnregisterKeyHandler(pair.Key, pair.Value);
            }

            _registeredHandlers.Clear();
        }

        private void OnResponse(string response)
        {
            if (_hasLoggedResult)
            {
                return;
            }

            var elapsedMs = (Time.realtimeSinceStartup - _stimulusStartTime) * 1000f;
            var result = CognitiveTrialEvaluator.Evaluate(response, GetCorrectResponse(), elapsedMs);
            CommitResult(result,
                CognitiveTrialDataUtilities.GetOptionalInt(TrialData, fixationDurationColumn, defaultFixationMs),
                CognitiveTrialDataUtilities.GetOptionalInt(TrialData, stimulusDurationColumn, defaultStimulusMs),
                CognitiveTrialDataUtilities.GetOptionalInt(TrialData, responseWindowColumn, defaultResponseWindowMs));
        }

        private void CommitResult(CognitiveTrialResult result, int fixationMs, int stimulusMs, int responseWindowMs)
        {
            if (_hasLoggedResult)
            {
                return;
            }

            _hasLoggedResult = true;
            var trialId = CognitiveTrialDataUtilities.GetOptionalString(TrialData, trialIdColumn,
                ParentRepeatTask.currentRepeat.ToString());
            var condition = CognitiveTrialDataUtilities.GetOptionalString(TrialData, conditionColumn);

            ParentRepeatTask.Context[CognitiveTrialColumns.TrialId] = trialId;
            ParentRepeatTask.Context[CognitiveTrialColumns.TaskType] = GetType().Name;
            ParentRepeatTask.Context[CognitiveTrialColumns.Condition] = condition;
            ParentRepeatTask.Context[CognitiveTrialColumns.Stimulus] = GetStimulusLabel(TrialData);
            ParentRepeatTask.Context[CognitiveTrialColumns.SelectedResponse] = result.SelectedResponse;
            ParentRepeatTask.Context[CognitiveTrialColumns.CorrectResponse] = result.CorrectResponse;
            ParentRepeatTask.Context[CognitiveTrialColumns.IsCorrect] = result.IsCorrect.ToString().ToLowerInvariant();
            ParentRepeatTask.Context[CognitiveTrialColumns.HasResponse] = result.HasResponse.ToString().ToLowerInvariant();
            ParentRepeatTask.Context[CognitiveTrialColumns.ReactionTimeMs] =
                CognitiveTrialDataUtilities.ToLogString(result.ReactionTimeMs);
            ParentRepeatTask.Context[CognitiveTrialColumns.FixationMs] = fixationMs.ToString();
            ParentRepeatTask.Context[CognitiveTrialColumns.StimulusMs] = stimulusMs.ToString();
            ParentRepeatTask.Context[CognitiveTrialColumns.ResponseWindowMs] = responseWindowMs.ToString();

            foreach (var pair in GetTaskSpecificContext(TrialData))
            {
                ParentRepeatTask.Context[pair.Key] = pair.Value ?? string.Empty;
            }
        }

        private string GetCorrectResponse()
        {
            return CognitiveTrialDataUtilities.GetRequiredString(TrialData, correctResponseColumn, name);
        }
    }
}
