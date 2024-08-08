using LandmarksR.Scripts.Experiment;
using NUnit.Framework;

namespace LandmarksR.Scripts.Tests.Data
{
    [TestFixture]
    public class ExperimentSettingsTests
    {
        [Test]
        public void StartNewRunSession_GeneratesUuidAndClearsLegacyPlaceholderSubject()
        {
            var settings = new ExperimentSettings
            {
                subjectId = "default_participant_0"
            };

            settings.StartNewRunSession();

            Assert.That(settings.subjectId, Is.EqualTo(string.Empty));
            Assert.That(settings.runSessionId, Is.Not.Empty);
            Assert.That(System.Guid.TryParse(settings.runSessionId, out _), Is.True);
        }

        [Test]
        public void SetSubjectId_TrimsWhitespace()
        {
            var settings = new ExperimentSettings();

            settings.SetSubjectId("  S-1001  ");

            Assert.That(settings.subjectId, Is.EqualTo("S-1001"));
        }
    }
}
