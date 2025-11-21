using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BranchButton : MonoBehaviour
{
    public string branchKey;              // 对应 branch1/branch2/branch3 等
    public TextMeshProUGUI titleText;     // 显示章节名称
    public GameObject lockIcon;           // 未解锁时显示
    public GameObject completeIcon;       // 已完成时显示
    public Button button;                 // Unity 按钮组件

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Refresh(BranchManager.BranchInfo info)
    {
        // 设置按钮文字
        if (titleText != null)
            titleText.text = info.displayName;

        // 解锁逻辑
        if (!info.unlocked)
        {
            lockIcon?.SetActive(true);
            completeIcon?.SetActive(false);
            button.interactable = false;
        }
        else
        {
            lockIcon?.SetActive(false);
            button.interactable = true;

            // 已完成
            completeIcon?.SetActive(info.completed);
        }
    }

    private void OnClick()
    {
        BranchManager.Instance.LoadBranch(branchKey);
    }
}
