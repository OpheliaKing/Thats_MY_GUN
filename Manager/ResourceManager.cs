using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables Load / Instantiate / Release 진입점.
/// 주소와 라벨은 참조 수로 보관한다. 참조가 0이 될 때만 핸들을 푼다.
/// 스코프를 풀 때는 그 스코프의 인스턴스를 먼저 없앤 뒤 참조를 내린다.
/// </summary>
public partial class ResourceManager : ManagerBase
{
    private readonly Dictionary<string, AssetHold> _assets = new();
    private readonly Dictionary<string, AssetHold> _labels = new();
    private readonly Dictionary<EntityId, TrackedInstance> _instances = new();

    public async Task<T> LoadAsync<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[ResourceManager] address가 비어 있습니다.");
            return null;
        }

        if (_assets.TryGetValue(address, out var existing))
        {
            if (!existing.Handle.IsValid())
            {
                _assets.Remove(address);
            }
            else
            {
                if (!existing.Handle.IsDone)
                    await existing.Handle.Task;

                if (!existing.Handle.IsValid() || existing.Handle.Status != AsyncOperationStatus.Succeeded)
                    return null;

                return TakeAsset<T>(existing, address);
            }
        }

        TraceLoadStarted(address, ResourceTraceKind.Asset);
        var handle = Addressables.LoadAssetAsync<T>(address);
        var hold = new AssetHold { Handle = handle };
        _assets[address] = hold;
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[ResourceManager] 로드 실패: {address}");
            ReleaseFailedHandle(address, hold);
            TraceLoadFinished(address, ResourceTraceKind.Asset, false);
            return null;
        }

        var asset = TakeAsset<T>(hold, address);
        if (asset == null)
        {
            ReleaseFailedHandle(address, hold);
            TraceLoadFinished(address, ResourceTraceKind.Asset, false);
            return null;
        }

        TraceLoadFinished(address, ResourceTraceKind.Asset, true);
        return asset;
    }

    public void LoadAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
    {
        LoadInternalAsync(address, onComplete);
    }

    private async void LoadInternalAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
    {
        var result = await LoadAsync<T>(address);
        onComplete?.Invoke(result);
    }

    /// <summary>
    /// Addressables 프리팹을 생성한다.
    /// startInactive면 Load 후 동기 Instantiate → 즉시 비활성화해서
    /// InstantiateAsync 완료~비활성 사이 한 프레임 노출을 막는다.
    /// 생성된 인스턴스는 주소와 현재 스코프에 기록한다.
    /// </summary>
    public async Task<GameObject> InstantiateAsync(
        string address,
        Transform parent = null,
        bool startInactive = true,
        Vector3? worldPosition = null)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[ResourceManager] address가 비어 있습니다.");
            return null;
        }

        var prefab = await LoadAsync<GameObject>(address);
        if (prefab == null)
        {
            Debug.LogError($"[ResourceManager] 생성 실패(프리팹 로드): {address}");
            return null;
        }

        var instance = UnityEngine.Object.Instantiate(prefab);
        if (startInactive)
            instance.SetActive(false);

        if (parent != null)
            instance.transform.SetParent(parent, false);

        if (worldPosition.HasValue)
            instance.transform.position = worldPosition.Value;
        else
            instance.transform.localPosition = Vector3.zero;

        instance.transform.localRotation = Quaternion.identity;
        TrackInstance(instance, address);
        return instance;
    }

    public void InstantiateAsync(
        string address,
        Transform parent,
        Action<GameObject> onComplete,
        bool startInactive = true)
    {
        InstantiateInternalAsync(address, parent, onComplete, startInactive);
    }

    private async void InstantiateInternalAsync(
        string address,
        Transform parent,
        Action<GameObject> onComplete,
        bool startInactive)
    {
        var instance = await InstantiateAsync(address, parent, startInactive);
        onComplete?.Invoke(instance);
    }

    /// <summary>
    /// 라벨에 붙은 에셋을 한 번에 프리로드한다. 씬/전투 진입 전 호출.
    /// </summary>
    public async Task PreloadLabelAsync(string label)
    {
        if (string.IsNullOrEmpty(label))
            return;

        if (_labels.TryGetValue(label, out var existing))
        {
            if (!existing.Handle.IsValid())
            {
                _labels.Remove(label);
            }
            else
            {
                if (!existing.Handle.IsDone)
                    await existing.Handle.Task;

                if (!existing.Handle.IsValid() || existing.Handle.Status != AsyncOperationStatus.Succeeded)
                    return;

                existing.RefCount++;
                TrackLoadedLabel(label);
                return;
            }
        }

        TraceLoadStarted(label, ResourceTraceKind.Label);
        var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
        var hold = new AssetHold { Handle = handle };
        _labels[label] = hold;
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[ResourceManager] 라벨 프리로드 실패: {label}");
            ReleaseFailedLabel(label, hold);
            TraceLoadFinished(label, ResourceTraceKind.Label, false);
            return;
        }

        hold.RefCount++;
        TrackLoadedLabel(label);
        TraceLoadFinished(label, ResourceTraceKind.Label, true);
    }

    /// <summary>LoadAsync 한 번이 잡은 참조를 하나 내린다. 0이 되면 핸들을 푼다.</summary>
    public void Release(string address)
    {
        if (string.IsNullOrEmpty(address) || !_assets.ContainsKey(address))
            return;

        UntrackOneAsset(address);
        ReleaseAssetReferences(address, 1);
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null)
            return;

        if (!_instances.TryGetValue(instance.GetEntityId(), out var hold))
        {
            if (!Addressables.ReleaseInstance(instance))
                Destroy(instance);

            TraceReleased(instance.name, ResourceTraceKind.Instance);
            return;
        }

        DestroyTrackedInstance(hold, releaseAsset: true);
    }

    /// <summary>PreloadLabelAsync 한 번이 잡은 참조를 하나 내린다.</summary>
    public void ReleaseLabel(string label)
    {
        if (string.IsNullOrEmpty(label) || !_labels.ContainsKey(label))
            return;

        UntrackOneLabel(label);
        ReleaseLabelReferences(label, 1);
    }

    public void ReleaseAllAssets()
    {
        DestroyTrackedInstances();

        foreach (var scope in _scopes.Values)
            scope.AddressCounts.Clear();

        foreach (var pair in _assets)
        {
            NotifyAssetReleased(pair.Key);
            if (pair.Value.Handle.IsValid())
                Addressables.Release(pair.Value.Handle);
            TraceReleased(pair.Key, ResourceTraceKind.Asset);
        }

        _assets.Clear();
    }

    public void ReleaseAllLabels()
    {
        foreach (var scope in _scopes.Values)
            scope.LabelCounts.Clear();

        foreach (var pair in _labels)
        {
            if (pair.Value.Handle.IsValid())
                Addressables.Release(pair.Value.Handle);
            TraceReleased(pair.Key, ResourceTraceKind.Label);
        }

        _labels.Clear();
    }

    private void OnDestroy()
    {
        ReleaseAllLabels();
        ReleaseAllAssets();
    }

    private T TakeAsset<T>(AssetHold hold, string address) where T : UnityEngine.Object
    {
        var result = hold.Handle.Result as T;
        if (result == null)
        {
            Debug.LogError($"[ResourceManager] 타입 불일치: {address} -> {typeof(T).Name}");
            return null;
        }

        hold.RefCount++;
        TrackLoadedAsset(address);
        return result;
    }

    private void ReleaseFailedHandle(string address, AssetHold hold)
    {
        if (_assets.TryGetValue(address, out var current) && current == hold)
            _assets.Remove(address);

        if (hold.Handle.IsValid())
            Addressables.Release(hold.Handle);
    }

    private void ReleaseFailedLabel(string label, AssetHold hold)
    {
        if (_labels.TryGetValue(label, out var current) && current == hold)
            _labels.Remove(label);

        if (hold.Handle.IsValid())
            Addressables.Release(hold.Handle);
    }

    private void ReleaseAssetReferences(string address, int count)
    {
        if (count <= 0 || string.IsNullOrEmpty(address))
            return;

        if (!_assets.TryGetValue(address, out var hold))
            return;

        hold.RefCount -= count;
        if (hold.RefCount > 0)
            return;

        NotifyAssetReleased(address);
        if (hold.Handle.IsValid())
            Addressables.Release(hold.Handle);

        _assets.Remove(address);
        TraceReleased(address, ResourceTraceKind.Asset);
    }

    private void ReleaseLabelReferences(string label, int count)
    {
        if (count <= 0 || string.IsNullOrEmpty(label))
            return;

        if (!_labels.TryGetValue(label, out var hold))
            return;

        hold.RefCount -= count;
        if (hold.RefCount > 0)
            return;

        if (hold.Handle.IsValid())
            Addressables.Release(hold.Handle);

        _labels.Remove(label);
        TraceReleased(label, ResourceTraceKind.Label);
    }

    private void TrackInstance(GameObject instance, string address)
    {
        var hold = new TrackedInstance
        {
            Id = instance.GetEntityId(),
            Instance = instance,
            Address = address,
            ScopeName = _currentScope
        };
        _instances[hold.Id] = hold;

        if (!string.IsNullOrEmpty(_currentScope) && _scopes.TryGetValue(_currentScope, out var scope))
            scope.Instances.Add(hold);

        var traceKey = InstanceTraceKey(hold);
        TraceLoadStarted(traceKey, ResourceTraceKind.Instance);
        TraceLoadFinished(traceKey, ResourceTraceKind.Instance, true);
    }

    private void DestroyTrackedInstances()
    {
        foreach (var scope in _scopes.Values)
            scope.Instances.Clear();

        if (_instances.Count == 0)
            return;

        var holds = new TrackedInstance[_instances.Count];
        _instances.Values.CopyTo(holds, 0);
        _instances.Clear();

        for (var i = 0; i < holds.Length; i++)
        {
            TraceReleased(InstanceTraceKey(holds[i]), ResourceTraceKind.Instance);
            if (holds[i].Instance != null)
                Destroy(holds[i].Instance);
        }
    }

    private void DestroyTrackedInstance(TrackedInstance hold, bool releaseAsset)
    {
        _instances.Remove(hold.Id);

        if (!string.IsNullOrEmpty(hold.ScopeName)
            && _scopes.TryGetValue(hold.ScopeName, out var scope))
        {
            scope.Instances.Remove(hold);
        }

        TraceReleased(InstanceTraceKey(hold), ResourceTraceKind.Instance);

        if (hold.Instance != null)
            Destroy(hold.Instance);

        if (!releaseAsset)
            return;

        UntrackAssetCount(hold.ScopeName, hold.Address, 1);
        ReleaseAssetReferences(hold.Address, 1);
    }

    private static string InstanceTraceKey(TrackedInstance hold)
    {
        return hold.Address + "#" + hold.Id;
    }

    private sealed class AssetHold
    {
        public AsyncOperationHandle Handle;
        public int RefCount;
    }

    private sealed class TrackedInstance
    {
        public EntityId Id;
        public GameObject Instance;
        public string Address;
        public string ScopeName;
    }
}
