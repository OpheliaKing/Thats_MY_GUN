using System;
using System.Collections.Generic;

/// <summary>
/// 스코프는 Addressables 라벨과 별개다.
/// 이름은 호출하는 쪽이 정한다. 스코프 밖에서 올라간 에셋은 묶음에 넣지 않는다.
/// </summary>
public partial class ResourceManager
{
    /// <summary>주소 핸들을 실제로 풀기 직전에 알린다. 캐시를 먼저 비우게 한다.</summary>
    public event Action<string> AssetReleased;

    private string _currentScope;
    private readonly Dictionary<string, ScopeSet> _scopes = new();

    /// <summary>
    /// 이후 LoadAsync / PreloadLabelAsync / InstantiateAsync 결과를 이 묶음에 기록한다.
    /// BeginScope 자체는 이미 올라와 있는 핸들을 묶음에 넣지 않는다.
    /// 이후 호출이 캐시에 맞아도 이 스코프의 참조는 하나 늘어난다.
    /// </summary>
    public void BeginScope(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!_scopes.ContainsKey(name))
            _scopes[name] = new ScopeSet();

        _currentScope = name;
    }

    /// <summary>
    /// 묶음에 기록된 인스턴스를 먼저 없애고, 그다음 주소와 라벨 참조를 내린다.
    /// 다른 스코프가 아직 잡고 있는 핸들은 유지한다.
    /// </summary>
    public void ReleaseScope(string name)
    {
        if (string.IsNullOrEmpty(name) || !_scopes.TryGetValue(name, out var scope))
            return;

        if (_currentScope == name)
            _currentScope = null;

        _scopes.Remove(name);

        var instances = scope.Instances.ToArray();
        scope.Instances.Clear();
        for (var i = 0; i < instances.Length; i++)
            DestroyTrackedInstance(instances[i], releaseAsset: false);

        foreach (var pair in scope.AddressCounts)
            ReleaseAssetReferences(pair.Key, pair.Value);

        scope.AddressCounts.Clear();

        foreach (var pair in scope.LabelCounts)
            ReleaseLabelReferences(pair.Key, pair.Value);

        scope.LabelCounts.Clear();
    }

    private void TrackLoadedAsset(string address)
    {
        if (string.IsNullOrEmpty(_currentScope) || string.IsNullOrEmpty(address))
            return;

        if (!_scopes.TryGetValue(_currentScope, out var scope))
            return;

        scope.AddressCounts.TryGetValue(address, out var count);
        scope.AddressCounts[address] = count + 1;
    }

    private void TrackLoadedLabel(string label)
    {
        if (string.IsNullOrEmpty(_currentScope) || string.IsNullOrEmpty(label))
            return;

        if (!_scopes.TryGetValue(_currentScope, out var scope))
            return;

        scope.LabelCounts.TryGetValue(label, out var count);
        scope.LabelCounts[label] = count + 1;
    }

    private void UntrackOneAsset(string address)
    {
        if (TryDecreaseScopeCount(_currentScope, address, isLabel: false))
            return;

        foreach (var pair in _scopes)
        {
            if (DecreaseCount(pair.Value.AddressCounts, address, 1))
                return;
        }
    }

    private void UntrackOneLabel(string label)
    {
        if (TryDecreaseScopeCount(_currentScope, label, isLabel: true))
            return;

        foreach (var pair in _scopes)
        {
            if (DecreaseCount(pair.Value.LabelCounts, label, 1))
                return;
        }
    }

    private void UntrackAssetCount(string scopeName, string address, int count)
    {
        if (string.IsNullOrEmpty(scopeName) || count <= 0)
            return;

        if (!_scopes.TryGetValue(scopeName, out var scope))
            return;

        DecreaseCount(scope.AddressCounts, address, count);
    }

    private bool TryDecreaseScopeCount(string scopeName, string key, bool isLabel)
    {
        if (string.IsNullOrEmpty(scopeName) || !_scopes.TryGetValue(scopeName, out var scope))
            return false;

        var counts = isLabel ? scope.LabelCounts : scope.AddressCounts;
        return DecreaseCount(counts, key, 1);
    }

    private static bool DecreaseCount(Dictionary<string, int> counts, string key, int count)
    {
        if (count <= 0 || !counts.TryGetValue(key, out var current) || current <= 0)
            return false;

        current -= count;
        if (current > 0)
            counts[key] = current;
        else
            counts.Remove(key);

        return true;
    }

    private void NotifyAssetReleased(string address)
    {
        if (string.IsNullOrEmpty(address))
            return;

        AssetReleased?.Invoke(address);
    }

    private sealed class ScopeSet
    {
        public readonly Dictionary<string, int> AddressCounts = new();
        public readonly Dictionary<string, int> LabelCounts = new();
        public readonly List<TrackedInstance> Instances = new();
    }
}
