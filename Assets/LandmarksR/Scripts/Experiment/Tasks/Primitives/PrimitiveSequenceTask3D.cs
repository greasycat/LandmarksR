using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public class PrimitiveSequenceTask3D : PrimitiveSequenceTaskBase<PrimitiveTarget3D>
    {
        protected override void ApplyRow(PrimitiveTarget3D target, PrimitiveSequenceRow row)
        {
            var targetTransform = target.TargetTransform;

            switch (row.Operation)
            {
                case PrimitiveOperation.Translate:
                    if (row.Space == PrimitiveCoordinateSpace.World)
                    {
                        targetTransform.position = row.Vector;
                    }
                    else
                    {
                        targetTransform.localPosition = row.Vector;
                    }

                    break;
                case PrimitiveOperation.Rotate:
                    if (row.Space == PrimitiveCoordinateSpace.World)
                    {
                        targetTransform.rotation = Quaternion.Euler(row.Vector);
                    }
                    else
                    {
                        targetTransform.localRotation = Quaternion.Euler(row.Vector);
                    }

                    break;
                case PrimitiveOperation.Scale:
                    targetTransform.localScale = row.Vector;
                    break;
                case PrimitiveOperation.Color:
                    foreach (var renderer in target.Renderers)
                    {
                        if (renderer == null)
                        {
                            continue;
                        }

                        foreach (var material in renderer.materials)
                        {
                            material.color = row.Color;
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
