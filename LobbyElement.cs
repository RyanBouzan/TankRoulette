using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyElement : MonoBehaviour
{

    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI CountText;
    public Button JoinButton;

    public void AssignButton(Button button)
    {
        JoinButton = button;
    }
}
