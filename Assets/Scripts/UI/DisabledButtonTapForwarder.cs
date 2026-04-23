using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisabledButtonTapForwarder : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Button targetButton;
    [SerializeField] private MainMenuManager mainMenuManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (targetButton == null || targetButton.interactable)
        {
            return;
        }

        mainMenuManager?.OnDisabledContinueTapped();
    }
}
