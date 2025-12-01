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

    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;

    [SerializeField] private Image[] p1Hearts;
    [SerializeField] private Image[] p2Hearts;
    private Vector3 _leftPanelPos;
    private Vector3 _rightPanelPos;

    private void Awake()
    {
        if (p1HeartContainer) _leftPanelPos = p1HeartContainer.transform.position;
        if (p2HeartContainer) _rightPanelPos = p2HeartContainer.transform.position;
    }
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

        Health.OnLivesChanged += UpdateLives;

        Invoke(nameof(AdjustUILayout), 0.5f);

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
    private void AdjustUILayout()
    {
        int localTeamId = 1; 

        var players = FindObjectsByType<PlayerVisuals>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && p.IsOwner)
            {
                localTeamId = p.NetTeamId.Value;
                break;
            }
            if (gameConfig.CurrentGameMode == GameModeType.LocalSplitScreen && p.PlayerID == 1)
            {
                return;
            }
        }

        if (localTeamId == 2)
        {
            p1HeartContainer.transform.position = _rightPanelPos;
            p2HeartContainer.transform.position = _leftPanelPos;
        }
    }
}