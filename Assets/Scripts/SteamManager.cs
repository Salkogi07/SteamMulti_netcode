// --- START OF FILE SteamManager.cs ---

using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Linq;
using System.Threading.Tasks; // Task.Delay를 위해 추가

public enum LobbyType
{
    FriendsOnly,
    Public,
    Private
}

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance;

    public Lobby? CurrentLobby { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
    }

    public async void HostLobby(LobbyType lobbyType, string lobbyName)
    {
        try
        {
            Debug.Log("로비 생성을 시도합니다...");
            var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(4); 

            if (!createLobbyOutput.HasValue)
            {
                Debug.LogError("로비 생성에 실패했습니다.");
                return;
            }

            Lobby lobby = createLobbyOutput.Value;
            lobby.SetData("name", lobbyName); 

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
            CurrentLobby.Value.SetData("closing", "true");
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            TransitionToMainMenu();
        }
    }

    public void LeaveLobby()
    {
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            Debug.Log("로비를 떠났습니다.");
            TransitionToMainMenu();
        }
    }

    public async void JoinLobby(SteamId lobbyId)
    {
        Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (!lobby.HasValue)
        {
            Debug.LogError($"{lobbyId} ID를 가진 로비에 참가할 수 없습니다.");
        }
    }

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

    // [수정] 친구 로비 목록 가져오기 로직 개선
    public async void GetFriendLobbies()
    {
        if (LobbyListManager.instance == null) return;
    
        LobbyListManager.instance.DestroyLobbies();
    
        List<Lobby> friendLobbies = new List<Lobby>();
        
        var friendsInLobbies = SteamFriends.GetFriends()
            .Where(friend => friend.IsPlayingThisGame && friend.GameInfo.HasValue && friend.GameInfo.Value.Lobby.HasValue);

        if (!friendsInLobbies.Any())
        {
            Debug.Log("플레이 중인 친구 로비가 없습니다.");
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
            return;
        }
    
        // 각 로비의 정보 갱신을 '요청'합니다.
        foreach (var friend in friendsInLobbies)
        {
            Lobby lobby = friend.GameInfo.Value.Lobby.Value;
            lobby.Refresh(); // 데이터 갱신 요청
            friendLobbies.Add(lobby);
        }

        // [핵심 수정] Steamworks 콜백이 데이터를 수신할 시간을 잠시 기다립니다.
        // lobby.Refresh()는 비동기 요청이므로, 데이터가 즉시 채워지지 않습니다.
        // 0.2초의 딜레이는 대부분의 경우 충분합니다.
        await Task.Delay(200);

        // 이제 데이터가 갱신되었을 가능성이 높은 로비 목록을 화면에 표시합니다.
        if (friendLobbies.Count > 0)
        {
            Debug.Log($"{friendLobbies.Count}개의 친구 로비를 찾았습니다. 목록을 표시합니다.");
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
        }
        else
        {
            Debug.Log("플레이 중인 친구 로비가 없습니다.");
            LobbyListManager.instance.DisplayLobbies(friendLobbies);
        }
    }

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

    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"로비 입장: {lobby.Id}");

        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdateLobbyName();
            LobbyController.Instance.UpdatePlayerList();
        }
        
        SetPlayerData("ready", "false"); 
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("Lobby");
        }
        else
        {
            Debug.LogError("SceneTransitionManager.Instance가 없습니다! MainMenu 씬에 설정되었는지 확인하세요.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log($"{friend.Name} 님이 로비에 참가했습니다.");
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log($"{friend.Name} 님이 로비를 떠났습니다.");
        
        if (lobby.GetData("closing") != "true" && LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    private void OnLobbyDataChanged(Lobby lobby)
    {
        if (lobby.GetData("closing") == "true")
        {
            if (CurrentLobby.HasValue)
            {
                Debug.Log("방장이 로비를 닫았습니다. 로비에서 나갑니다.");
                LeaveLobby(); 
                return;
            }
        }
        
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