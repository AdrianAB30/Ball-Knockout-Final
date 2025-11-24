using UnityEngine;
using DG.Tweening;
using System;

public static class AnimationsDotween
{
    public static void AnimateUIElement(
         RectTransform target,
         Vector2 targetPos,
         float targetScale,
         CarouselSettings settings,
         Action onComplete = null)
    {
        target.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Join(target.DOAnchorPos(targetPos, settings.animationDuration)
            .SetEase(settings.easeType));

        seq.Join(target.DOScale(Vector3.one * targetScale, settings.animationDuration)
            .SetEase(settings.easeType));

        if (onComplete != null)
        {
            seq.OnComplete(() => onComplete.Invoke());
        }
    }

    public static void PunchObject(Transform target)
    {
        target.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }
}