using System.Collections.Generic;
using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Experiment.UI;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    public class FlankerTask : CognitiveTrialTaskBase
    {
        [SerializeField] private string centerSymbolColumn = "center_symbol";
        [SerializeField] private string flankerSymbolColumn = "flanker_symbol";
        [SerializeField] private int flankCount = 2;

        protected override void PresentStimulus(CognitiveStimulusPresenter presenter, DataFrame trialData)
        {
            presenter.ShowFlanker(
                CognitiveTrialDataUtilities.GetRequiredString(trialData, centerSymbolColumn, name),
                CognitiveTrialDataUtilities.GetRequiredString(trialData, flankerSymbolColumn, name),
                Color.white, Color.white, flankCount);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetTaskSpecificContext(DataFrame trialData)
        {
            yield return new KeyValuePair<string, string>("center_symbol",
                CognitiveTrialDataUtilities.GetRequiredString(trialData, centerSymbolColumn, name));
            yield return new KeyValuePair<string, string>("flanker_symbol",
                CognitiveTrialDataUtilities.GetRequiredString(trialData, flankerSymbolColumn, name));
        }

        protected override IEnumerable<string> GetTaskSpecificOutputColumns()
        {
            yield return "center_symbol";
            yield return "flanker_symbol";
        }

        protected override string GetStimulusLabel(DataFrame trialData)
        {
            var center = CognitiveTrialDataUtilities.GetRequiredString(trialData, centerSymbolColumn, name);
            var flanker = CognitiveTrialDataUtilities.GetRequiredString(trialData, flankerSymbolColumn, name);
            return $"{flanker}{flanker}{center}{flanker}{flanker}";
        }
    }
}
