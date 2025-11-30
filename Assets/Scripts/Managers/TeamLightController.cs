using UnityEngine;
using DG.Tweening;

public class TeamLightController : MonoBehaviour
{
    [SerializeField] private LightAnimSettings settings;
    private Vector3 _startPos;

    private void Awake()
    {
        _startPos = transform.position;
        transform.localScale = transform.localScale;
        gameObject.SetActive(false);
    }

    public void MoveToTarget(Transform target)
    {
        gameObject.SetActive(true);

        transform.DOKill();

        transform.DOMove(target.position, settings.moveDuration)
            .SetEase(settings.moveEase);
    }

    public void ReturnToStart()
    {
        if (!gameObject.activeSelf) return;

        transform.DOKill();

        transform.DOMove(_startPos, settings.exitDuration)
            .SetEase(settings.exitEase)
            .OnComplete(() => gameObject.SetActive(true));
    }
}