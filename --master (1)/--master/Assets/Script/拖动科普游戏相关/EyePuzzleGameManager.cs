using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EyePuzzleGameManager : MonoBehaviour
{
    [Header("游戏设置")]
    [SerializeField] private GameObject popupPrefab; // 弹窗预制体
    [SerializeField] private Canvas popupCanvas; // 弹窗画布
    [SerializeField] private TextMeshProUGUI progressText; // 进度文本
    [SerializeField] private Button resetButton; // 重置按钮
    
    [Header("眼球部件信息")]
    [SerializeField] private List<EyePartInfo> eyeParts = new List<EyePartInfo>();
    
    private List<EyeDragUI> allPieces = new List<EyeDragUI>();
    private int completedPieces = 0;
    
    [System.Serializable]
    public class EyePartInfo
    {
        public string partName;
        public string description;
        public int layerOrder;
    }
    
    private void Start()
    {
        InitializeGame();
        SetupEventListeners();
    }
    
    private void InitializeGame()
    {
        // 获取所有拼图部件
        allPieces.Clear();
        allPieces.AddRange(FindObjectsOfType<EyeDragUI>());
        
        // 设置每个部件的信息
        for (int i = 0; i < allPieces.Count && i < eyeParts.Count; i++)
        {
            var piece = allPieces[i];
            var info = eyeParts[i];
            
            piece.SetPartInfo(info.partName, info.description);
            piece.SetLayerOrder(info.layerOrder);
        }
        
        UpdateProgress();
    }
    
    private void SetupEventListeners()
    {
        // 监听部件完成事件
        EyeDragUI.onPieceCompleted += OnPieceCompleted;
        EyeDragUI.onAllPiecesCompleted += OnAllPiecesCompleted;
        
        // 设置重置按钮
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
    }
    
    private void OnPieceCompleted(EyeDragUI piece)
    {
        completedPieces++;
        UpdateProgress();
        
        Debug.Log($"部件 '{piece.PartName}' 拼装完成！");
    }
    
    private void OnAllPiecesCompleted()
    {
        Debug.Log("恭喜！眼球拼装完成！");
        
        // 可以在这里添加完成后的逻辑
        // 比如显示完成界面、播放音效等
        if (progressText != null)
            progressText.text = "恭喜！眼球拼装完成！";
    }
    
    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"进度: {completedPieces}/{allPieces.Count}";
        }
    }
    
    public void ResetGame()
    {
        completedPieces = 0;
        
        foreach (var piece in allPieces)
        {
            piece.ResetPosition();
        }
        
        UpdateProgress();
        Debug.Log("游戏已重置");
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        EyeDragUI.onPieceCompleted -= OnPieceCompleted;
        EyeDragUI.onAllPiecesCompleted -= OnAllPiecesCompleted;
    }
    
    // 公共方法
    public void SetPopupPrefab(GameObject prefab)
    {
        popupPrefab = prefab;
    }
    
    public void SetPopupCanvas(Canvas canvas)
    {
        popupCanvas = canvas;
    }
    
    public void AddEyePartInfo(string name, string description, int layerOrder)
    {
        eyeParts.Add(new EyePartInfo
        {
            partName = name,
            description = description,
            layerOrder = layerOrder
        });
    }
}
