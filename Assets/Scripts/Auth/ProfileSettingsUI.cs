using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class ProfileSettingsUI : MonoBehaviour
{
    [Header("Configuración (Scriptable Object)")]
    [SerializeField] private CarouselSettings animSettings;

    [Header("Botones y Paneles")]
    [SerializeField] private GameObject profileSettings;
    [SerializeField] private GameObject buttonsMenu;

    [Header("Panel Derecho (Nombre)")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button randomNameButton;

    [Header("Panel Izquierdo (Datos Cloud Save)")]
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_InputField birthdayInput;
    [SerializeField] private TMP_InputField statusInput;

    [Header("Avatar Carrusel")]
    [SerializeField] private Image avatarDisplayCenter;
    [SerializeField] private Image avatarDisplayPrev;
    [SerializeField] private Image avatarDisplayNext;
    [SerializeField] private Button nextAvatarButton;
    [SerializeField] private Button prevAvatarButton;
    [SerializeField] private List<Sprite> avatarBank;

    [Header("Guardar")]
    [SerializeField] private Button saveButton;

    private int _currentAvatarIndex = 0;
    private bool _isAnimating = false;

    private Vector2 _posCenter;
    private Vector2 _posPrev;
    private Vector2 _posNext;

    private void Start()
    {
        if (saveButton) saveButton.onClick.AddListener(OnSaveClicked);
        if (randomNameButton) randomNameButton.onClick.AddListener(OnRandomNameClicked);

        if (nextAvatarButton) nextAvatarButton.onClick.AddListener(() => RequestAvatarChange(1));
        if (prevAvatarButton) prevAvatarButton.onClick.AddListener(() => RequestAvatarChange(-1));

        _posCenter = avatarDisplayCenter.rectTransform.anchoredPosition;
        _posPrev = avatarDisplayPrev.rectTransform.anchoredPosition;
        _posNext = avatarDisplayNext.rectTransform.anchoredPosition;

        LoadDataFromManager();
    }

    private void LoadDataFromManager()
    {
        if (PlayerAccountManager.Instance == null) return;

        nameInput.text = PlayerAccountManager.Instance.PlayerName;
        UserProfileData profile = PlayerAccountManager.Instance.CurrentProfile;

        if (profile != null)
        {
            descriptionInput.text = profile.description;
            birthdayInput.text = profile.birthday;
            statusInput.text = profile.status;
            _currentAvatarIndex = profile.avatarIndex;
        }

        UpdateVisualsImmediate();
    }

    private void UpdateVisualsImmediate()
    {
        if (avatarBank == null || avatarBank.Count == 0) return;

        // Matar animaciones
        avatarDisplayCenter.rectTransform.DOKill();
        avatarDisplayPrev.rectTransform.DOKill();
        avatarDisplayNext.rectTransform.DOKill();

        _currentAvatarIndex = Mathf.Clamp(_currentAvatarIndex, 0, avatarBank.Count - 1);
        SetSpritesForIndex(_currentAvatarIndex);

        float centerS = animSettings != null ? animSettings.centerScale : 1f;
        float sideS = animSettings != null ? animSettings.sideScale : 0.7f;

        avatarDisplayCenter.rectTransform.anchoredPosition = _posCenter;
        avatarDisplayCenter.rectTransform.localScale = Vector3.one * centerS;

        avatarDisplayPrev.rectTransform.anchoredPosition = _posPrev;
        avatarDisplayPrev.rectTransform.localScale = Vector3.one * sideS;

        avatarDisplayNext.rectTransform.anchoredPosition = _posNext;
        avatarDisplayNext.rectTransform.localScale = Vector3.one * sideS;

        ToggleOutline(avatarDisplayCenter, true);
        ToggleOutline(avatarDisplayPrev, false);
        ToggleOutline(avatarDisplayNext, false);

      
        avatarDisplayPrev.transform.SetAsFirstSibling(); 
        avatarDisplayNext.transform.SetAsFirstSibling();
        avatarDisplayCenter.transform.SetAsLastSibling(); 
    }

    private void RequestAvatarChange(int direction)
    {
        if (_isAnimating || avatarBank.Count <= 1 || animSettings == null) return;
        _isAnimating = true;

        int targetIndex = GetCircularIndex(_currentAvatarIndex + direction);

        Image imageEnteringCenter;
        Image imageLeavingCenter;

        if (direction > 0) 
        {
            imageEnteringCenter = avatarDisplayNext;
            imageLeavingCenter = avatarDisplayCenter;

            imageEnteringCenter.sprite = avatarBank[targetIndex];

            imageEnteringCenter.transform.SetAsLastSibling();

            AnimationsDotween.AnimateUIElement(imageLeavingCenter.rectTransform, _posPrev, animSettings.sideScale, animSettings);
        }
        else 
        {
            imageEnteringCenter = avatarDisplayPrev;
            imageLeavingCenter = avatarDisplayCenter;

            imageEnteringCenter.sprite = avatarBank[targetIndex];

            imageEnteringCenter.transform.SetAsLastSibling();

            AnimationsDotween.AnimateUIElement(imageLeavingCenter.rectTransform, _posNext, animSettings.sideScale, animSettings);
        }

        ToggleOutline(imageEnteringCenter, true);
        ToggleOutline(imageLeavingCenter, false);

        AnimationsDotween.AnimateUIElement(
            imageEnteringCenter.rectTransform,
            _posCenter,
            animSettings.centerScale,
            animSettings,
            () =>
            {
                _currentAvatarIndex = targetIndex;
                UpdateVisualsImmediate(); 
                _isAnimating = false;
            }
        );
    }

    private void ToggleOutline(Image targetImage, bool state)
    {
        if (targetImage != null)
        {
            var outline = targetImage.GetComponent<Outline>();
            if (outline != null) outline.enabled = state;
        }
    }

    private int GetCircularIndex(int index)
    {
        if (index >= avatarBank.Count) return 0;
        if (index < 0) return avatarBank.Count - 1;
        return index;
    }

    private void SetSpritesForIndex(int centerIndex)
    {
        if (avatarDisplayCenter) avatarDisplayCenter.sprite = avatarBank[centerIndex];
        if (avatarDisplayPrev) avatarDisplayPrev.sprite = avatarBank[GetCircularIndex(centerIndex - 1)];
        if (avatarDisplayNext) avatarDisplayNext.sprite = avatarBank[GetCircularIndex(centerIndex + 1)];
    }

    private void OnRandomNameClicked() 
    { 
        if (PlayerAccountManager.Instance != null) nameInput.text = PlayerAccountManager.Instance.GenerateRandomName(); 
    }

    private async void OnSaveClicked()
    {
        if (PlayerAccountManager.Instance == null) return;
        saveButton.interactable = false;

        await PlayerAccountManager.Instance.ChangePlayerName(nameInput.text);

        await PlayerAccountManager.Instance.SaveProfileData(descriptionInput.text, birthdayInput.text, statusInput.text, _currentAvatarIndex);

        if (PlayerAccountManager.Instance != null) PlayerAccountManager.Instance.ForceProfileUpdateEvent();

        saveButton.interactable = true;
        profileSettings.SetActive(false);
        buttonsMenu.SetActive(true);
    }
}