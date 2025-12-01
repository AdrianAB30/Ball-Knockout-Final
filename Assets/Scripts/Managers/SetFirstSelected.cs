using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class SetFirstSelected : MonoBehaviour
{
    [SerializeField] private GameObject firstSelectedButton;

    private void OnEnable()
    {
        StartCoroutine(SelectButtonLater());
    }

    private IEnumerator SelectButtonLater()
    {
        yield return null; 

        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);

            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }
}