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
    }

    private void OnDisable()
    {
        // Steamworks 이벤트 구독 해제
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
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

    public void DestroyLobby()
    {
        if (CurrentLobby.HasValue && CurrentLobby.Value.Owner.Id == SteamClient.SteamId)
        {
            Debug.Log("로비에 'closing' 신호를 보내고 파괴합니다.");
            
            // 다른 멤버들에게 로비가 닫힌다는 신호를 보냄
            CurrentLobby.Value.SetData("closing", "true");
            
            // 신호를 보낸 후, 자신도 로비를 떠남 (이것이 로비를 최종 파괴함)
            CurrentLobby.Value.Leave();
            CurrentLobby = null;

            // 메인 메뉴로 돌아감
            TransitionToMainMenu();
        }
    }

    // [수정] 일반 멤버용: 로비에서 나가기
    public void LeaveLobby()
    {
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            Debug.Log("로비를 떠났습니다.");
            
            // 메인 메뉴로 돌아감
            TransitionToMainMenu();
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
        if (LobbyListManager.instance == null) return;

        LobbyListManager.instance.DestroyLobbies();
        var lobbies = await SteamMatchmaking.LobbyList
            .WithSlotsAvailable(1)
            .RequestAsync();

        if (lobbies != null)
        {
            LobbyListManager.instance.DisplayLobbies(lobbies.ToList());
        }
    }

    // 친구 로비 목록 가져오기
    public async void GetFriendLobbies()
    {
        if (LobbyListManager.instance == null) return;
    
        LobbyListManager.instance.DestroyLobbies();
    
        // 최종적으로 화면에 표시할 로비 목록
        List<Lobby> friendLobbies = new List<Lobby>();

        // 1. 현재 이 게임을 플레이 중이고 로비에 있는 친구들만 필터링합니다.
        var friendsInLobbies = SteamFriends.GetFriends()
            .Where(friend => friend.IsPlayingThisGame && friend.GameInfo.HasValue && friend.GameInfo.Value.Lobby.HasValue);

        if (!friendsInLobbies.Any())
        {
            Debug.Log("플레이 중인 친구 로비가 없습니다.");
            LobbyListManager.instance.DisplayLobbies(friendLobbies); // 빈 목록이라도 UI 갱신을 위해 호출
            return;
        }
    
        // 2. 각 로비의 정보 갱신을 '요청'합니다.
        //    이 시점에서는 요청만 보내고 기다리지 않습니다.
        foreach (var friend in friendsInLobbies)
        {
            Lobby lobby = friend.GameInfo.Value.Lobby.Value;
            lobby.Refresh(); // 데이터 갱신 요청
            friendLobbies.Add(lobby); // 우선 갱신될 로비 객체를 리스트에 추가
        }

        // 3. Steamworks 콜백이 처리될 시간을 잠시 기다립니다. (핵심)
        //    0.2초 정도면 대부분의 경우 충분합니다. 네트워크 상태에 따라 조절할 수 있습니다.
        await System.Threading.Tasks.Task.Delay(200);

        // 4. 이제 데이터가 갱신된 로비 목록을 화면에 표시합니다.
        //    LobbyListManager는 이 리스트의 Lobby 객체에서 최신 데이터를 읽어 UI를 생성합니다.
        if (friendLobbies.Count > 0)
        {
            Debug.Log($"{friendLobbies.Count}개의 친구 로비를 찾았습니다. 목록을 표시합니다.");
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
        }
        else
        {
            Debug.Log("플레이 중인 친구 로비가 없습니다.");
            // DisplayLobbies를 호출하여 UI를 "로비 없음" 상태로 확실히 갱신합니다.
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
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
        
        // 로비가 닫히는 중이 아닐 때만 플레이어 목록을 갱신합니다.
        if (lobby.GetData("closing") != "true" && LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    // 로비 데이터가 변경될 때 호출됩니다. [수정됨]
    private void OnLobbyDataChanged(Lobby lobby)
    {
        // 방장이 설정한 'closing' 신호가 있는지 확인
        if (lobby.GetData("closing") == "true")
        {
            if (CurrentLobby.HasValue)
            {
                Debug.Log("방장이 로비를 닫았습니다. 로비에서 나갑니다.");
                // 일반 멤버용 LeaveLobby 함수를 호출
                LeaveLobby(); 
                return; // 로비를 떠났으므로 더 이상 아래 로직을 실행할 필요 없음
            }
        }

        // 'closing' 신호가 없을 때만 일반적인 데이터 변경 로직을 처리
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
    
    private void TransitionToMainMenu()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("MainMenu");
        }
        else
        {
            Debug.LogError("SceneTransitionManager.Instance가 없습니다! MainMenu 씬에 설정되었는지 확인하세요.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
