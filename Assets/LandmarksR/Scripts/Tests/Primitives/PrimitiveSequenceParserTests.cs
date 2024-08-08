using System;
using System.Collections.Generic;
using LandmarksR.Scripts.Experiment.Data;
using LandmarksR.Scripts.Experiment.Tasks.Primitives;
using NUnit.Framework;
using UnityEngine;

namespace LandmarksR.Scripts.Tests.Primitives
{
    [TestFixture]
    public class PrimitiveSequenceParserTests
    {
        [Test]
        public void Parse_SortsRowsAndGroupsStepsBySequenceAndTimestamp()
        {
            var dataFrame = CreateDataFrame(
                new[] { "sequence", "timestamp_ms", "target_id", "operation", "x", "y", "z" },
                new object[] { "2", "150", "CubeA", "Translate", "2", "0", "0" },
                new object[] { "1", "100", "CubeA", "Translate", "1", "0", "0" },
                new object[] { "1", "100", "CubeB", "Scale", "1", "2", "1" });

            var steps = PrimitiveSequenceParser.Parse(dataFrame);

            Assert.AreEqual(2, steps.Count);
            Assert.AreEqual(1, steps[0].Sequence);
            Assert.AreEqual(100, steps[0].TimestampMs);
            Assert.AreEqual(2, steps[0].Rows.Count);
            Assert.AreEqual(2, steps[1].Sequence);
            Assert.AreEqual("CubeA", steps[1].Rows[0].TargetId);
            Assert.AreEqual(new Vector3(2f, 0f, 0f), steps[1].Rows[0].Vector);
        }

        [Test]
        public void Parse_ColorRowNormalizesByteColors()
        {
            var dataFrame = CreateDataFrame(
                new[] { "sequence", "timestamp_ms", "target_id", "operation", "r", "g", "b", "a" },
                new object[] { "1", "0", "Stimulus", "Color", "255", "128", "0", "128" });

            var steps = PrimitiveSequenceParser.Parse(dataFrame);
            var color = steps[0].Rows[0].Color;

            Assert.That(color.r, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(128f / 255f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(color.a, Is.EqualTo(128f / 255f).Within(0.0001f));
        }

        [Test]
        public void Parse_VisibilityRowParsesBooleanValues()
        {
            var dataFrame = CreateDataFrame(
                new[] { "sequence", "timestamp_ms", "target_id", "operation", "visible" },
                new object[] { "1", "0", "Stimulus", "Visibility", "false" });

            var steps = PrimitiveSequenceParser.Parse(dataFrame);

            Assert.IsFalse(steps[0].Rows[0].Visible);
        }

        [Test]
        public void Parse_MissingRequiredColumn_Throws()
        {
            var dataFrame = CreateDataFrame(
                new[] { "sequence", "timestamp_ms", "target_id" },
                new object[] { "1", "0", "Stimulus" });

            Assert.Throws<ArgumentException>(() => PrimitiveSequenceParser.Parse(dataFrame));
        }

        [Test]
        public void Parse_InvalidVectorValue_Throws()
        {
            var dataFrame = CreateDataFrame(
                new[] { "sequence", "timestamp_ms", "target_id", "operation", "x", "y", "z" },
                new object[] { "1", "0", "Stimulus", "Translate", "bad", "0", "0" });

            Assert.Throws<ArgumentException>(() => PrimitiveSequenceParser.Parse(dataFrame));
        }

        private static DataFrame CreateDataFrame(IReadOnlyList<string> headers, params object[][] rows)
        {
            var dataFrame = new DataFrame(headers);
            foreach (var row in rows)
            {
                dataFrame.AppendRow(new List<object>(row));
            }

            return dataFrame;
        }
    }
}
