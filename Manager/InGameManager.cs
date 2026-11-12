using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 한 판의 시작, 중단, 맵 로드를 담당한다.
/// </summary>
public class InGameManager : ManagerBase
{
    private const string RaidScope = "raid";

    public int MapTid { get; private set; }
    public bool IsRunning { get; private set; }

    public async Task EnterAsync(int mapTid)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[InGameManager] 이미 인게임입니다.");
            return;
        }

        var resources = GameManager.Instance != null ? GameManager.Instance.ResourceManager : null;
        if (resources == null)
        {
            Debug.LogError("[InGameManager] ResourceManager가 없습니다.");
            return;
        }

        IsRunning = true;
        MapTid = mapTid;
        resources.BeginScope(RaidScope);
        await LoadMapAsync(mapTid);
    }

    public void Exit()
    {
        if (!IsRunning)
            return;

        if (GameManager.Instance != null && GameManager.Instance.ResourceManager != null)
            GameManager.Instance.ResourceManager.ReleaseScope(RaidScope);

        IsRunning = false;
    }

    /// <summary>
    /// tid로 맵을 올린다. 맵 데이터는 아직 없다.
    /// </summary>
    private Task LoadMapAsync(int mapTid)
    {
        Debug.Log($"[InGameManager] 맵 로드. tid={mapTid}. 맵 데이터가 없어 콘텐츠는 올리지 않습니다.");
        return Task.CompletedTask;
    }
}
