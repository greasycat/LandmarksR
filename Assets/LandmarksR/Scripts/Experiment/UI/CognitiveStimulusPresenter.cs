using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LandmarksR.Scripts.Experiment.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class CognitiveStimulusPresenter : MonoBehaviour
    {
        private static CognitiveStimulusPresenter _instance;
        private Color _centerTextColor = Color.white;

        private Canvas _canvas;
        private Image _background;
        private TextMeshProUGUI _fixationText;
        private TextMeshProUGUI _leftText;
        private TextMeshProUGUI _centerText;
        private TextMeshProUGUI _rightText;

        public static CognitiveStimulusPresenter GetOrCreate()
        {
            if (_instance != null)
            {
                _instance.EnsureCanvas();
                return _instance;
            }

            _instance = FindObjectOfType<CognitiveStimulusPresenter>();
            if (_instance != null)
            {
                _instance.EnsureCanvas();
                return _instance;
            }

            var gameObject = new GameObject("CognitiveStimulusPresenter", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            _instance = gameObject.AddComponent<CognitiveStimulusPresenter>();
            _instance.EnsureCanvas();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureCanvas();
            DontDestroyOnLoad(gameObject);
            ClearAll();
        }

        public void ShowFixation(string fixation)
        {
            EnsureCanvas();
            SetCanvasVisible(true);
            _fixationText.gameObject.SetActive(true);
            _fixationText.text = fixation;
            _leftText.gameObject.SetActive(false);
            _centerText.gameObject.SetActive(false);
            _rightText.gameObject.SetActive(false);
        }

        public void SetCenteredTextColor(Color color)
        {
            _centerTextColor = color;

            if (_centerText != null)
            {
                _centerText.color = _centerTextColor;
            }
        }

        public void ShowCenteredText(string content)
        {
            EnsureCanvas();
            SetCanvasVisible(true);
            _fixationText.gameObject.SetActive(false);
            _leftText.gameObject.SetActive(false);
            _rightText.gameObject.SetActive(false);
            _centerText.gameObject.SetActive(true);
            _centerText.text = content;
            _centerText.color = _centerTextColor;
        }

        public void ShowCenteredText(string content, Color color)
        {
            SetCenteredTextColor(color);
            ShowCenteredText(content);
        }

        public void ShowFlanker(string center, string flanker, Color centerColor, Color flankerColor, int flankCount = 2)
        {
            EnsureCanvas();
            SetCanvasVisible(true);
            _fixationText.gameObject.SetActive(false);
            _centerText.gameObject.SetActive(true);
            _leftText.gameObject.SetActive(true);
            _rightText.gameObject.SetActive(true);

            var flankers = string.Empty;
            for (var i = 0; i < flankCount; i++)
            {
                flankers += flanker;
            }

            _leftText.text = flankers;
            _rightText.text = flankers;
            _centerText.text = center;

            _leftText.color = flankerColor;
            _rightText.color = flankerColor;
            _centerText.color = centerColor;
        }

        public void ClearStimulus()
        {
            if (_canvas == null)
            {
                return;
            }

            _fixationText.gameObject.SetActive(false);
            _leftText.gameObject.SetActive(false);
            _centerText.gameObject.SetActive(false);
            _rightText.gameObject.SetActive(false);
        }

        public void ClearAll()
        {
            if (_canvas == null)
            {
                return;
            }

            ClearStimulus();
            SetCanvasVisible(false);
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var root = _canvas.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            _background = CreateBackground(root);
            _fixationText = CreateText(root, "FixationText", Vector2.zero, 96f, TextAlignmentOptions.Center);
            _leftText = CreateText(root, "LeftFlankerText", new Vector2(-280f, 0f), 92f, TextAlignmentOptions.Center);
            _centerText = CreateText(root, "CenterStimulusText", Vector2.zero, 120f, TextAlignmentOptions.Center);
            _rightText = CreateText(root, "RightFlankerText", new Vector2(280f, 0f), 92f, TextAlignmentOptions.Center);
            SetCenteredTextColor(_centerTextColor);
        }

        private static Image CreateBackground(RectTransform parent)
        {
            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(parent, false);

            var rectTransform = backgroundObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = backgroundObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.2f);
            return image;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string objectName, Vector2 anchoredPosition,
            float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(360f, 180f);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.enableWordWrapping = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }

        private void SetCanvasVisible(bool visible)
        {
            _canvas.enabled = visible;
            _background.enabled = visible;
        }
    }
}
