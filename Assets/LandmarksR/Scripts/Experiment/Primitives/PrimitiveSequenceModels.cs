using System.Collections.Generic;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public enum PrimitiveOperation
    {
        Translate,
        Rotate,
        Scale,
        Color,
        Visibility,
        Custom
    }

    public enum PrimitiveCoordinateSpace
    {
        Local,
        World
    }

    public interface IPrimitiveSequenceAction
    {
        void ApplyCustomAction(PrimitiveSequenceRow row);
    }

    public sealed class PrimitiveSequenceRow
    {
        public PrimitiveSequenceRow(int sequence, int timestampMs, string targetId, PrimitiveOperation operation,
            Vector3 vector, Color color, bool visible, int durationMs, PrimitiveCoordinateSpace space, string customKey,
            string customValue, IReadOnlyDictionary<string, string> rawValues)
        {
            Sequence = sequence;
            TimestampMs = timestampMs;
            TargetId = targetId;
            Operation = operation;
            Vector = vector;
            Color = color;
            Visible = visible;
            DurationMs = durationMs;
            Space = space;
            CustomKey = customKey;
            CustomValue = customValue;
            RawValues = rawValues;
        }

        public int Sequence { get; }
        public int TimestampMs { get; }
        public string TargetId { get; }
        public PrimitiveOperation Operation { get; }
        public Vector3 Vector { get; }
        public Color Color { get; }
        public bool Visible { get; }
        public int DurationMs { get; }
        public PrimitiveCoordinateSpace Space { get; }
        public string CustomKey { get; }
        public string CustomValue { get; }
        public IReadOnlyDictionary<string, string> RawValues { get; }
    }

    public sealed class PrimitiveSequenceStep
    {
        public PrimitiveSequenceStep(int sequence, int timestampMs, IReadOnlyList<PrimitiveSequenceRow> rows)
        {
            Sequence = sequence;
            TimestampMs = timestampMs;
            Rows = rows;
        }

        public int Sequence { get; }
        public int TimestampMs { get; }
        public IReadOnlyList<PrimitiveSequenceRow> Rows { get; }
    }
}
