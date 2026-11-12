using System.Threading.Tasks;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private const string BootLabel = "boot";

    private ResourceManager _resourceManager;
    private InGameManager _inGameManager;

    public ResourceManager ResourceManager
    {
        get
        {
            ManagerBase.EnsureManager(transform, ref _resourceManager);
            return _resourceManager;
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
    /// 인게임 시작 신호. 맵 tid를 인게임 매니저에 넘긴다.
    /// </summary>
    public Task StartGameAsync(int mapTid)
    {
        var inGameManager = InGameManager;
        if (inGameManager == null)
        {
            Debug.LogError("[GameManager] InGameManager가 없습니다.");
            return Task.CompletedTask;
        }

        return inGameManager.EnterAsync(mapTid);
    }
}
