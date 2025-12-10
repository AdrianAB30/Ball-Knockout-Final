using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Referencias")]
    [SerializeField] private UIAnimationData uiAnimations;

    private Button myButton;
    private Vector3 originalScale;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (NotInteractable()) return;
        if (uiAnimations != null) uiAnimations.AnimateButtonPunch(gameObject, originalScale);
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (NotInteractable()) return;
        HighlightButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (NotInteractable()) return;
        UnhighlightButton();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (NotInteractable()) return;
        HighlightButton();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (NotInteractable()) return;
        UnhighlightButton();
    }

    private void HighlightButton()
    {
        if (uiAnimations != null) uiAnimations.AnimateHoverEnter(gameObject, originalScale);
        if (AudioManager.Instance) AudioManager.Instance.PlayHover();
    }

    private void UnhighlightButton()
    {
        if (uiAnimations != null) uiAnimations.AnimateHoverExit(gameObject, originalScale);
    }

    private bool NotInteractable()
    {
        return myButton != null && !myButton.interactable;
    }

    public void UpdateOriginalScale()
    {
        originalScale = transform.localScale;
    }
}