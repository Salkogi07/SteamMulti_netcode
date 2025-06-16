using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Linq;
using TMPro;

public enum LobbyType
{
    FriendsOnly,
    Public,
    Private
}

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance;

    // 현재 접속해 있는 로비를 저장합니다. 이 객체를 통해 멤버, 데이터 등 모든 정보에 접근합니다.
    public Lobby? CurrentLobby { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지되도록 설정
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Steamworks 이벤트 구독
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDisable()
    {
        // Steamworks 이벤트 구독 해제
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        await lobby.Join();
    }

    // 로비 생성
    public async void HostLobby(LobbyType lobbyType, string lobbyName)
    {
        try
        {
            Debug.Log("로비 생성을 시도합니다...");
            // Facepunch.Steamworks의 async/await를 사용한 로비 생성
            var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(4); // maxConnections

            if (!createLobbyOutput.HasValue)
            {
                Debug.LogError("로비 생성에 실패했습니다.");
                return;
            }

            Lobby lobby = createLobbyOutput.Value;
            lobby.SetData("name", lobbyName); // 로비 이름을 메타데이터로 설정

            if (lobbyType == LobbyType.Private)
                lobby.SetPrivate();
            else if(lobbyType == LobbyType.FriendsOnly)
                lobby.SetFriendsOnly();
            else if(lobbyType == LobbyType.Public)
                lobby.SetPublic();
            
            lobby.SetJoinable(true);

            Debug.Log($"로비 생성 완료! ID: {lobby.Id}, 이름: {lobbyName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로비 생성 중 예외 발생: {e.Message}");
        }
    }

    // 로비 떠나기
    public void LeaveLobby()
    {
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            Debug.Log("로비를 떠났습니다.");

            // [수정] 메인 메뉴 씬으로 전환
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("MainMenu");
            }
            else
            {
                Debug.LogError("SceneTransitionManager.Instance가 없습니다! MainMenu 씬에 설정되었는지 확인하세요.");
                // 폴백: 로딩 화면 없이 바로 씬 로드
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }

    // ID로 로비에 참가
    public async void JoinLobby(SteamId lobbyId)
    {
        Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (!lobby.HasValue)
        {
            Debug.LogError($"{lobbyId} ID를 가진 로비에 참가할 수 없습니다.");
        }
    }

    // 공개 로비 목록 가져오기
    public async void GetPublicLobbies()
    {
        if (LobbyListManager.instance != null)
        {
            LobbyListManager.instance.DestroyLobbies();
            var lobbies = await SteamMatchmaking.LobbyList
                .WithSlotsAvailable(1)
                .RequestAsync();

            if (lobbies != null)
            {
                LobbyListManager.instance.DisplayLobbies(lobbies.ToList());
            }
        }
    }

    // 친구 로비 목록 가져오기
    public async void GetFriendLobbies()
    {
        if (LobbyListManager.instance == null) return;
        
        LobbyListManager.instance.DestroyLobbies();
        List<Lobby> friendLobbies = new List<Lobby>();
        
        foreach (var friend in SteamFriends.GetFriends())
        {
            if (friend.IsPlayingThisGame && friend.GameInfo.HasValue && friend.GameInfo.Value.Lobby.HasValue)
            {
                Lobby lobby = friend.GameInfo.Value.Lobby.Value;
                // 로비 정보를 최신으로 갱신
                if (lobby.Refresh())
                {
                    friendLobbies.Add(lobby);
                }
            }
        }

        if (friendLobbies.Count > 0)
        {
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
        }
        else
        {
            Debug.Log("플레이 중인 친구 로비가 없습니다.");
        }
    }
    

    // 로비가 성공적으로 생성되었을 때 호출됩니다.
    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            Debug.Log("OnLobbyCreated 콜백 수신.");
        }
        else
        {
            Debug.LogError($"로비 생성 실패: {result}");
        }
    }

    // 로비에 들어갔을 때 호출됩니다. (생성, 참가 모두 포함)
    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"로비 입장: {lobby.Id}");

        // 로비에 들어간 후, 로비 UI 컨트롤러에 알림
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdateLobbyName();
            LobbyController.Instance.UpdatePlayerList();
        }

        // 로비에 참가한 모든 플레이어에게 내 정보 전송
        // (이름은 Steam에서 자동으로 가져오므로 준비 상태 같은 추가 정보만 전송)
        SetPlayerData("ready", "false"); // 처음엔 준비 안된 상태
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("Lobby");
        }
        else
        {
            Debug.LogError("SceneTransitionManager.Instance가 없습니다! MainMenu 씬에 설정되었는지 확인하세요.");
            // 폴백: 로딩 화면 없이 바로 씬 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    // 다른 플레이어가 로비에 참가했을 때 호출됩니다.
    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log($"{friend.Name} 님이 로비에 참가했습니다.");
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    // 다른 플레이어가 로비를 떠났을 때 호출됩니다.
    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log($"{friend.Name} 님이 로비를 떠났습니다.");
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
        
        // 만약 떠난 사람이 방장이었다면 새 방장 정보를 업데이트
        if(LobbyController.Instance != null && lobby.Owner.Id == friend.Id)
        {
            LobbyController.Instance.UpdatePlayerList();
            Debug.Log($"새 방장: {lobby.Owner.Name}");
        }
    }

    // 로비 데이터가 변경될 때 호출됩니다.
    private void OnLobbyDataChanged(Lobby lobby)
    {
        Debug.Log("로비 데이터가 변경되었습니다.");
        // LobbyController가 null이 아닐 때만 (즉, Lobby 씬에 있을 때만) UI를 업데이트합니다.
        if (LobbyController.Instance != null && CurrentLobby.HasValue && CurrentLobby.Value.Id == lobby.Id)
        {
            LobbyController.Instance.UpdateLobbyName();
            LobbyController.Instance.UpdatePlayerList();
        }
    }
    public void SetPlayerData(string key, string value)
    {
        if (!CurrentLobby.HasValue) return;
        CurrentLobby.Value.SetMemberData(key, value);
    }
}
