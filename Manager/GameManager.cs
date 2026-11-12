using System.Threading.Tasks;
using SHIN;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private const string BootLabel = "boot";

    private ResourceManager _resourceManager;
    private InputManager _inputManager;
    private CharacterBase _player;

    public ResourceManager ResourceManager
    {
        get
        {
            ManagerBase.EnsureManager(transform, ref _resourceManager);
            return _resourceManager;
        }
    }

    public InputManager InputManager
    {
        get
        {
            ManagerBase.EnsureManager(transform, ref _inputManager);
            return _inputManager;
        }
    }

    /// <summary>
    /// 시작에 필요한 boot 라벨을 올린다.
    /// 스코프는 열지 않아서, 이 로드는 한 판이 끝나도 내려가지 않는다.
    /// </summary>
    public async Task PreloadBootAsync()
    {
        var resourceManager = ResourceManager;
        if (resourceManager == null)
        {
            Debug.LogError("[GameManager] ResourceManager가 없습니다.");
            return;
        }

        await resourceManager.PreloadLabelAsync(BootLabel);
    }

    /// <summary>
    /// 스폰한 플레이어에게만 입력 콜백을 붙인다. 이전 플레이어는 구독을 해제한다.
    /// </summary>
    public void BindPlayerInput(CharacterBase player)
    {
        if (_player != null)
            _player.UnbindInput();

        _player = player;
        if (_player == null)
            return;

        _player.BindInput(InputManager);
    }
}
