using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Experiment.Tasks.Structural;
using LandmarksR.Scripts.Experiment.UI;
using UnityEngine;
using UnityEngine.Assertions;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    public class SetStimulusTextColorTask : BaseTask
    {
        [SerializeField] private string colorColumn = "ink_color";
        [SerializeField] private Color fallbackColor = Color.white;

        protected override void Prepare()
        {
            SetTaskType(TaskType.Functional);
            base.Prepare();

            var parentRepeatTask = GetComponentInParent<RepeatTask>();
            Assert.IsNotNull(parentRepeatTask, $"{name} must be placed under a RepeatTask.");

            var trialData = parentRepeatTask.CurrentData;
            Assert.IsNotNull(trialData, $"{name} requires current trial data from RepeatTask.");

            var rawColor = CognitiveTrialDataUtilities.GetRequiredString(trialData, colorColumn, name);
            var color = CognitiveTrialDataUtilities.ParseColor(rawColor, fallbackColor);

            CognitiveStimulusPresenter.GetOrCreate().SetCenteredTextColor(color);
        }
    }
}
