using System;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField MessageInputField;
    [SerializeField] private TextMeshProUGUI MessageTemplate;
    [SerializeField] private GameObject MessageContainer;

    private void Start()
    {
        MessageTemplate.text = "";
    }

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
    }

    private void LobbyMemberLeave(Lobby lobby, Friend friend) => AddMessageToBox(friend.Name + "Left the Lobby");

    private void LobbyMemberJoined(Lobby lobby, Friend friend) => AddMessageToBox(friend.Name + "Joined the Lobby");

    private void LobbyEntered(Lobby obj) => AddMessageToBox("You entered the lobby!");
    private void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        AddMessageToBox(friend.Name + ": " + msg);
    }

    private void AddMessageToBox(string msg)
    {
        GameObject message = Instantiate(MessageTemplate.gameObject, MessageContainer.transform);
        message.GetComponent<TextMeshProUGUI>().text = msg;
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
