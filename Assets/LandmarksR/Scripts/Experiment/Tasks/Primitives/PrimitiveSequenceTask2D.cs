using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public class PrimitiveSequenceTask2D : PrimitiveSequenceTaskBase<PrimitiveTarget2D>
    {
        protected override void ApplyRow(PrimitiveTarget2D target, PrimitiveSequenceRow row)
        {
            var rectTransform = target.TargetTransform;
            if (rectTransform == null)
            {
                Logger.E("sequence", $"{target.name} is missing a RectTransform.");
                return;
            }

            switch (row.Operation)
            {
                case PrimitiveOperation.Translate:
                    if (row.Space == PrimitiveCoordinateSpace.World)
                    {
                        rectTransform.position = row.Vector;
                    }
                    else
                    {
                        rectTransform.anchoredPosition3D = row.Vector;
                    }

                    break;
                case PrimitiveOperation.Rotate:
                    if (row.Space == PrimitiveCoordinateSpace.World)
                    {
                        rectTransform.rotation = Quaternion.Euler(row.Vector);
                    }
                    else
                    {
                        rectTransform.localRotation = Quaternion.Euler(row.Vector);
                    }

                    break;
                case PrimitiveOperation.Scale:
                    rectTransform.localScale = row.Vector;
                    break;
                case PrimitiveOperation.Color:
                    foreach (var graphic in target.Graphics)
                    {
                        if (graphic != null)
                        {
                            graphic.color = row.Color;
                        }
                    }

                    break;
                case PrimitiveOperation.Visibility:
                    target.gameObject.SetActive(row.Visible);
                    break;
                case PrimitiveOperation.Custom:
                    DispatchCustomActions(target, row);
                    break;
            }
        }
    }
}
