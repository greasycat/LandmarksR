using System.Collections.Generic;
using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Experiment.UI;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    public class NBackTask : CognitiveTrialTaskBase
    {
        [SerializeField] private string stimulusColumn = "stimulus";
        [SerializeField] private string nValueColumn = "n_value";

        protected override void PresentStimulus(CognitiveStimulusPresenter presenter, DataFrame trialData)
        {
            presenter.ShowCenteredText(
                CognitiveTrialDataUtilities.GetRequiredString(trialData, stimulusColumn, name), Color.white);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetTaskSpecificContext(DataFrame trialData)
        {
            yield return new KeyValuePair<string, string>("n_value",
                CognitiveTrialDataUtilities.GetOptionalString(trialData, nValueColumn));
        }

        protected override IEnumerable<string> GetTaskSpecificOutputColumns()
        {
            yield return "n_value";
        }

        protected override string GetStimulusLabel(DataFrame trialData)
        {
            return CognitiveTrialDataUtilities.GetRequiredString(trialData, stimulusColumn, name);
        }
    }
}
