using UnityEngine;
using TMPro;

public class PlayerSlot : MonoBehaviour
{
    public TextMeshProUGUI playerName;
    public GameObject hostIcon;

    public void Setup(string name, bool isHost)
    {
        playerName.text = name;
        hostIcon.SetActive(isHost);
    }
}
