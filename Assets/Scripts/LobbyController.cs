// --- START OF FILE LobbyController.cs ---

using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI LobbyNameText;
    public Button StartGameButton;
    public Button ReadyButton;
    public Button LeaveButton;
    public TextMeshProUGUI ReadyButtonText;

    [Header("Player List")]
    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;

    private List<PlayerListItem> playerListItems = new List<PlayerListItem>();
    private bool isReady = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
    }

    private void Start()
    {
        StartGameButton.onClick.AddListener(Onclick_StartGame);
        ReadyButton.onClick.AddListener(OnClick_ReadyPlayer);
        LeaveButton.onClick.AddListener(OnClick_LeaveLobby);
    }

    private void Onclick_StartGame()
    {
        SteamManager.Instance.StartGameServer();
    }

    public void OnClick_LeaveLobby()
    {
        // [수정] SteamManager와 CurrentLobby가 유효한지 확인
        if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
        {
            var lobby = SteamManager.Instance.CurrentLobby.Value;
            bool isOwner = lobby.Owner.Id == SteamClient.SteamId;

            if (isOwner)
            {
                // [수정] 내가 방장이라면, 로비를 파괴하는 함수를 호출합니다.
                Debug.Log("방장이 로비 파괴를 시도합니다.");
                SteamManager.Instance.DestroyLobby();
            }
            else
            {
                // [수정] 일반 멤버라면, 그냥 로비를 나갑니다.
                Debug.Log("일반 멤버가 로비를 나갑니다.");
                SteamManager.Instance.LeaveLobby();
            }
        }
    }

    public void OnClick_ReadyPlayer()
    {
        isReady = !isReady;
        SteamManager.Instance.SetPlayerData("ready", isReady.ToString().ToLower());
        UpdateButtonText();
    }
    
    private void UpdateButtonText()
    {
        ReadyButtonText.text = isReady ? "Ready" : "Unready";
    }

    // 모든 플레이어가 준비되었는지 확인하고 시작 버튼 상태를 업데이트
    public void CheckIfAllReady()
    {
        var lobby = SteamManager.Instance.CurrentLobby;
        if (!lobby.HasValue)
        {
            if(StartGameButton != null) StartGameButton.gameObject.SetActive(false);
            return;
        }
        
        bool isOwner = lobby.Value.Owner.Id == SteamClient.SteamId;
        
    
        if(StartGameButton != null)
        {
            // 방장일 경우에만 시작 버튼을 활성화합니다.
            StartGameButton.gameObject.SetActive(isOwner);
        }

        // 방장이 아니면 더 이상 진행할 필요가 없으므로 return 합니다.
        if (!isOwner) return;

        // --- 아래 코드는 방장인 경우에만 실행됩니다 ---

        // 모든 멤버가 준비되었는지 확인합니다.
        bool allReady = true;
        foreach (var member in lobby.Value.Members)
        {
            // GetMemberData는 키-값 쌍으로 데이터를 가져옵니다.
            string readyStatus = lobby.Value.GetMemberData(member, "ready");
        
            // "true"가 아니면 준비되지 않은 것으로 간주합니다.
            if (readyStatus != "true")
            {
                allReady = false;
                break; // 한 명이라도 준비가 안됐으면 더 확인할 필요가 없습니다.
            }
        }

        // 모든 멤버가 준비되었을 때만 시작 버튼을 클릭할 수 있게 합니다.
        if(StartGameButton != null)
        {
            StartGameButton.interactable = allReady;
        }
    }

    public void UpdateLobbyName()
    {
        var lobby = SteamManager.Instance.CurrentLobby;
        if (lobby.HasValue)
        {
            LobbyNameText.text = lobby.Value.GetData("name");
        }
    }

    public void UpdatePlayerList()
    {
        var lobby = SteamManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;

        var members = lobby.Value.Members.ToList();
        
        var itemsToRemove = playerListItems.Where(item => !members.Any(member => member.Id == item.PlayerSteamID)).ToList();
        foreach (var item in itemsToRemove)
        {
            Destroy(item.gameObject);
            playerListItems.Remove(item);
        }

        foreach (var member in members)
        {
            var existingItem = playerListItems.FirstOrDefault(item => item.PlayerSteamID == member.Id);
            if (existingItem == null)
            {
                GameObject newPlayerItem = Instantiate(PlayerListItemPrefab, PlayerListViewContent.transform);
                PlayerListItem newPlayerItemScript = newPlayerItem.GetComponent<PlayerListItem>();
                playerListItems.Add(newPlayerItemScript);
                existingItem = newPlayerItemScript;
            }

            existingItem.PlayerName = member.Name;
            existingItem.PlayerSteamID = member.Id;
            
            // [수정] 멤버의 준비 상태는 GetMemberData로 가져옵니다.
            existingItem.Ready = lobby.Value.GetMemberData(member, "ready") == "true";

            // [수정] 멤버가 방장인지 확인하려면, 멤버의 ID와 로비의 Owner ID를 비교해야 합니다.
            existingItem.IsLobbyOwner = (member.Id == lobby.Value.Owner.Id);

            existingItem.SetPlayerValues();
        }
        
        CheckIfAllReady();
    }
}