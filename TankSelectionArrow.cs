using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TankSelectionArrow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int currentTank = 0;
    [SerializeField]
    List<GameObject> TankSelect = new();

    [SerializeField] private Image _img;
    [SerializeField] private Sprite _default, _pressed;
    public void showNextTank()
    {
        int previousTank = currentTank;
        if (currentTank == TankSelect.Count - 1)
        {
            currentTank = 0;
        }
        else
        {
            currentTank++;
        }

        TankSelect[previousTank].SetActive(false);
        TankSelect[currentTank].SetActive(true);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        _img.sprite = _pressed;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _img.sprite = _default;

    }
}
