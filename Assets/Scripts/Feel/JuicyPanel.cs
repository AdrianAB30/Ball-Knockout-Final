using UnityEngine;

public class JuicyPanel : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private UIAnimationData uiAnimations;

    private Vector3 originalScale;
    private bool initialized = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        initialized = true;

        if (uiAnimations == null) uiAnimations = Resources.Load<UIAnimationData>("UI/GlobalUIStyle");
    }

    private void OnEnable()
    {
        if (uiAnimations != null)
        {
            if (!initialized)
            {
                originalScale = transform.localScale;
                initialized = true;
            }

            uiAnimations.AnimatePanelOpen(gameObject, originalScale);
        }
    }
    public void ClosePanel()
    {
        if (uiAnimations != null)
        {
            uiAnimations.AnimatePanelClose(gameObject, () =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}