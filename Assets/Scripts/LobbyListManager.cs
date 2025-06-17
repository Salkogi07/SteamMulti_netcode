// --- START OF FILE LobbyListManager.cs ---

using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data; // Lobby 사용을 위해 추가
using UnityEngine.UI;

public class LobbyListManager : MonoBehaviour
{
    public static LobbyListManager instance;
    
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lobbyListMenu;
    [SerializeField] private GameObject lobbyEntryPrefab;
    [SerializeField] private GameObject scrollViewContent;
    
    private List<GameObject> listOfLobbies = new List<GameObject>();
    
    [Header("Button Click Events")]
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }

    private void Start()
    {
        backButton.onClick.AddListener(OnClick_Back);
    }

    public void OnClick_GetFriendLobbies()
    {
        mainMenu.SetActive(false);
        lobbyListMenu.SetActive(true);
        SteamManager.Instance.GetFriendLobbies();
    }
    
    public void OnClick_GetPublicLobbies()
    {
        mainMenu.SetActive(false);
        lobbyListMenu.SetActive(true);
        SteamManager.Instance.GetPublicLobbies();
    }

    public void OnClick_Back()
    {
        mainMenu.SetActive(true);
        lobbyListMenu.SetActive(false);
    }
    
    // Facepunch.Steamworks의 Lobby 객체 리스트를 직접 받아서 처리
    public void DisplayLobbies(List<Lobby> lobbies)
    {
        DestroyLobbies(); // 목록을 표시하기 전에 기존 목록을 항상 초기화

        foreach (var lobby in lobbies)
        {
            string lobbyName = lobby.GetData("name");

            // 로비 이름이 비어있으면 목록에 표시하지 않거나, 기본 이름을 지정합니다.
            if (string.IsNullOrEmpty(lobbyName))
                continue;
            
            // 프리팹에서 UI 항목 생성
            GameObject createdLobbyItem = Instantiate(lobbyEntryPrefab, scrollViewContent.transform);
            LobbyEntryData lobbyData = createdLobbyItem.GetComponent<LobbyEntryData>();

            // 생성된 UI 항목에 로비 정보 설정
            lobbyData.lobbySteamID = lobby.Id;
            lobbyData.lobbyName = lobbyName;
            lobbyData.SetLobbyName();

            // 관리 리스트에 추가
            listOfLobbies.Add(createdLobbyItem);
        }
    }

    public void DestroyLobbies()
    {
        foreach (GameObject lobbyItem in listOfLobbies)
        {
            Destroy(lobbyItem);
        }
        listOfLobbies.Clear();
    }
}