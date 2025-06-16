// --- START OF FILE HostLobbyController.cs ---

using UnityEngine;
using UnityEngine.UI;
using Steamworks; // Facepunch.Steamworks

public class HostLobbyController : MonoBehaviour
{
    [SerializeField] private GameObject hostLobbyPanel;
    [SerializeField] private InputField lobbyNameInput;
    [SerializeField] private Toggle friendsOnlyToggle;
    [SerializeField] private GameObject mainMenuButtonsPanel;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button backButton;
    
    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    public void ShowHostLobbyPanel()
    {
        mainMenuButtonsPanel.SetActive(false);
        hostLobbyPanel.SetActive(true);
        // 기본 로비 이름 설정
        lobbyNameInput.text = SteamClient.Name + "'s Lobby";
    }

    private void OnCreateLobbyClicked()
    {
        if (string.IsNullOrWhiteSpace(lobbyNameInput.text))
        {
            Debug.LogWarning("로비 이름은 비워둘 수 없습니다.");
            return;
        }
        
        LobbyType lobbyType = friendsOnlyToggle.isOn ? LobbyType.FriendsOnly : LobbyType.Public;
        
        SteamManager.Instance.HostLobby(lobbyType, lobbyNameInput.text);
        
        // 로비 생성 후, 패널을 즉시 닫지 않고 OnLobbyEntered에서 닫도록 처리할 수 있습니다.
        // 여기서는 일단 기존 로직대로 닫습니다.
        hostLobbyPanel.SetActive(false);
    }

    private void OnBackButtonClicked()
    {
        hostLobbyPanel.SetActive(false);
        mainMenuButtonsPanel.SetActive(true);
    }
}