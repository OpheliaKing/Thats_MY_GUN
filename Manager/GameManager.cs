using System.Threading.Tasks;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private ResourceManager _resourceManager;
    private InputManager _inputManager;
    private InGameManager _inGameManager;

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

    public InGameManager InGameManager
    {
        get
        {
            ManagerBase.EnsureManager(transform, ref _inGameManager);
            return _inGameManager;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        PreloadBootAsync();
    }

    private void Start()
    {
        InGameManager.StartGameAsync();
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

        await resourceManager.PreloadLabelAsync(PublicVariable.BootLabel);
    }
}
