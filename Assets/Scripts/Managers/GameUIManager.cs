using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject p1HeartContainer;
    [SerializeField] private GameObject p2HeartContainer;

    [SerializeField] private Image[] p1Hearts;
    [SerializeField] private Image[] p2Hearts;

    private void OnEnable()
    {
        Health.OnLivesChanged += UpdateLives;
    }
    private void OnDisable()
    {
        Health.OnLivesChanged -= UpdateLives;
    }
    private void Start()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnCountdownStart += ShowCountdown;
            GameModeManager.Instance.OnRoundStart += ShowGo;
        }


        UpdateLives(1, 3);
        UpdateLives(2, 3);

        countdownText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnCountdownStart -= ShowCountdown;
            GameModeManager.Instance.OnRoundStart -= ShowGo;
        }

    }

    private void ShowCountdown(int number)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = number.ToString();

        countdownText.transform.localScale = Vector3.zero;
        countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }

    private void ShowGo()
    {
        countdownText.text = "LET'S PLAY!";
        countdownText.transform.localScale = Vector3.zero;
        countdownText.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutElastic);

        Invoke(nameof(HideCountdown), 1f);
    }

    private void HideCountdown()
    {
        countdownText.gameObject.SetActive(false);
    }

    public void UpdateLives(int playerIndex, int currentLives)
    {
        UnityEngine.UI.Image[] hearts = (playerIndex == 1) ? p1Hearts : p2Hearts;

        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = (i < currentLives) ? Color.white : Color.black; 
        }
    }
}