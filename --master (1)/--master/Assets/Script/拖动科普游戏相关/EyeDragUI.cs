using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EyeDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("拖拽设置")]
    [SerializeField] private RectTransform correctTrans; // 正确位置
    [SerializeField] private bool isFinished = false; // 是否已完成拼装
    [SerializeField] private float snapDistance = 100f; // 吸附距离
    [SerializeField] private int layerOrder = 0; // 拼装完成后的图层顺序
    
    [Header("大小调整设置")]
    [SerializeField] private float scaleMultiplier = 0.5f; // 未拼装时的大小倍数
    [SerializeField] private bool useScaleAdjustment = true; // 是否启用大小调整
    [SerializeField] private float scaleTransitionSpeed = 5f; // 大小变化过渡速度
    
    [Header("提示设置")]
    [SerializeField] private string partName = "眼球部件"; // 部件名称
    [SerializeField] private string description = "这是眼球的一个重要组成部分"; // 部件描述
    [SerializeField] private GameObject popupPrefab; // 弹窗预制体
    [SerializeField] private Canvas popupCanvas; // 弹窗画布
    
    [Header("NPC提示设置")]
    [SerializeField] private NPCHintSystem npcHintSystem; // NPC提示系统引用
    [SerializeField] private bool enableNPCHints = true; // 是否启用NPC提示
    
    private Vector2 startAnchoredPos;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private int originalSortingOrder;
    
    // 大小调整相关变量
    private Vector3 originalScale; // 原始大小
    private Vector3 targetScale; // 目标大小
    private bool isScaling = false; // 是否正在调整大小
    
    // 事件
    public static System.Action<EyeDragUI> onPieceCompleted;
    public static System.Action onAllPiecesCompleted;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        startAnchoredPos = rectTransform.anchoredPosition;
        originalSortingOrder = canvas.sortingOrder;
        
        // 初始化大小
        originalScale = transform.localScale;
        if (useScaleAdjustment)
        {
            targetScale = originalScale * scaleMultiplier;
            transform.localScale = targetScale;
        }
    }
    
    private void Start()
    {
        // 确保初始状态正确
        ResetPosition();
    }
    
    private void Update()
    {
        // 处理大小调整动画
        if (isScaling)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleTransitionSpeed * Time.deltaTime);
            
            // 检查是否接近目标大小
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                isScaling = false;
            }
        }
    }
    
    public void ResetPosition()
    {
        rectTransform.anchoredPosition = startAnchoredPos;
        isFinished = false;
        canvas.sortingOrder = originalSortingOrder;
        canvasGroup.alpha = 1f;
        
        // 重置为缩小状态
        if (useScaleAdjustment)
        {
            SetScale(originalScale * scaleMultiplier);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFinished) return;
        
        // 拖拽时提升层级
        canvas.sortingOrder = 1000;
        canvasGroup.alpha = 0.8f;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isFinished) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint);
        rectTransform.anchoredPosition = localPoint;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isFinished) return;
        
        float distance = Vector2.Distance(rectTransform.anchoredPosition, correctTrans.anchoredPosition);
        
        if (distance <= snapDistance)
        {
            // 拼装成功
            rectTransform.anchoredPosition = correctTrans.anchoredPosition;
            isFinished = true;
            canvas.sortingOrder = originalSortingOrder + layerOrder;
            canvasGroup.alpha = 1f;
            
            // 拼装成功后恢复正常大小
            if (useScaleAdjustment)
            {
                SetScale(originalScale);
            }
            
            // 触发完成事件
            onPieceCompleted?.Invoke(this);
            CheckAllPiecesCompleted();
        }
        else
        {
            // 检查是否在NPC区域内
            if (enableNPCHints && npcHintSystem != null && npcHintSystem.IsInAvatarArea(rectTransform))
            {
                // 在NPC区域内，显示提示并回弹
                npcHintSystem.ShowPartHint(partName);
                ResetPosition();
            }
            else
            {
                // 不在NPC区域内，直接回弹
                ResetPosition();
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // 点击未完成拼装的部件时显示提示
        if (!isFinished)
        {
            ShowPartInfo();
        }
    }
    
    private void ShowPartInfo()
    {
        if (popupPrefab == null || popupCanvas == null) return;
        
        // 创建弹窗
        GameObject popup = Instantiate(popupPrefab, popupCanvas.transform);
        popup.transform.SetAsLastSibling();
        
        // 设置弹窗内容
        var titleText = popup.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        var descText = popup.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
        var closeButton = popup.transform.Find("CloseButton")?.GetComponent<Button>();
        
        if (titleText != null)
            titleText.text = partName;
        if (descText != null)
            descText.text = description;
        if (closeButton != null)
            closeButton.onClick.AddListener(() => Destroy(popup));
        
        // 自动关闭弹窗（5秒后）
        Destroy(popup, 5f);
    }
    
    private void CheckAllPiecesCompleted()
    {
        EyeDragUI[] allPieces = FindObjectsOfType<EyeDragUI>();
        foreach (EyeDragUI piece in allPieces)
        {
            if (!piece.isFinished) return;
        }
        
        Debug.Log("所有眼球部件拼装完成！");
        onAllPiecesCompleted?.Invoke();
    }
    
    // 公共方法
    public bool IsFinished => isFinished;
    public string PartName => partName;
    public string Description => description;
    
    // 设置部件信息
    public void SetPartInfo(string name, string desc)
    {
        partName = name;
        description = desc;
    }
    
    // 设置图层顺序
    public void SetLayerOrder(int order)
    {
        layerOrder = order;
        if (isFinished)
        {
            canvas.sortingOrder = originalSortingOrder + layerOrder;
        }
    }
    
    // 设置大小（带动画）
    private void SetScale(Vector3 newScale)
    {
        targetScale = newScale;
        isScaling = true;
    }
    
    // 设置大小倍数
    public void SetScaleMultiplier(float multiplier)
    {
        scaleMultiplier = multiplier;
        if (!isFinished && useScaleAdjustment)
        {
            SetScale(originalScale * scaleMultiplier);
        }
    }
    
    // 启用/禁用大小调整
    public void SetScaleAdjustment(bool enabled)
    {
        useScaleAdjustment = enabled;
        if (enabled && !isFinished)
        {
            SetScale(originalScale * scaleMultiplier);
        }
        else if (!enabled)
        {
            transform.localScale = originalScale;
        }
    }
    
    // 获取当前大小倍数
    public float GetScaleMultiplier()
    {
        return scaleMultiplier;
    }
    
    // 获取是否启用大小调整
    public bool IsScaleAdjustmentEnabled()
    {
        return useScaleAdjustment;
    }
    
    // NPC提示相关方法
    public void SetNPCHintSystem(NPCHintSystem hintSystem)
    {
        npcHintSystem = hintSystem;
    }
    
    public void SetNPCHintsEnabled(bool enabled)
    {
        enableNPCHints = enabled;
    }
    
    public bool IsNPCHintsEnabled()
    {
        return enableNPCHints;
    }
}
