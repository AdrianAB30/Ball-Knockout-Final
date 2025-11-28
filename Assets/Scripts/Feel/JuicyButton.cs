using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
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

    private void Start()
    {
        if (uiAnimations == null) uiAnimations = Resources.Load<UIAnimationData>("UI/GlobalUIStyle");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (NotInteractable()) return;
        if (uiAnimations != null) uiAnimations.AnimateButtonPunch(gameObject, originalScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (NotInteractable()) return;
        if (uiAnimations != null) uiAnimations.AnimateHoverEnter(gameObject, originalScale);
        if (AudioManager.Instance) AudioManager.Instance.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (NotInteractable()) return;
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