using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    public TextMeshProUGUI plT;

    void Update()
    {
        plT.text = $"PL: {PlayerManager.Instance.players.Count}";
    }
}
