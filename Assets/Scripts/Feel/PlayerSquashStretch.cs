using UnityEngine;
using DG.Tweening;

public class PlayerSquashStretch : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float stretchAmount = 1.3f;
    [SerializeField] private float squashAmount = 0.7f;
    [SerializeField] private float duration = 0.2f;

    private Vector3 _originalScale;

    private void Awake()
    {
        if (spriteTransform == null) spriteTransform = transform.GetChild(0);
        _originalScale = spriteTransform.localScale;
    }

    public void TriggerSquashAndStretch(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        spriteTransform.DOKill();
        spriteTransform.localScale = _originalScale;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        spriteTransform.rotation = Quaternion.Euler(0, 0, angle - 90);

        Sequence seq = DOTween.Sequence();
        seq.Append(spriteTransform.DOScale(new Vector3(squashAmount, stretchAmount, 1f), duration / 2).SetEase(Ease.OutQuad));
        seq.Append(spriteTransform.DOScale(_originalScale, duration / 2).SetEase(Ease.OutElastic));
    }
}