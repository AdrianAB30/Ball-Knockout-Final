using UnityEngine;
using UnityEngine.UI;

public class LightFlicker : MonoBehaviour
{
    private Image _img;
    private float _baseAlpha;
    [SerializeField] private float minFlicker;
    [SerializeField] private float maxFlicker;

    void Awake() { _img = GetComponent<Image>(); _baseAlpha = _img.color.a; }

    void Update()
    {
        float flicker = Random.Range(minFlicker, maxFlicker);
        Color c = _img.color;
        c.a = Mathf.Clamp01(_baseAlpha + flicker);
        _img.color = c;
    }
}