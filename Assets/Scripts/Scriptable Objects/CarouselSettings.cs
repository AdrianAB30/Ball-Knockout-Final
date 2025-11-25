using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "CarouselSettings", menuName = "ScriptableObjects/Carousel Settings", order = 1)]
public class CarouselSettings : ScriptableObject
{
    [Header("Tiempos y Curvas")]
    public float animationDuration = 0.3f;
    public Ease easeType = Ease.OutBack; 

    [Header("Escalas")]
    public float centerScale = 1.0f;
    public float sideScale = 0.7f;
}