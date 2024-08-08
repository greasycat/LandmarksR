using LandmarksR.Scripts.Experiment.Tasks.Cognitive;
using NUnit.Framework;

namespace LandmarksR.Scripts.Tests.Cognitive
{
    [TestFixture]
    public class CognitiveTrialEvaluatorTests
    {
        [Test]
        public void Evaluate_WithMatchingResponse_ReturnsCorrectTrialResult()
        {
            var result = CognitiveTrialEvaluator.Evaluate("Left", "left", 512f);

            Assert.IsTrue(result.HasResponse);
            Assert.IsTrue(result.IsCorrect);
            Assert.AreEqual("Left", result.SelectedResponse);
            Assert.AreEqual("left", result.CorrectResponse);
            Assert.AreEqual(512f, result.ReactionTimeMs);
        }

        [Test]
        public void Evaluate_WithoutResponse_ReturnsIncorrectTrialResultWithoutReactionTime()
        {
            var result = CognitiveTrialEvaluator.Evaluate(string.Empty, "right", 700f);

            Assert.IsFalse(result.HasResponse);
            Assert.IsFalse(result.IsCorrect);
            Assert.AreEqual(string.Empty, result.SelectedResponse);
            Assert.AreEqual("right", result.CorrectResponse);
            Assert.IsNull(result.ReactionTimeMs);
        }
    }
}
