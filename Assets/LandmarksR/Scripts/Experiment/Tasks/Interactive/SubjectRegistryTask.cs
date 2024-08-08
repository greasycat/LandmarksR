using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Interactive
{
    /// <summary>
    /// Captures the subject ID at the beginning of a run.
    /// </summary>
    public class SubjectRegistryTask : BaseTask
    {
        [SerializeField] private string title = "Subject Registry";

        [TextArea(3, 10)]
        [SerializeField] private string instructions =
            "Type the subject ID with the keyboard, then press Enter to continue.";

        [SerializeField] private string promptLabel = "Subject ID";
        [SerializeField] private string confirmLabel = "Press Enter";
        [SerializeField] private bool showRunSessionId = true;
        [SerializeField] private TextAlignmentOptions textAlignmentOptions = TextAlignmentOptions.TopLeft;
        [SerializeField] private float opacity = 1f;
        [SerializeField] private int maxSubjectIdLength = 64;
        [SerializeField] private List<string> layersToHide = new();

        private readonly StringBuilder _buffer = new();
        private string _validationMessage = string.Empty;

        protected override void Prepare()
        {
            SetTaskType(TaskType.Interactive);
            base.Prepare();

            _buffer.Clear();
            _validationMessage = string.Empty;

            var currentSubjectId = Settings.experiment.subjectId;
            if (!string.IsNullOrWhiteSpace(currentSubjectId))
            {
                _buffer.Append(currentSubjectId.Trim());
            }

            HUD.ShowAll()
                .ShowButton()
                .SetButtonText(confirmLabel)
                .SetOpacity(opacity)
                .HideLayers(layersToHide)
                .SetContentAlignment(textAlignmentOptions);

            RefreshHud();
            PlayerEvent.RegisterConfirmHandler(OnConfirm);
        }

        private void Update()
        {
            if (!IsTaskRunning())
            {
                return;
            }

            var input = Input.inputString;
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            var changed = false;
            foreach (var character in input)
            {
                if (character == '\b')
                {
                    if (_buffer.Length == 0)
                    {
                        continue;
                    }

                    _buffer.Length--;
                    changed = true;
                    continue;
                }

                if (character == '\n' || character == '\r' || char.IsControl(character))
                {
                    continue;
                }

                if (_buffer.Length >= maxSubjectIdLength)
                {
                    continue;
                }

                _buffer.Append(character);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            _validationMessage = string.Empty;
            RefreshHud();
        }

        public override void Finish()
        {
            base.Finish();
            PlayerEvent.UnregisterConfirmHandler(OnConfirm);
            HUD.ClearAllText().HideButton();
        }

        private void OnConfirm()
        {
            var subjectId = _buffer.ToString().Trim();
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                _validationMessage = "Subject ID is required.";
                RefreshHud();
                return;
            }

            Settings.experiment.SetSubjectId(subjectId);
            Logger.I("subject", $"SubjectRegistered:{Settings.experiment.subjectId}");
            StopCurrentTask();
        }

        private void RefreshHud()
        {
            var lines = new List<string>
            {
                instructions,
                string.Empty,
                $"{promptLabel}: {GetDisplayBuffer()}"
            };

            if (showRunSessionId)
            {
                lines.Add($"Run Session: {Settings.experiment.GetRunSessionIdOrCreate()}");
            }

            lines.Add(string.Empty);
            lines.Add(confirmLabel);

            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                lines.Add(string.Empty);
                lines.Add($"<color=#ff6b6b>{_validationMessage}</color>");
            }

            HUD.SetTitle(title);
            HUD.SetContent(string.Join("\n", lines));
        }

        private string GetDisplayBuffer()
        {
            return _buffer.Length == 0 ? "|" : $"{_buffer}|";
        }
    }
}
