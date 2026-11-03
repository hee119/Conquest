using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>Modal confirmation for the lobby's application quit action.</summary>
public sealed class QuitConfirmationDialog : MonoBehaviour
{
    private GameObject previousSelection;
    [SerializeField] private UnityEngine.UI.Button noButton;
    [SerializeField] private UnityEngine.UI.Button yesButton;

    public void Show()
    {
        if (gameObject.activeSelf) return;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        gameObject.SetActive(true);
        noButton.Select();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    public void Confirm()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
