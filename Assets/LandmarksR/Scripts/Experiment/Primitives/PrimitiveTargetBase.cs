using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public abstract class PrimitiveTargetBase : MonoBehaviour
    {
        [SerializeField] private string targetId;

        public string TargetId => string.IsNullOrWhiteSpace(targetId) ? gameObject.name : targetId.Trim();

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                targetId = gameObject.name;
            }
        }
    }

    public class PrimitiveTarget2D : PrimitiveTargetBase
    {
        [SerializeField] private RectTransform targetTransform;
        [SerializeField] private Graphic[] graphics;

        public RectTransform TargetTransform => targetTransform ? targetTransform : GetComponent<RectTransform>();

        public IReadOnlyList<Graphic> Graphics
        {
            get
            {
                if (graphics == null || graphics.Length == 0)
                {
                    graphics = GetComponentsInChildren<Graphic>(true);
                }

                return graphics;
            }
        }
    }

    public class PrimitiveTarget3D : PrimitiveTargetBase
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Renderer[] renderers;

        public Transform TargetTransform => targetTransform ? targetTransform : transform;

        public IReadOnlyList<Renderer> Renderers
        {
            get
            {
                if (renderers == null || renderers.Length == 0)
                {
                    renderers = GetComponentsInChildren<Renderer>(true);
                }

                return renderers;
            }
        }
    }
}
