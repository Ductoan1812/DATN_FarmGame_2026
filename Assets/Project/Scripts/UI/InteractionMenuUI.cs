using System;
using UnityEngine;

/// <summary>
/// Legacy component kept only so old scenes do not lose the script reference.
/// NPC root options are now rendered inside DialoguePanelUI.
/// </summary>
[Obsolete("NPC interaction options are rendered by DialoguePanelUI. Remove this component from scenes when convenient.")]
public class InteractionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private void OnEnable()
    {
        if (panel != null)
            panel.SetActive(false);

        enabled = false;
    }
}
