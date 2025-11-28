using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "LightAnimSettings", menuName = "ScriptableObjects/LightAnimSettings", order = 6)]
public class LightAnimSettings : ScriptableObject
{
    [Header("Movimiento Entrada")]
    public float moveDuration = 0.5f;
    public Ease moveEase = Ease.InExpo;

    [Header("Movimiento Salida")]
    public float exitDuration = 0.5f;
    public Ease exitEase = Ease.OutExpo;

}

