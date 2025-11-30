using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

[CreateAssetMenu(fileName = "UIAnimations", menuName = "ScriptableObjects/UI Animation Data", order = 3)]
public class UIAnimationData : ScriptableObject
{
    [Header("Click (Punch) Settings")]
    [Range(0f, 1f)] public float punchStrength = 0.2f; 
    public float punchDuration = 0.2f;
    public int vibrato = 10;
    public float elasticity = 1;

    [Header("Hover (Scale) Settings")]
    public float hoverScaleFactor = 1.1f; 
    public float hoverDuration = 0.1f;

    [Header("Text Pop Settings")]
    public float textScaleFactor = 1.2f;
    public float textDuration = 0.15f;

    [Header("Panel Open Settings")]
    public float panelOpenDuration = 0.4f;
    public Ease panelOpenEase = Ease.OutBack;
    public float startScaleMultiplier = 0.5f;

    [Header("Panel Close Settings")] 
    public float panelCloseDuration = 0.3f; 
    public Ease panelCloseEase = Ease.InBack;


    public void AnimateButtonPunch(GameObject target, Vector3 originalScale)
    {
        if (target == null) return;
        target.transform.DOKill(true);

        target.transform.localScale = originalScale;

        Vector3 finalPunch = originalScale * punchStrength;

        target.transform.DOPunchScale(finalPunch, punchDuration, vibrato, elasticity);
    }

    public void AnimateHoverEnter(GameObject target, Vector3 originalScale)
    {
        if (target == null) return;
        target.transform.DOKill(true);

        target.transform.DOScale(originalScale * hoverScaleFactor, hoverDuration);
    }

    public void AnimateHoverExit(GameObject target, Vector3 originalScale)
    {
        if (target == null) return;
        target.transform.DOKill(true);
        target.transform.DOScale(originalScale, hoverDuration);
    }

    public void AnimateTextPop(GameObject targetText)
    {
        if (targetText == null) return;

        targetText.transform.DOKill(true);
        targetText.transform.localScale = Vector3.one;

        targetText.transform.DOScale(Vector3.one * textScaleFactor, textDuration)
            .SetLoops(2, LoopType.Yoyo);
    }
    public void AnimatePanelOpen(GameObject panel, Vector3 finalScale)
    {
        if (panel == null) return;

        panel.transform.DOKill(true);

        panel.transform.localScale = finalScale * startScaleMultiplier;

        panel.transform.DOScale(finalScale, panelOpenDuration).SetEase(panelOpenEase);
    }
    public void AnimatePanelClose(GameObject panel, Action onComplete)
    {
        if (panel == null) return;

        panel.transform.DOKill(true);

        panel.transform.DOScale(Vector3.zero, panelCloseDuration)
            .SetEase(panelCloseEase)
            .OnComplete(() =>
            {
                onComplete?.Invoke();

                // Opcional: Restaurar escala para la próxima vez (aunque el Open ya lo maneja)
                // panel.transform.localScale = Vector3.one; 
            });
    }
}