// --- START OF FILE PlayerListItem.cs ---

using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Threading.Tasks; // 비동기 작업을 위해 추가

public class PlayerListItem : MonoBehaviour
{
    public string PlayerName;
    public SteamId PlayerSteamID;
    public bool Ready;
    public bool IsLobbyOwner;

    [Header("UI Elements")]
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text playerStatusText; // "준비", "방장" 등 상태 표시
    [SerializeField] private Image playerOwnerImage;
    [SerializeField] private RawImage playerAvatarImage; // Steam 아바타를 표시할 RawImage

    // 이전에 아바타를 로드했는지 확인하여 중복 로드를 방지합니다.
    private bool avatarLoaded = false;

    public void SetPlayerValues()
    {
        playerNameText.text = PlayerName;
        if (IsLobbyOwner)
            playerOwnerImage.enabled = true;
        else
            playerOwnerImage.enabled = false;

        playerStatusText.text = Ready ? "준비 완료" : "대기 중";
        playerStatusText.color = Ready ? Color.green : Color.white;
        
        // 아바타가 아직 로드되지 않았다면 로드를 시작합니다.
        if (!avatarLoaded)
        {
            FetchAndSetAvatar();
        }
    }
    
    private async void FetchAndSetAvatar()
    {
        // PlayerSteamID가 유효한지 확인
        if (PlayerSteamID.IsValid)
        {
            // Steamworks.NET의 최신 기능인 async/await를 사용하여 아바타를 가져옵니다.
            var avatar = await SteamFriends.GetLargeAvatarAsync(PlayerSteamID);

            if (avatar.HasValue)
            {
                // Steam 이미지를 Unity Texture2D로 변환
                Texture2D avatarTexture = ConvertSteamImageToTexture2D(avatar.Value);
                if (avatarTexture != null)
                {
                    playerAvatarImage.texture = avatarTexture;
                    playerAvatarImage.gameObject.SetActive(true); // 이미지가 있을 때만 활성화
                    avatarLoaded = true; // 로드 완료 플래그 설정
                }
            }
            else
            {
                // 아바타를 가져오지 못한 경우 RawImage를 비활성화할 수 있습니다.
                playerAvatarImage.gameObject.SetActive(false);
            }
        }
    }
    
    private static Texture2D ConvertSteamImageToTexture2D(Steamworks.Data.Image image)
    {
        // 텍스처를 생성합니다.
        var texture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, false);

        // Steamworks.NET에서 제공하는 이미지 데이터는 상하 반전 없이 바로 사용할 수 있습니다.
        // 원본 데이터(image.Data)를 텍스처에 직접 로드합니다.
        texture.LoadRawTextureData(image.Data);
        texture.Apply();

        return texture;
    }
}