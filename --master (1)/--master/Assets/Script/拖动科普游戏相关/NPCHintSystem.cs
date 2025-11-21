using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NPCHintSystem : MonoBehaviour
{
    [Header("NPC组件引用")]
    [SerializeField] private Image avatarImage; // NPC贴图
    [SerializeField] private Image chatBoxImage; // 对话框图片
    [SerializeField] private TextMeshProUGUI hintText; // 提示文本组件
    
    [Header("对话框设置")]
    [SerializeField] private float dialogShowDuration = 3f; // 对话框显示时长
    [SerializeField] private float fadeInSpeed = 2f; // 淡入速度
    [SerializeField] private float fadeOutSpeed = 2f; // 淡出速度
    [SerializeField] private float scaleInSpeed = 5f; // 缩放进入速度（加快）
    [SerializeField] private float scaleOutSpeed = 3f; // 缩放退出速度
    [SerializeField] private float textDelay = 0.3f; // 文字延迟显示时间
    
    [Header("部件提示信息")]
    [SerializeField] private List<PartHintInfo> partHints = new List<PartHintInfo>();
    
    [System.Serializable]
    public class PartHintInfo
    {
        public string partName; // 部件名称（与EyeDragUI中的partName对应）
        public string hintMessage; // 提示语句
    }
    
    private bool isDialogShowing = false;
    private Coroutine currentDialogCoroutine;
    private Vector3 originalChatBoxScale;
    private Color originalChatBoxColor;
    private Color originalTextColor;
    
    private void Awake()
    {
        // 初始化对话框状态
        InitializeDialog();
    }
    
    private void InitializeDialog()
    {
        if (chatBoxImage != null)
        {
            originalChatBoxScale = chatBoxImage.transform.localScale;
            originalChatBoxColor = chatBoxImage.color;
            
            // 初始状态：隐藏对话框
            chatBoxImage.transform.localScale = Vector3.zero;
            chatBoxImage.color = new Color(originalChatBoxColor.r, originalChatBoxColor.g, originalChatBoxColor.b, 0f);
        }
        
        if (hintText != null)
        {
            originalTextColor = hintText.color;
            hintText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        }
    }
    
    private void Start()
    {
        // 订阅拖拽事件
        EyeDragUI.onPieceCompleted += OnPieceCompleted;
    }
    
    private void OnDestroy()
    {
        // 取消订阅
        EyeDragUI.onPieceCompleted -= OnPieceCompleted;
    }
    
    /// <summary>
    /// 检查拖拽的部件是否在avatar区域内
    /// </summary>
    /// <param name="draggedObject">被拖拽的对象</param>
    /// <returns>是否在avatar区域内</returns>
    public bool IsInAvatarArea(RectTransform draggedObject)
    {
        if (avatarImage == null || draggedObject == null) return false;
        
        // 获取avatar的RectTransform
        RectTransform avatarRect = avatarImage.rectTransform;
        
        // 将拖拽对象的世界坐标转换为avatar的本地坐标
        Vector3 draggedWorldPos = draggedObject.position;
        Vector3 avatarLocalPos = avatarRect.InverseTransformPoint(draggedWorldPos);
        
        // 检查是否在avatar的矩形范围内
        Rect avatarRectBounds = new Rect(
            -avatarRect.rect.width * 0.5f,
            -avatarRect.rect.height * 0.5f,
            avatarRect.rect.width,
            avatarRect.rect.height
        );
        
        return avatarRectBounds.Contains(avatarLocalPos);
    }
    
    /// <summary>
    /// 显示部件提示
    /// </summary>
    /// <param name="partName">部件名称</param>
    public void ShowPartHint(string partName)
    {
        if (isDialogShowing) return;
        
        // 首先尝试从配置中查找提示信息
        PartHintInfo hintInfo = partHints.Find(hint => hint.partName == partName);
        string hintMessage = null;
        
        if (hintInfo != null)
        {
            hintMessage = hintInfo.hintMessage;
        }
        else
        {
            // 如果配置中没有找到，尝试从EyeDragUI组件中获取description
            EyeDragUI dragUI = FindEyeDragUIByPartName(partName);
            if (dragUI != null)
            {
                hintMessage = dragUI.Description;
                Debug.Log($"使用EyeDragUI中的描述作为提示: {partName}");
            }
        }
        
        if (string.IsNullOrEmpty(hintMessage))
        {
            Debug.LogWarning($"未找到部件 '{partName}' 的提示信息");
            return;
        }
        
        // 停止之前的对话框协程
        if (currentDialogCoroutine != null)
        {
            StopCoroutine(currentDialogCoroutine);
        }
        
        // 开始显示对话框
        currentDialogCoroutine = StartCoroutine(ShowDialogCoroutine(hintMessage));
    }
    
    /// <summary>
    /// 根据部件名称查找对应的EyeDragUI组件
    /// </summary>
    private EyeDragUI FindEyeDragUIByPartName(string partName)
    {
        EyeDragUI[] allDragUIs = FindObjectsOfType<EyeDragUI>();
        foreach (var dragUI in allDragUIs)
        {
            if (dragUI.PartName == partName)
            {
                return dragUI;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 显示对话框的协程
    /// </summary>
    private IEnumerator ShowDialogCoroutine(string message)
    {
        isDialogShowing = true;
        
        // 先隐藏文字
        if (hintText != null)
        {
            hintText.text = message;
            hintText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        }
        
        // 先让对话框弹出（不包含文字）
        yield return StartCoroutine(AnimateDialogIn());
        
        // 等待一小段时间，让对话框完全稳定
        yield return new WaitForSeconds(textDelay);
        
        // 然后显示文字
        yield return StartCoroutine(AnimateTextIn());
        
        // 等待指定时间
        yield return new WaitForSeconds(dialogShowDuration);
        
        // 先隐藏文字
        yield return StartCoroutine(AnimateTextOut());
        
        // 然后隐藏对话框
        yield return StartCoroutine(AnimateDialogOut());
        
        isDialogShowing = false;
        currentDialogCoroutine = null;
    }
    
    /// <summary>
    /// 对话框进入动画（只处理对话框，不处理文字）
    /// </summary>
    private IEnumerator AnimateDialogIn()
    {
        float elapsedTime = 0f;
        float duration = 1f / scaleInSpeed; // 使用更快的缩放速度
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 缩放动画
            if (chatBoxImage != null)
            {
                Vector3 currentScale = Vector3.Lerp(Vector3.zero, originalChatBoxScale, progress);
                chatBoxImage.transform.localScale = currentScale;
            }
            
            // 透明度动画
            float alpha = Mathf.Lerp(0f, 1f, progress);
            
            if (chatBoxImage != null)
            {
                Color currentColor = chatBoxImage.color;
                currentColor.a = alpha;
                chatBoxImage.color = currentColor;
            }
            
            yield return null;
        }
        
        // 确保最终状态正确
        if (chatBoxImage != null)
        {
            chatBoxImage.transform.localScale = originalChatBoxScale;
            chatBoxImage.color = originalChatBoxColor;
        }
    }
    
    /// <summary>
    /// 文字进入动画
    /// </summary>
    private IEnumerator AnimateTextIn()
    {
        if (hintText == null) yield break;
        
        float elapsedTime = 0f;
        float duration = 1f / fadeInSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            float alpha = Mathf.Lerp(0f, 1f, progress);
            Color currentTextColor = hintText.color;
            currentTextColor.a = alpha;
            hintText.color = currentTextColor;
            
            yield return null;
        }
        
        // 确保最终状态正确
        hintText.color = originalTextColor;
    }
    
    /// <summary>
    /// 文字退出动画
    /// </summary>
    private IEnumerator AnimateTextOut()
    {
        if (hintText == null) yield break;
        
        float elapsedTime = 0f;
        float duration = 1f / fadeOutSpeed;
        
        Color startTextColor = hintText.color;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            float alpha = Mathf.Lerp(1f, 0f, progress);
            Color currentTextColor = startTextColor;
            currentTextColor.a = alpha;
            hintText.color = currentTextColor;
            
            yield return null;
        }
        
        // 确保最终状态正确
        hintText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 0f);
    }
    
    /// <summary>
    /// 对话框退出动画（只处理对话框，不处理文字）
    /// </summary>
    private IEnumerator AnimateDialogOut()
    {
        float elapsedTime = 0f;
        float duration = 1f / scaleOutSpeed;
        
        Vector3 startScale = chatBoxImage != null ? chatBoxImage.transform.localScale : Vector3.one;
        Color startChatBoxColor = chatBoxImage != null ? chatBoxImage.color : Color.white;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 缩放动画
            if (chatBoxImage != null)
            {
                Vector3 currentScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                chatBoxImage.transform.localScale = currentScale;
            }
            
            // 透明度动画
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            if (chatBoxImage != null)
            {
                Color currentColor = startChatBoxColor;
                currentColor.a = alpha;
                chatBoxImage.color = currentColor;
            }
            
            yield return null;
        }
        
        // 确保最终状态正确
        if (chatBoxImage != null)
        {
            chatBoxImage.transform.localScale = Vector3.zero;
            chatBoxImage.color = new Color(startChatBoxColor.r, startChatBoxColor.g, startChatBoxColor.b, 0f);
        }
    }
    
    /// <summary>
    /// 部件完成时的回调
    /// </summary>
    private void OnPieceCompleted(EyeDragUI piece)
    {
        // 如果对话框正在显示，可以选择隐藏它
        if (isDialogShowing)
        {
            HideDialog();
        }
    }
    
    /// <summary>
    /// 立即隐藏对话框
    /// </summary>
    public void HideDialog()
    {
        if (currentDialogCoroutine != null)
        {
            StopCoroutine(currentDialogCoroutine);
            currentDialogCoroutine = null;
        }
        
        StartCoroutine(AnimateDialogOut());
        isDialogShowing = false;
    }
    
    /// <summary>
    /// 添加部件提示信息
    /// </summary>
    public void AddPartHint(string partName, string hintMessage)
    {
        // 检查是否已存在
        PartHintInfo existingHint = partHints.Find(hint => hint.partName == partName);
        if (existingHint != null)
        {
            existingHint.hintMessage = hintMessage;
        }
        else
        {
            partHints.Add(new PartHintInfo
            {
                partName = partName,
                hintMessage = hintMessage
            });
        }
    }
    
    /// <summary>
    /// 设置对话框显示时长
    /// </summary>
    public void SetDialogDuration(float duration)
    {
        dialogShowDuration = duration;
    }
    
    /// <summary>
    /// 设置动画速度
    /// </summary>
    public void SetAnimationSpeeds(float fadeIn, float fadeOut, float scaleIn, float scaleOut)
    {
        fadeInSpeed = fadeIn;
        fadeOutSpeed = fadeOut;
        scaleInSpeed = scaleIn;
        scaleOutSpeed = scaleOut;
    }
    
    /// <summary>
    /// 设置文字延迟显示时间
    /// </summary>
    public void SetTextDelay(float delay)
    {
        textDelay = delay;
    }
    
    // 公共属性
    public bool IsDialogShowing => isDialogShowing;
    public List<PartHintInfo> PartHints => partHints;
}
