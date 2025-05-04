using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class VersionManager : MonoBehaviour
{
    [Header("Version Settings")]
    [SerializeField] private string currentVersion = "0.1.5";
    [SerializeField] private string nextVersion    = "0.2.0";

    [Header("Checklist (plain bools)")]
    [SerializeField] private List<bool> features = new();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI versionText;

    /* ──────────────────────────────── */
    private void Start()      => Refresh();
    private void OnValidate() => Refresh();

    /* ──────────────────────────────── */
    private void Refresh()
    {
        if (!TryParse(currentVersion, out int maj, out int min, out int pat) ||
            !TryParse(nextVersion,    out int tgtMaj, out int tgtMin, out int tgtPat))
        {
            versionText.text = $"v{currentVersion}";
            return;
        }

        int total = Mathf.Max(1, features.Count);
        int done  = features.FindAll(f => f).Count;

        // encode units: MAJOR*100 + MINOR*10 + PATCH (each unit = 0.0.1)
        int curUnits = maj * 100 + min * 10 + pat;
        int tgtUnits = tgtMaj * 100 + tgtMin * 10 + tgtPat;

        if (tgtUnits <= curUnits)
        {
            versionText.text = $"v{currentVersion}";
            return;
        }

        int distUnits  = tgtUnits - curUnits;
        int stepUnits  = Mathf.CeilToInt(distUnits / (float)total);
        int newUnits   = Mathf.Clamp(curUnits + stepUnits * done, curUnits, tgtUnits);

        // decode back
        int newMajor =  newUnits / 100;
        int newMinor = (newUnits / 10) % 10;
        int newPatch =  newUnits % 10;

        versionText.text = $"v{newMajor}.{newMinor}.{newPatch}";
    }

    /* ──────────────────────────────── */
    private bool TryParse(string v, out int maj, out int min, out int pat)
    {
        maj = min = pat = 0;
        var seg = v.Trim().Split('.');
        return seg.Length == 3 &&
               int.TryParse(seg[0], out maj) &&
               int.TryParse(seg[1], out min) &&
               int.TryParse(seg[2], out pat);
    }
}
