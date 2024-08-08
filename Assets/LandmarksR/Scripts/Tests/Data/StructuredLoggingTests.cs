using System.Collections.Generic;
using LandmarksR.Scripts.Experiment;
using LandmarksR.Scripts.Experiment.Log;
using NUnit.Framework;

namespace LandmarksR.Scripts.Tests.Data
{
    [TestFixture]
    public class StructuredLoggingTests
    {
        [Test]
        public void LoggingSettings_ApplyDefaults_ConfiguresJsonFirstOutputs()
        {
            var settings = new LoggingSettings
            {
                jsonFileExtension = string.Empty,
                eventFileName = string.Empty,
                exportTsv = false,
                exportCsv = false
            };

            settings.ApplyDefaults();

            Assert.That(settings.GetJsonFileExtension(), Is.EqualTo("jsonl"));
            Assert.That(settings.GetEventBaseFileName(), Is.EqualTo("events"));
            Assert.That(settings.ShouldExportTsv(), Is.True);
            Assert.That(settings.ShouldExportCsv(), Is.True);
        }

        [Test]
        public void LegacyEventAdapter_Create_MapsSubjectRegistration()
        {
            var record = LegacyEventAdapter.Create("subject", LogLevel.Info, "SubjectRegistered:S-2048",
                "run-1", "unassigned_subject");

            Assert.That(record.record_type, Is.EqualTo("event"));
            Assert.That(record.source, Is.EqualTo("subject"));
            Assert.That(record.level, Is.EqualTo("info"));
            Assert.That(record.event_name, Is.EqualTo("subject_registered"));
            Assert.That(record.subject_id, Is.EqualTo("S-2048"));
            Assert.That(record.payload["subject_id"], Is.EqualTo("S-2048"));
        }

        [Test]
        public void LegacyEventAdapter_Create_MapsTaskLifecycle()
        {
            var started = LegacyEventAdapter.Create("task", LogLevel.Info, "(NBack Trial) Started", "run-1", "S-1");
            var finished = LegacyEventAdapter.Create("task", LogLevel.Info, "(NBack Trial) Finished", "run-1", "S-1");

            Assert.That(started.event_name, Is.EqualTo("task_started"));
            Assert.That(started.task_name, Is.EqualTo("NBack Trial"));
            Assert.That(finished.event_name, Is.EqualTo("task_finished"));
            Assert.That(finished.task_name, Is.EqualTo("NBack Trial"));
        }

        [Test]
        public void BuildDatasetColumns_PrefixesEnvelopeColumnsWithoutDuplicates()
        {
            var columns = StructuredLogging.BuildDatasetColumns(new[]
            {
                ExperimentMetadataColumns.SubjectId,
                "trial_id",
                ExperimentMetadataColumns.RunSessionId,
                "response"
            });

            CollectionAssert.AreEqual(new[]
            {
                ExperimentMetadataColumns.RunSessionId,
                ExperimentMetadataColumns.SubjectId,
                "ts_utc",
                "ts_unix_ms",
                "dataset_name",
                "trial_id",
                "response"
            }, columns);
        }

        [Test]
        public void BuildDatasetRow_FlattensEnvelopeAndRowValues()
        {
            var record = new DatasetLogRecord
            {
                dataset_name = "nback_text",
                ts_utc = "2026-04-05T19:21:00.000Z",
                ts_unix_ms = 1775416860000,
                run_session_id = "run-77",
                subject_id = "S-77",
                row = new Dictionary<string, string>
                {
                    ["trial_id"] = "12",
                    ["stimulus"] = "B"
                }
            };

            var row = StructuredLogging.BuildDatasetRow(record, new[] { "trial_id", "stimulus" });

            Assert.That(row[ExperimentMetadataColumns.RunSessionId], Is.EqualTo("run-77"));
            Assert.That(row[ExperimentMetadataColumns.SubjectId], Is.EqualTo("S-77"));
            Assert.That(row["dataset_name"], Is.EqualTo("nback_text"));
            Assert.That(row["trial_id"], Is.EqualTo("12"));
            Assert.That(row["stimulus"], Is.EqualTo("B"));
        }

        [Test]
        public void FormatDelimitedRow_EscapesQuotesTabsAndNewlines()
        {
            var columns = new[] { "plain", "quoted", "tabbed", "multiline" };
            var row = new Dictionary<string, string>
            {
                ["plain"] = "alpha",
                ["quoted"] = "say \"hello\"",
                ["tabbed"] = "A\tB",
                ["multiline"] = "line1\nline2"
            };

            var tsv = StructuredLogging.FormatDelimitedRow(columns, row, '\t');
            var csv = StructuredLogging.FormatDelimitedRow(columns, row, ',');

            Assert.That(tsv, Is.EqualTo("alpha\t\"say \"\"hello\"\"\"\t\"A\tB\"\t\"line1\nline2\""));
            Assert.That(csv, Is.EqualTo("alpha,\"say \"\"hello\"\"\",A\tB,\"line1\nline2\""));
        }
    }
}
