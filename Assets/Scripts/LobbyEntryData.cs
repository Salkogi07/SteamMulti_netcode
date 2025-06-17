// --- START OF FILE LobbyEntryData.cs ---

using UnityEngine;
using UnityEngine.UI;
using Steamworks; // Facepunch.Steamworks
using TMPro;

public class LobbyEntryData : MonoBehaviour
{
    // CSteamID 대신 SteamId 사용
    public SteamId lobbySteamID;
    public string lobbyName;
    public TextMeshProUGUI lobbyNameText;
    public Button joinLobbyButton;

    private void Start()
    {
        joinLobbyButton.onClick.AddListener(JoinLobby);
    }
    
    public void SetLobbyName()
    {
        lobbyNameText.text = string.IsNullOrEmpty(lobbyName) ? "이름 없는 로비" : lobbyName;
    }
  
    public void JoinLobby()
    {
        SteamManager.Instance.JoinLobby(lobbySteamID);
    }
}