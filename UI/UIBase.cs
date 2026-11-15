using UnityEngine;

/// <summary>
/// 화면에 띄우는 UI 하나. 프리팹 루트에 붙인다.
/// </summary>
public class UIBase : MonoBehaviour
{
    public UIType Type { get; private set; }
    public string Address { get; private set; }

    public void SetOpenInfo(UIType type, string address)
    {
        Type = type;
        Address = address;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
