using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class BranchManager : MonoBehaviour
{
    public static BranchManager Instance;

    [Serializable]
    public class BranchInfo
    {
        public string key;         // "branch1"
        public string fileName;    // "branch1.xlsx"
        public string displayName; // "单元1"
        public bool unlocked;      // 是否解锁
        public bool completed;     // 是否已完成
    }

    [Serializable]
    private class SaveData
    {
        public List<string> unlocked = new List<string>();
        public List<string> completed = new List<string>();
    }

    public BranchInfo[] branches =
    {
        new BranchInfo { key = "branch1", fileName = "branch1.xlsx", displayName = "第一章", unlocked = true },
        new BranchInfo { key = "branch2", fileName = "branch2.xlsx", displayName = "第二章", unlocked = false },
        new BranchInfo { key = "branch3", fileName = "branch3.xlsx", displayName = "第三章", unlocked = false },
        new BranchInfo { key = "branch4", fileName = "branch4.xlsx", displayName = "第四章", unlocked = false },
        new BranchInfo { key = "branch5", fileName = "branch5.xlsx", displayName = "第五章", unlocked = false },
    };

    private Dictionary<string, BranchInfo> branchDict = new Dictionary<string, BranchInfo>();
    private string savePath;
    private string currentBranchKey = "";
    public bool isBranchMode = false;

    // 重要：在 Inspector 中拖入你的章节选择面板
    public GameObject branchSelectionPanel;
    // 分支面板背景图片组件（可选，如果面板有背景Image组件）
    public Image branchPanelBackgroundImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        foreach (var b in branches)
        {
            branchDict[b.key] = b;
        }

        savePath = Path.Combine(Application.persistentDataPath, "branch_save.json");
        LoadProgress();

        // ⭐ 重要修改：开始时隐藏选择面板
        if (branchSelectionPanel != null)
        {
            branchSelectionPanel.SetActive(false);
        }

        Debug.Log("章节选择面板初始状态：隐藏");
        if (branchSelectionPanel == null)
        {
            Debug.LogError("❌ BranchManager: branchSelectionPanel 未分配！请在 Inspector 中拖入面板");
        }
        else
        {
            Debug.Log("✅ BranchManager: 面板引用正常，初始隐藏");
            branchSelectionPanel.SetActive(false);
        }
    }
    public void LoadProgress()
    {
        if (!File.Exists(savePath)) return;

        try
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 重置所有状态
            foreach (var branch in branches)
            {
                branch.unlocked = false;
                branch.completed = false;
            }

            // 应用存档数据
            foreach (var key in data.unlocked)
            {
                if (branchDict.ContainsKey(key))
                    branchDict[key].unlocked = true;
            }

            foreach (var key in data.completed)
            {
                if (branchDict.ContainsKey(key))
                    branchDict[key].completed = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"读取存档失败: {e.Message}");
        }
    }

    public void SaveProgress()
    {
        try
        {
            SaveData data = new SaveData();

            foreach (var b in branches)
            {
                if (b.unlocked) data.unlocked.Add(b.key);
                if (b.completed) data.completed.Add(b.key);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"进度已保存: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存存档失败: {e.Message}");
        }
    }

    // 刷新所有按钮状态
    public void RefreshAllButtons()
    {
        var buttons = FindObjectsOfType<BranchButton>();
        foreach (var btn in buttons)
        {
            if (branchDict.ContainsKey(btn.branchKey))
            {
                btn.Refresh(branchDict[btn.branchKey]);
            }
        }
        Debug.Log($"刷新了 {buttons.Length} 个按钮");
    }

    // 解锁分支
    public void Unlock(string branchKey)
    {
        if (branchDict.ContainsKey(branchKey) && !branchDict[branchKey].unlocked)
        {
            branchDict[branchKey].unlocked = true;
            SaveProgress();
            RefreshAllButtons(); // 立即刷新UI
            Debug.Log($"【BranchManager】解锁分支: {branchKey}");
        }
    }

    // 加载分支剧情
    public void LoadBranch(string branchKey)
    {
        if (!branchDict.ContainsKey(branchKey))
        {
            Debug.LogError($"分支不存在: {branchKey}");
            return;
        }

        BranchInfo info = branchDict[branchKey];

        if (!info.unlocked)
        {
            Debug.LogWarning($"分支未解锁: {branchKey}");
            return;
        }

        currentBranchKey = branchKey;
        isBranchMode = true;

        // 隐藏选择面板
        HideBranchSelection();

        // ⭐ 修复路径拼接
        string fullPath = Path.Combine(Application.streamingAssetsPath, "Story", info.fileName);
        Debug.Log($"📁 完整文件路径: {fullPath}");

        // 确保 VNManager 存在
        if (VNManagerNew.Instance != null)
        {
            VNManagerNew.Instance.LoadStoryFile(fullPath);
        }
        else
        {
            Debug.LogError("❌ VNManagerNew.Instance 为 null");
        }
     
    }
    // 完成当前分支
    public void CompleteCurrentBranch()
    {
        if (string.IsNullOrEmpty(currentBranchKey))
        {
            Debug.LogWarning("CompleteCurrentBranch: 没有正在进行的分支");
            return;
        }

        if (branchDict.ContainsKey(currentBranchKey))
        {
            branchDict[currentBranchKey].completed = true;
            SaveProgress();
            Debug.Log($"【BranchManager】完成分支: {currentBranchKey}");
        }

        // 重置状态
        isBranchMode = false;
        currentBranchKey = "";

        // ⭐ 重要：完成分支后自动显示选择面板
        ShowBranchSelection();
    }

    /// <summary>
    /// 显示章节选择面板
    /// </summary>
    /// <summary>
    /// 显示章节选择面板
    /// </summary>
    public void ShowBranchSelection()
    {
        Debug.Log("🔄 BranchManager.ShowBranchSelection() 被调用");

        if (branchSelectionPanel == null)
        {
            Debug.LogError("❌ branchSelectionPanel 为 null，无法显示");
            return;
        }

        branchSelectionPanel.SetActive(true);
        Debug.Log("✅ 章节选择面板已显示");

        // 设置分支面板背景（从Excel读取）
        SetBranchPanelBackground();

        RefreshAllButtons();
    }

    /// <summary>
    /// 设置分支面板背景图片
    /// </summary>
    private void SetBranchPanelBackground()
    {
        if (branchPanelBackgroundImage == null)
        {
            Debug.LogWarning("⚠️ branchPanelBackgroundImage 未绑定，跳过背景设置");
            return;
        }

        // 从VNManager获取当前分支面板背景文件名
        string backgroundFileName = "";
        if (VNManagerNew.Instance != null)
        {
            backgroundFileName = VNManagerNew.Instance.GetCurrentBranchPanelBackground();
        }

        // 如果文件名为空，隐藏背景或使用默认背景
        if (string.IsNullOrEmpty(backgroundFileName))
        {
            Debug.Log("📝 分支面板背景文件名为空，保持当前背景或隐藏");
            // 可以选择隐藏背景或保持原样
            // branchPanelBackgroundImage.gameObject.SetActive(false);
            return;
        }

        // 加载背景图片
        string path = $"{ConstantsNew.BRANCH_PANEL_BACKGROUND_PATH}{backgroundFileName}";
        // 移除可能的文件扩展名
        path = path.Replace(".png", "").Replace(".jpg", "").Replace(".jpeg", "");
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite != null)
        {
            branchPanelBackgroundImage.sprite = sprite;
            branchPanelBackgroundImage.gameObject.SetActive(true);
            Debug.Log($"✅ 成功加载分支面板背景: {path}");
        }
        else
        {
            Debug.LogError($"❌ 分支面板背景加载失败！\n" +
                          $"   - 检查路径: Resources/{path}\n" +
                          $"   - 检查文件名: {backgroundFileName}\n" +
                          $"   - 确保图片在 Resources/BranchPanel/ 目录下\n" +
                          $"   - 确保图片已导入Unity（不是文件夹中的原始文件）");
        }
    }

    /// <summary>
    /// 隐藏章节选择面板
    /// </summary>
    public void HideBranchSelection()
    {
        Debug.Log("🔄 BranchManager.HideBranchSelection() 被调用");

        if (branchSelectionPanel != null)
        {
            branchSelectionPanel.SetActive(false);
            Debug.Log("✅ 章节选择面板已隐藏");
        }
    }
    // 获取分支信息（供其他脚本使用）
    public BranchInfo GetBranchInfo(string key)
    {
        return branchDict.ContainsKey(key) ? branchDict[key] : null;
    }
}