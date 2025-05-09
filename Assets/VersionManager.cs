using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class VersionManager : MonoBehaviour
{
    [Header("Version Settings")]
    [Tooltip("Minimum allowed version")]
    [SerializeField] private string currentVersion = "0.1.5"; // format: major.minor.patch (0–99)
    [Tooltip("Maximum version if all features completed")]
    [SerializeField] private string nextVersion = "0.2.0";    // format: major.minor.patch (0–99)
    [Tooltip("Optional version tag to display (e.g. T1, dev)")]
    [SerializeField] private string versionTag = "";           // e.g. "T1", "dev"

    [Header("Features Checklist")]
    [Tooltip("Ticked features count towards version progress")]
    [SerializeField] private List<bool> features = new();

    [Header("UI")]
    [Tooltip("Displays computed version")]
    [SerializeField] private TextMeshProUGUI versionText;

    private string lastDisplay = "";

    private void Update() => Refresh();
    private void OnValidate() => Refresh();

    private void Refresh()
    {
        if (!TryParseVersion(currentVersion, out int curUnits)) return;
        if (!TryParseVersion(nextVersion, out int nextUnits)) return;

        int total = Mathf.Max(features.Count, 1);
        int completed = features.FindAll(f => f).Count;

        int versionRange = nextUnits - curUnits;
        int stepSize = Mathf.CeilToInt(versionRange / (float)total);

        int newUnits = Mathf.Clamp(curUnits + completed * stepSize, curUnits, nextUnits);
        string formatted = FormatVersion(newUnits);

        // Append version tag if provided
        if (!string.IsNullOrWhiteSpace(versionTag))
            formatted += $"-{versionTag}";

        if (formatted != lastDisplay)
        {
            lastDisplay = formatted;
            if (versionText != null)
                versionText.text = $"v{formatted}";
        }
    }

    private bool TryParseVersion(string version, out int units)
    {
        units = 0;
        var parts = version.Trim().Split('.');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out int major)) return false;
        if (!int.TryParse(parts[1], out int minor)) return false;
        if (!int.TryParse(parts[2], out int patch)) return false;

        if (patch < 0 || patch > 99) return false;

        // Encode as MAJOR*10000 + MINOR*100 + PATCH
        units = major * 10000 + minor * 100 + patch;
        return true;
    }

    private string FormatVersion(int units)
    {
        int major = units / 10000;
        int minor = (units / 100) % 100;
        int patch = units % 100;

        if (patch % 10 == 0)
            patch /= 10; // strip trailing zero in patch if it's even

        return $"{major}.{minor}.{patch}";
    }
}
