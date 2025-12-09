using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;

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
        countdownText.gameObject.SetActive(false);

        StartCoroutine(LateStartUI());
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
        var allPlayers = FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int teamId = -1;
        foreach (var h in allPlayers)
        {
            if (h.PlayerID == playerIndex)
            {
                teamId = h.TeamID;
                break;
            }
        }

        if (teamId == -1) return;

        Image[] hearts = (teamId == 1) ? p1Hearts : p2Hearts;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = (i < currentLives) ? Color.white : Color.black;
        }
    }
  
    private IEnumerator LateStartUI()
    {
        yield return new WaitForSeconds(0.5f);

        UpdateLives(1, 3);
        UpdateLives(2, 3);
    }
    private void RefreshAllLives()
    {
        var players = FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            // Pedimos al script Health que nos diga cuántas vidas tiene realmente
            // Necesitas exponer una propiedad pública en Health para leer 'NetLives.Value'
            // O simplemente confiamos en el UpdateLives(1, 3) por ahora.
        }
    }
}