// --- START OF FILE ChatManager.cs ---

using System;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // [추가] ScrollRect를 사용하기 위해 필요
using System.Collections; // [추가] 코루틴(IEnumerator)을 사용하기 위해 필요

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField MessageInputField;
    [SerializeField] private TextMeshProUGUI MessageTemplate;
    [SerializeField] private GameObject MessageContainer;
    [SerializeField] private ScrollRect chatScrollRect; // [추가] 채팅 스크롤 뷰의 ScrollRect 컴포넌트

    private void Start()
    {
        MessageTemplate.text = "";
        
        if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
        {
            AddMessageToBox("You entered the lobby!");
        }
    }

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
    }

    void OnDisable()
    {
        SteamMatchmaking.OnChatMessage -= ChatSent;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeave;
    }

    private void LobbyMemberLeave(Lobby lobby, Friend friend) => AddMessageToBox(friend.Name + " Left the Lobby");

    private void LobbyMemberJoined(Lobby lobby, Friend friend) => AddMessageToBox(friend.Name + " Joined the Lobby");

    private void LobbyEntered(Lobby obj)
    {
        // Start()에서 이미 처리하므로 중복 호출될 수 있습니다.
    }
    
    private void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        AddMessageToBox(friend.Name + ": " + msg);
    }

    private void AddMessageToBox(string msg)
    {
        GameObject message = Instantiate(MessageTemplate.gameObject, MessageContainer.transform);
        message.GetComponent<TextMeshProUGUI>().text = msg;

        // [추가] 메시지 추가 후, 스크롤을 맨 아래로 내리는 코루틴을 시작합니다.
        StartCoroutine(ScrollToBottomCoroutine());
    }

    // [추가] 스크롤을 맨 아래로 내리는 코루틴
    private IEnumerator ScrollToBottomCoroutine()
    {
        // UI 레이아웃이 업데이트될 때까지 현재 프레임의 끝에서 대기합니다.
        // 이렇게 해야 ScrollRect가 콘텐츠 크기를 제대로 인지한 후 스크롤 위치를 설정할 수 있습니다.
        yield return new WaitForEndOfFrame();

        // ScrollRect의 수직 스크롤 위치를 0(가장 아래)으로 설정합니다. (1은 가장 위)
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ToggleChatBox();   
        }
    }

    private void ToggleChatBox()
    {
        if (MessageInputField.gameObject.activeSelf)
        {
            if (!String.IsNullOrEmpty(MessageInputField.text))
            {
                SteamManager.Instance.CurrentLobby?.SendChatString(MessageInputField.text);
                MessageInputField.text = "";
            }
            
            MessageInputField.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            MessageInputField.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(MessageInputField.gameObject);
        }
    }
}