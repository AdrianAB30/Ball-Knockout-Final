using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using UnityEngine.Events; 

public class FadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float transitionFadeDuration = 1.2f; 
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private Ease fadeEase = Ease.InOutSine;

    [Header("Simple Fade (Loading)")]
    [SerializeField] private float loadingFadeDuration = 0.3f; 

    public UnityEvent OnShowComplete;
    public UnityEvent OnHideComplete;

    [Header("Canvas References")]
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject mainMenuCanvas;

    private Coroutine m_FadeSequenceCoroutine;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f); 
            fadeImage.fillAmount = 1;
            fadeImage.raycastTarget = false; 
        }

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (loginCanvas != null)
            loginCanvas.SetActive(true);
    }
    public void StartFadeTransition()
    {
        if (m_FadeSequenceCoroutine != null)
        {
            StopCoroutine(m_FadeSequenceCoroutine);
        }
        m_FadeSequenceCoroutine = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        if (fadeImage == null) yield break;

        fadeImage.raycastTarget = true; 
        yield return fadeImage.DOFade(1f, transitionFadeDuration).SetEase(fadeEase).WaitForCompletion();

        yield return new WaitForSeconds(holdDuration);

        if (loginCanvas != null) loginCanvas.SetActive(false);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);

        yield return fadeImage.DOFade(0f, transitionFadeDuration).SetEase(fadeEase).WaitForCompletion();
        fadeImage.raycastTarget = false; 

        m_FadeSequenceCoroutine = null;
    }

    public void Show()
    {
        if (fadeImage == null) return;
        fadeImage.raycastTarget = true;
        fadeImage.DOFade(1f, loadingFadeDuration).OnComplete(() => OnShowComplete?.Invoke());
    }

    public void Hide()
    {
        if (fadeImage == null) return;
        fadeImage.DOFade(0f, loadingFadeDuration).OnComplete(() =>
        {
            fadeImage.raycastTarget = false;
            OnHideComplete?.Invoke();
        });
    }
}