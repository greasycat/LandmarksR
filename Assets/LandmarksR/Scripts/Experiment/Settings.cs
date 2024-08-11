using System;
using LandmarksR.Scripts.Attributes;
using LandmarksR.Scripts.Experiment.Log;
using LandmarksR.Scripts.Player;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace LandmarksR.Scripts.Experiment
{
    public static class ExperimentMetadataColumns
    {
        public const string SubjectId = "subject_id";
        public const string RunSessionId = "run_session_id";
    }

    /// <summary>
    /// Represents the settings for the experiment.
    /// </summary>
    [Serializable]
    public class ExperimentSettings
    {
        private const string LegacyDefaultParticipantId = "default_participant_0";
        private const string LegacyFallbackParticipantId = "default_participant_id";

        /// <summary>
        /// The subject ID for the experiment.
        /// </summary>
        [FormerlySerializedAs("participantId")]
        public string subjectId;

        /// <summary>
        /// The unique identifier for the current run session.
        /// </summary>
        [NotEditable] public string runSessionId;

        public void SetSubjectId(string value)
        {
            subjectId = NormalizeSubjectId(value);
        }

        public string GetSubjectIdOrDefault(string fallback = "unassigned_subject")
        {
            return string.IsNullOrWhiteSpace(subjectId) ? fallback : subjectId.Trim();
        }

        public string GetRunSessionIdOrCreate()
        {
            if (string.IsNullOrWhiteSpace(runSessionId))
            {
                StartNewRunSession();
            }

            return runSessionId;
        }

        public void StartNewRunSession()
        {
            runSessionId = Guid.NewGuid().ToString();

            if (IsPlaceholderSubjectId(subjectId))
            {
                subjectId = string.Empty;
                return;
            }

            subjectId = NormalizeSubjectId(subjectId);
        }

        private static string NormalizeSubjectId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsPlaceholderSubjectId(string value)
        {
            var normalized = NormalizeSubjectId(value);
            return string.Equals(normalized, LegacyDefaultParticipantId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, LegacyFallbackParticipantId, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Represents the display settings for the experiment.
    /// </summary>
    [Serializable]
    public class DisplaySettings
    {
        /// <summary>
        /// The display mode (e.g., Desktop, VR).
        /// </summary>
        public DisplayMode displayMode = DisplayMode.Desktop;

        /// <summary>
        /// The HUD mode (e.g., Follow, Fixed).
        /// </summary>
        public HudMode hudMode = HudMode.Follow;

        /// <summary>
        /// The distance of the HUD from the player.
        /// </summary>
        public float hudDistance = 1.5f;

        /// <summary>
        /// The screen size of the HUD.
        /// </summary>
        public Vector2 hudScreenSize = new Vector2(1920f, 1080f);
    }

    /// <summary>
    /// Represents the interaction settings for the experiment.
    /// </summary>
    [Serializable]
    public class InteractionSettings
    {
        /// <summary>
        /// The thickness of the HUD collider.
        /// </summary>
        public float hudColliderThickness = 0.05f;
    }

    /// <summary>
    /// Represents the calibration settings for the experiment.
    /// </summary>
    [Serializable]
    public class CalibrationSettings
    {
        /// <summary>
        /// The height of the controller in centimeters.
        /// </summary>
        public float controllerHeight = 0.15f;
    }

    /// <summary>
    /// Represents the space settings for the experiment.
    /// </summary>
    [Serializable]
    public class SpaceSettings
    {
        /// <summary>
        /// Indicates whether the space is calibrated.
        /// </summary>
        [NotEditable] public bool calibrated;

        /// <summary>
        /// The Y coordinate of the ground.
        /// </summary>
        [NotEditable] public float groundY;

        /// <summary>
        /// The position of the left top corner of the space.
        /// </summary>
        [NotEditable] public Vector3 leftTop;

        /// <summary>
        /// The position of the right top corner of the space.
        /// </summary>
        [NotEditable] public Vector3 rightTop;

        /// <summary>
        /// The position of the left bottom corner of the space.
        /// </summary>
        [NotEditable] public Vector3 leftBottom;

        /// <summary>
        /// The position of the right bottom corner of the space.
        /// </summary>
        [NotEditable] public Vector3 rightBottom;

        /// <summary>
        /// The center position of the space.
        /// </summary>
        [NotEditable] public Vector3 center;

        /// <summary>
        /// The forward direction of the space.
        /// </summary>
        [NotEditable] public Vector3 forward;

        /// <summary>
        /// Calibrates the space by computing the center and forward direction.
        /// </summary>
        public void CalibrateSpace()
        {
            ComputeCenter();
            ComputeForward();
            calibrated = true;
        }

        /// <summary>
        /// Computes the center of the space.
        /// </summary>
        private void ComputeCenter()
        {
            // Average x and z of the corners
            var x = (leftTop.x + rightTop.x + leftBottom.x + rightBottom.x) / 4;
            var z = (leftTop.z + rightTop.z + leftBottom.z + rightBottom.z) / 4;
            center = new Vector3(x, groundY, z);
        }

        /// <summary>
        /// Computes the forward direction of the space.
        /// </summary>
        private void ComputeForward()
        {
            // Use the leftTop and rightTop to compute the forward vector
            var leftTopTemp = new Vector3(leftTop.x, groundY, leftTop.z);
            var rightTopTemp = new Vector3(rightTop.x, groundY, rightTop.z);
            var vec1 = leftTopTemp - center;
            var vec2 = rightTopTemp - center;
            forward = (vec1.normalized + vec2.normalized).normalized;
        }

        /// <summary>
        /// Applies the space settings to the environment.
        /// </summary>
        public void ApplyToEnvironment()
        {
            // Apply the settings to the environment
            var environment = GameObject.FindGameObjectWithTag("Environment");
            Assert.IsNotNull(environment, "Can't find environment object, please add a GameObject with the tag 'Environment'");

            var calibrationSpace = GameObject.FindGameObjectWithTag("Calibration");
            Assert.IsNotNull(calibrationSpace, "Can't find calibration object, please add a GameObject with the tag 'Calibration'");

            var environmentTransform = environment.transform;
            environmentTransform.position = center;
            environmentTransform.forward = forward;

            var calibrationSpaceTransform = calibrationSpace.transform;
            calibrationSpaceTransform.position = center;
            calibrationSpaceTransform.forward = forward;
        }
    }

    /// <summary>
    /// Represents the logging settings for the experiment.
    /// </summary>
    [Serializable]
    public class LoggingSettings
    {
        /// <summary>
        /// Indicates whether local logging is enabled.
        /// </summary>
        public bool localLogging;

        /// <summary>
        /// Indicates whether remote logging is enabled.
        /// </summary>
        public bool remoteLogging;

        /// <summary>
        /// The URL for the remote logging status.
        /// </summary>
        public string remoteStatusUrl;

        /// <summary>
        /// The URL for remote logging.
        /// </summary>
        public string remoteLogUrl;

        /// <summary>
        /// The interval for logging data in milliseconds.
        /// </summary>
        public float loggingIntervalInMillisecond;

        /// <summary>
        /// Legacy delimiter preference for tabular exports.
        /// </summary>
        public string dataFileDelimiter = ",";

        /// <summary>
        /// Legacy primary tabular extension.
        /// </summary>
        public string dataFileExtension = "csv";

        /// <summary>
        /// The extension used for canonical JSON line files.
        /// </summary>
        public string jsonFileExtension = StructuredLoggingDefaults.JsonLineExtension;

        /// <summary>
        /// The stable base file name for the event log.
        /// </summary>
        public string eventFileName = StructuredLoggingDefaults.EventBaseFileName;

        /// <summary>
        /// Indicates whether TSV exports are enabled.
        /// </summary>
        public bool exportTsv = true;

        /// <summary>
        /// Indicates whether CSV exports are enabled.
        /// </summary>
        public bool exportCsv = true;

        public void ApplyDefaults()
        {
            if (string.IsNullOrWhiteSpace(dataFileDelimiter))
            {
                dataFileDelimiter = ",";
            }

            if (string.IsNullOrWhiteSpace(dataFileExtension))
            {
                dataFileExtension = "csv";
            }

            if (string.IsNullOrWhiteSpace(jsonFileExtension))
            {
                jsonFileExtension = StructuredLoggingDefaults.JsonLineExtension;
            }

            if (string.IsNullOrWhiteSpace(eventFileName))
            {
                eventFileName = StructuredLoggingDefaults.EventBaseFileName;
            }

            if (!exportTsv && !exportCsv)
            {
                exportTsv = true;
                exportCsv = true;
            }
        }

        public string GetJsonFileExtension()
        {
            ApplyDefaults();
            return NormalizeExtension(jsonFileExtension, StructuredLoggingDefaults.JsonLineExtension);
        }

        public string GetEventBaseFileName()
        {
            ApplyDefaults();
            var normalized = eventFileName.Trim();
            var extensionSeparator = normalized.LastIndexOf('.');
            if (extensionSeparator > 0)
            {
                normalized = normalized.Substring(0, extensionSeparator);
            }

            return string.IsNullOrWhiteSpace(normalized)
                ? StructuredLoggingDefaults.EventBaseFileName
                : normalized;
        }

        public bool ShouldExportTsv()
        {
            ApplyDefaults();
            return exportTsv;
        }

        public bool ShouldExportCsv()
        {
            ApplyDefaults();
            return exportCsv;
        }

        private static string NormalizeExtension(string value, string fallback)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            normalized = normalized.TrimStart('.');
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }
    }

    /// <summary>
    /// Represents the UI settings for the experiment.
    /// </summary>
    [Serializable]
    public class UISettings
    {
        /// <summary>
        /// The time in seconds for triggering calibration.
        /// </summary>
        public float calibrationTriggerTime = 1.25f;
    }

    /// <summary>
    /// Manages the settings for the experiment, ensuring there is only one instance.
    /// </summary>
    public class Settings : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the Settings class.
        /// </summary>
        public static Settings Instance => _instance ??= BuildConfig();
        private static Settings _instance;

        /// <summary>
        /// Builds the singleton instance of the Settings class.
        /// </summary>
        /// <returns>A new instance of the Settings class.</returns>
        private static Settings BuildConfig()
        {
            var settings = new GameObject("Settings").AddComponent<Settings>();
            settings.experiment.subjectId = string.Empty;
            settings.experiment.runSessionId = string.Empty;
            settings.logging.ApplyDefaults();
            return settings;
        }

        /// <summary>
        /// Unity Awake method. Ensures there is only one instance of the Settings class.
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this;
                logging.ApplyDefaults();
                SwitchDisplayMode(defaultDisplayMode);
                DontDestroyOnLoad(this);
            }
        }

        /// <summary>
        /// Switches the display mode based on the provided mode.
        /// </summary>
        /// <param name="displayMode">The display mode to switch to.</param>
        private void SwitchDisplayMode(DisplayMode displayMode)
        {
            displayReference = displayMode switch
            {
                DisplayMode.Desktop => desktopDisplay,
                DisplayMode.VR => vrDisplay,
                _ => throw new Exception("Invalid Display Mode")
            };
        }

        /// <summary>
        /// The experiment settings.
        /// </summary>
        public ExperimentSettings experiment = new()
        {
            subjectId = string.Empty,
            runSessionId = string.Empty
        };

        /// <summary>
        /// The default display mode.
        /// </summary>
        public DisplayMode defaultDisplayMode = DisplayMode.VR;

        /// <summary>
        /// The VR display settings.
        /// </summary>
        public DisplaySettings vrDisplay = new()
        {
            displayMode = DisplayMode.VR,
            hudMode = HudMode.Fixed,
            hudDistance = 3f,
            hudScreenSize = new Vector2(1920f, 1080f)
        };

        /// <summary>
        /// The desktop display settings.
        /// </summary>
        public DisplaySettings desktopDisplay = new()
        {
            displayMode = DisplayMode.Desktop,
            hudMode = HudMode.Follow,
            hudDistance = 1.5f,
            hudScreenSize = new Vector2(1920f, 1080f)
        };

        /// <summary>
        /// The reference to the current display settings.
        /// </summary>
        public DisplaySettings displayReference = new();

        /// <summary>
        /// The interaction settings.
        /// </summary>
        public InteractionSettings interaction = new();

        /// <summary>
        /// The space settings.
        /// </summary>
        public SpaceSettings space = new();

        /// <summary>
        /// The calibration settings.
        /// </summary>


 public CalibrationSettings calibration = new();

        /// <summary>
        /// The logging settings.
        /// </summary>
        public LoggingSettings logging = new()
        {
            localLogging = true,
            remoteLogging = false,
            remoteStatusUrl = "http://127.0.0.1:8000/healthz",
            remoteLogUrl = "http://127.0.0.1:8000/api/v1/records",
            loggingIntervalInMillisecond = 200f,
            dataFileDelimiter = ",",
            dataFileExtension = "csv",
            jsonFileExtension = StructuredLoggingDefaults.JsonLineExtension,
            eventFileName = StructuredLoggingDefaults.EventBaseFileName,
            exportTsv = true,
            exportCsv = true
        };

        /// <summary>
        /// The UI settings.
        /// </summary>
        public UISettings ui = new();
    }
}
