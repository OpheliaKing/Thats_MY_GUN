using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// UIBase를 스택으로 연다.
/// Common은 지금 켜져 있는 UI를 끄고 위에 띄운다. Popup은 아래 UI를 유지한 채 위에 띄운다.
/// </summary>
public class UIManager : ManagerBase
{
    private readonly List<OpenedUI> _stack = new();
    private Canvas _canvas;
    private int _generation;

    public async Task<UIBase> Show(string address, UIType type)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[UIManager] address가 비어 있습니다.", this);
            return null;
        }

        var resources = GameManager.Instance != null ? GameManager.Instance.ResourceManager : null;
        if (resources == null)
        {
            Debug.LogError("[UIManager] ResourceManager가 없습니다.", this);
            return null;
        }

        EnsureCanvas();
        var generation = _generation;
        var instance = await resources.InstantiateAsync(address, _canvas.transform, startInactive: true);
        if (this == null)
        {
            if (instance != null)
                resources.ReleaseInstance(instance);
            return null;
        }

        if (generation != _generation || instance == null)
        {
            if (instance != null)
                resources.ReleaseInstance(instance);
            return null;
        }

        if (instance.GetComponent<RectTransform>() == null)
        {
            Debug.LogError($"[UIManager] RectTransform이 없습니다. address={address}", instance);
            resources.ReleaseInstance(instance);
            return null;
        }

        var ui = instance.GetComponent<UIBase>();
        if (ui == null)
            ui = instance.AddComponent<UIBase>();

        ui.SetOpenInfo(type, address);
        var opened = new OpenedUI { UI = ui };
        if (type == UIType.Common)
            HideOpenUIs(opened);

        _stack.Add(opened);
        ui.transform.SetAsLastSibling();
        ui.SetVisible(true);
        return ui;
    }

    /// <summary>스택 맨 위를 닫는다. Common이었으면 그것이 껐던 UI를 다시 켠다.</summary>
    public void Close()
    {
        if (_stack.Count == 0)
            return;

        var opened = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);
        RestoreHidden(opened);
        Release(opened.UI);
    }

    public void CloseAll()
    {
        _generation++;
        for (var i = _stack.Count - 1; i >= 0; i--)
            Release(_stack[i].UI);

        _stack.Clear();
    }

    private void HideOpenUIs(OpenedUI opened)
    {
        for (var i = 0; i < _stack.Count; i++)
        {
            var below = _stack[i].UI;
            if (below == null || !below.gameObject.activeSelf)
                continue;

            below.SetVisible(false);
            opened.Hidden.Add(below);
        }
    }

    private static void RestoreHidden(OpenedUI opened)
    {
        if (opened.UI == null || opened.UI.Type != UIType.Common)
            return;

        for (var i = 0; i < opened.Hidden.Count; i++)
        {
            if (opened.Hidden[i] != null)
                opened.Hidden[i].SetVisible(true);
        }
    }

    private void Release(UIBase ui)
    {
        if (ui == null)
            return;

        var resources = GameManager.Instance != null ? GameManager.Instance.ResourceManager : null;
        if (resources == null)
        {
            Destroy(ui.gameObject);
            return;
        }

        resources.ReleaseInstance(ui.gameObject);
    }

    private void EnsureCanvas()
    {
        if (_canvas != null)
            return;

        var canvasObject = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 0;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventObject.transform.SetParent(transform, false);
    }

    private sealed class OpenedUI
    {
        public UIBase UI;
        public readonly List<UIBase> Hidden = new();
    }
}
