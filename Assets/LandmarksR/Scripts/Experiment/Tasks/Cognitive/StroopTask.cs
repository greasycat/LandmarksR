using System.Collections.Generic;
using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Experiment.UI;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    public class StroopTask : CognitiveTrialTaskBase
    {
        [SerializeField] private string wordColumn = "word";
        [SerializeField] private string inkColorColumn = "ink_color";

        protected override void PresentStimulus(CognitiveStimulusPresenter presenter, DataFrame trialData)
        {
            var word = CognitiveTrialDataUtilities.GetRequiredString(trialData, wordColumn, name);
            var inkColorRaw = CognitiveTrialDataUtilities.GetRequiredString(trialData, inkColorColumn, name);
            var inkColor = CognitiveTrialDataUtilities.ParseColor(inkColorRaw, Color.white);

            presenter.ShowCenteredText(word, inkColor);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetTaskSpecificContext(DataFrame trialData)
        {
            yield return new KeyValuePair<string, string>("word",
                CognitiveTrialDataUtilities.GetRequiredString(trialData, wordColumn, name));
            yield return new KeyValuePair<string, string>("ink_color",
                CognitiveTrialDataUtilities.GetRequiredString(trialData, inkColorColumn, name));
        }

        protected override IEnumerable<string> GetTaskSpecificOutputColumns()
        {
            yield return "word";
            yield return "ink_color";
        }

        protected override string GetStimulusLabel(DataFrame trialData)
        {
            var word = CognitiveTrialDataUtilities.GetRequiredString(trialData, wordColumn, name);
            var inkColor = CognitiveTrialDataUtilities.GetRequiredString(trialData, inkColorColumn, name);
            return $"{word}:{inkColor}";
        }
    }
}
