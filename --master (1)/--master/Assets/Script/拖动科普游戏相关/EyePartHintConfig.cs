using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EyePartHintConfig : MonoBehaviour
{
    [Header("部件提示配置")]
    [SerializeField] private List<PartHintData> partHints = new List<PartHintData>();
    
    [System.Serializable]
    public class PartHintData
    {
        [Header("部件信息")]
        public string partName; // 部件名称（必须与EyeDragUI中的partName完全一致）
        
        [Header("提示内容")]
        [TextArea(2, 4)]
        public string hintMessage; // 提示语句
        
        [Header("显示设置")]
        public float displayDuration = 3f; // 显示时长（秒）
        public bool enableHint = true; // 是否启用此部件的提示
    }
    
    [Header("默认设置")]
    [SerializeField] private float defaultDisplayDuration = 3f;
    [SerializeField] private bool enableAllHints = true;
    
    private NPCHintSystem npcHintSystem;
    
    private void Start()
    {
        // 查找NPC提示系统
        npcHintSystem = FindObjectOfType<NPCHintSystem>();
        if (npcHintSystem == null)
        {
            Debug.LogError("未找到NPCHintSystem组件！请确保场景中存在NPCHintSystem脚本。");
            return;
        }
        
        // 初始化提示信息
        InitializeHints();
        
        // 设置所有EyeDragUI组件的NPC提示系统引用
        SetupDragUIReferences();
    }
    
    private void InitializeHints()
    {
        if (npcHintSystem == null) return;
        
        // 添加自定义提示信息（用于覆盖EyeDragUI中的默认描述）
        foreach (var hintData in partHints)
        {
            if (hintData.enableHint && !string.IsNullOrEmpty(hintData.partName) && !string.IsNullOrEmpty(hintData.hintMessage))
            {
                npcHintSystem.AddPartHint(hintData.partName, hintData.hintMessage);
            }
        }
        
        // 设置默认显示时长
        npcHintSystem.SetDialogDuration(defaultDisplayDuration);
        
        Debug.Log($"NPC提示系统初始化完成。配置了 {partHints.Count} 个自定义提示，其余将使用EyeDragUI中的描述。");
    }
    
    private void SetupDragUIReferences()
    {
        EyeDragUI[] allDragUIs = FindObjectsOfType<EyeDragUI>();
        
        foreach (var dragUI in allDragUIs)
        {
            // 设置NPC提示系统引用
            dragUI.SetNPCHintSystem(npcHintSystem);
            
            // 设置是否启用NPC提示
            dragUI.SetNPCHintsEnabled(enableAllHints);
        }
    }
    
    /// <summary>
    /// 添加或更新部件提示信息
    /// </summary>
    public void AddOrUpdatePartHint(string partName, string hintMessage, float duration = 3f, bool enable = true)
    {
        // 查找是否已存在
        PartHintData existingHint = partHints.Find(hint => hint.partName == partName);
        
        if (existingHint != null)
        {
            // 更新现有提示
            existingHint.hintMessage = hintMessage;
            existingHint.displayDuration = duration;
            existingHint.enableHint = enable;
        }
        else
        {
            // 添加新提示
            partHints.Add(new PartHintData
            {
                partName = partName,
                hintMessage = hintMessage,
                displayDuration = duration,
                enableHint = enable
            });
        }
        
        // 如果NPC提示系统已初始化，立即更新
        if (npcHintSystem != null && enable)
        {
            npcHintSystem.AddPartHint(partName, hintMessage);
        }
    }
    
    /// <summary>
    /// 移除部件提示信息
    /// </summary>
    public void RemovePartHint(string partName)
    {
        partHints.RemoveAll(hint => hint.partName == partName);
    }
    
    /// <summary>
    /// 启用/禁用特定部件的提示
    /// </summary>
    public void SetPartHintEnabled(string partName, bool enabled)
    {
        PartHintData hint = partHints.Find(h => h.partName == partName);
        if (hint != null)
        {
            hint.enableHint = enabled;
        }
    }
    
    /// <summary>
    /// 启用/禁用所有提示
    /// </summary>
    public void SetAllHintsEnabled(bool enabled)
    {
        enableAllHints = enabled;
        
        EyeDragUI[] allDragUIs = FindObjectsOfType<EyeDragUI>();
        foreach (var dragUI in allDragUIs)
        {
            dragUI.SetNPCHintsEnabled(enabled);
        }
    }
    
    /// <summary>
    /// 设置默认显示时长
    /// </summary>
    public void SetDefaultDisplayDuration(float duration)
    {
        defaultDisplayDuration = duration;
        if (npcHintSystem != null)
        {
            npcHintSystem.SetDialogDuration(duration);
        }
    }
    
    /// <summary>
    /// 获取部件提示信息
    /// </summary>
    public PartHintData GetPartHint(string partName)
    {
        return partHints.Find(hint => hint.partName == partName);
    }
    
    /// <summary>
    /// 获取所有部件提示信息
    /// </summary>
    public List<PartHintData> GetAllPartHints()
    {
        return new List<PartHintData>(partHints);
    }
    
    /// <summary>
    /// 重新初始化所有提示（用于运行时更新）
    /// </summary>
    public void RefreshHints()
    {
        if (npcHintSystem != null)
        {
            InitializeHints();
        }
    }
    
    // 在Inspector中显示帮助信息
    private void OnValidate()
    {
        // 确保部件名称不为空
        foreach (var hint in partHints)
        {
            if (string.IsNullOrEmpty(hint.partName))
            {
                Debug.LogWarning("发现空的部件名称，请填写部件名称！");
            }
            
            if (string.IsNullOrEmpty(hint.hintMessage))
            {
                Debug.LogWarning($"部件 '{hint.partName}' 的提示信息为空，请填写提示信息！");
            }
        }
    }
}
