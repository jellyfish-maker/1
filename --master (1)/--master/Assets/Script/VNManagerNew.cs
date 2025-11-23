using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
using UnityEngine.EventSystems;
using System;
using static ExcelReaderNew;

/// <summary>
/// 视觉小说管理器 - 纯净框架版本
/// 保留核心功能：Excel读取、点击切换、UI设置、背景淡入淡出、打字机效果
/// </summary>
public class VNManagerNew : MonoBehaviour
{
    public static VNManagerNew Instance;

    [Header("UI组件")]
    public TextMeshProUGUI protagonistName;      // 右侧主角名字
    public TextMeshProUGUI otherCharacterName;   // 左侧其他角色名字
    public TextMeshProUGUI speakingContent;      // 对话内容
    public TypeWriterEffectNew typeWriterEffect; // 打字机效果组件

    public Image protagonistAvatar;  // 右侧主角头像
    public Image otherAvatar;        // 左侧其他角色头像
    public Image dialogueBox;        // 对话框背景
    public Image avatarImage1;       // 立绘图片（独立于对话框和文字）
    public AudioSource backgroundMusic; // 背景音乐
    [Header("背景叠加设置")]
    public RectTransform backgroundLayerRoot;
    public Image backgroundLayerTemplate;
    [Range(1, 10)] public int maxBackgroundLayers = 10;
    public float fadeDuration = 0.5f; // 背景淡入淡出时长
    private readonly List<Image> activeBackgroundLayers = new List<Image>();
    private readonly List<string> currentBackgroundSequence = new List<string>();
    private int currentBackgroundSequenceIndex = -1;

    // 配置路径
    private string storyPath = ConstantsNew.STORY_PATH;
    private string defaultStoryFileName = ConstantsNew.DEFAULT_STORY_FILE_NAME;

    // 数据存储
    private List<ExcelReaderNew.ExcelData> storyData;
    private int currentLine = ConstantsNew.DEFAULT_START_LINE;

    // 输入防抖
    private float _lastClickTime;
    public float clickCooldown = 0.3f;

    private string lastDisplayedCharacter = "";
    private bool lastAvatarDisplayed = false;
    private bool lastCharacterIsProtagonist = false;
    private string currentBranchPanelBackground = "";  // 当前分支面板背景

    // 事件
    public UnityEvent onStoryEnd;

    /// <summary>
    /// 数据是否已加载
    /// </summary>
    public bool IsDataReady
    {
        get
        {
            return storyData != null && storyData.Count > 0;
        }
    }

    /// <summary>
    /// 获取当前Excel数据
    /// </summary>
    public ExcelReaderNew.ExcelData GetCurrentExcelData()
    {
        if (storyData != null && currentLine >= 0 && currentLine < storyData.Count)
            return storyData[currentLine];
        return default(ExcelReaderNew.ExcelData);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 初始隐藏所有UI
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
            if (speakingContent != null) speakingContent.gameObject.SetActive(false);
            if (protagonistAvatar != null) protagonistAvatar.gameObject.SetActive(false);
            if (otherAvatar != null) otherAvatar.gameObject.SetActive(false);
            if (avatarImage1 != null) avatarImage1.gameObject.SetActive(false);
            if (backgroundLayerTemplate != null)
            {
                backgroundLayerTemplate.gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 异步加载Excel数据
        string path = Path.Combine(Application.streamingAssetsPath, "Story", ConstantsNew.DEFAULT_STORY_FILE_NAME);
        StartCoroutine(ExcelReaderNew.ReadExcelAsync(path, result =>
        {
            if (result == null)
            {
                Debug.LogError("Excel加载失败！");
                return;
            }

            storyData = result;
            currentLine = ConstantsNew.DEFAULT_START_LINE;
            DisplayNextLine();
        }));
    }

    void Update()
    {
        // ESC键可以用于调试（可选）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 可以添加暂停菜单等
        }

        // 处理点击（鼠标或触屏）
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleClick(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleClick(Input.mousePosition);
        }
    }

    /// <summary>
    /// 处理点击事件
    /// </summary>
    private void HandleClick(Vector2 screenPosition)
    {
        // 防抖检查
        if (Time.unscaledTime - _lastClickTime < clickCooldown) return;
        _lastClickTime = Time.unscaledTime;

        // 如果点击在UI按钮上则忽略
        if (IsPointerOverUI()) return;

        // 如果正在打字，立即完成
        if (typeWriterEffect != null && typeWriterEffect.IsTyping())
        {
            typeWriterEffect.CompleteLine();
            return;
        }

        // 背景多图：若当前行还有未展示的图片，优先切换图片而非换行
        if (TryAdvanceBackgroundSequence())
        {
            return;
        }

        // 显示下一行
        DisplayNextLine();
    }

    /// <summary>
    /// 检查是否点击在UI上
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Button>() != null ||
                result.gameObject.GetComponent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }


    public void DisplayNextLine()
    {
        // 数据检查
        if (storyData == null)
        {
            Debug.LogError("storyData 未加载！");
            return;
        }

        if (typeWriterEffect == null)
        {
            Debug.LogError("typeWriterEffect 未初始化！");
            return;
        }

        if (speakingContent == null)
        {
            Debug.LogError("speakingContent 未初始化！");
            return;
        }

        // 检查是否到达结尾
        if (currentLine >= storyData.Count)
        {
            Debug.Log("剧情结束");
            onStoryEnd?.Invoke();
            return;
        }

        // 显示当前行
        DisplayThisLine();

        // 移动到下一行
        currentLine++;
    }

    /// <summary>
    /// 显示当前行
    /// </summary>
    private void DisplayThisLine()
    {
        if (storyData == null || currentLine < 0 || currentLine >= storyData.Count)
        {
            Debug.LogError($"无效的数据索引！currentLine={currentLine}");
            return;
        }
        // ⭐ 添加完整的安全检查
        if (storyData == null)
        {
            Debug.LogError("❌ storyData 为 null");
            return;
        }

        if (currentLine < 0 || currentLine >= storyData.Count)
        {
            Debug.LogError($"❌ 行索引越界: {currentLine}/{storyData.Count}");
            return;
        }

        if (typeWriterEffect == null)
        {
            Debug.LogError("❌ typeWriterEffect 为 null");
            return;
        }

        if (speakingContent == null)
        {
            Debug.LogError("❌ speakingContent 为 null");
            return;
        }

        Debug.Log($"📖 准备显示第 {currentLine} 行");

        var data = storyData[currentLine];
        // ⭐ 修复：确保 data.content 不为 null
        if (data.content == null)
        {
            Debug.LogWarning($"⚠️ 第 {currentLine} 行的 content 为 null，设置为空字符串");
            data.content = "";
        }

        // 调试：打印AvatarImage1的值
        Debug.Log($"🔍 第 {currentLine} 行的 AvatarImage1 值: '{data.AvatarImage1}' (是否为null: {data.AvatarImage1 == null}, 是否为空: {string.IsNullOrEmpty(data.AvatarImage1)})");

        // 保存分支面板背景（如果当前行有值）
        if (!string.IsNullOrEmpty(data.BranchPanelBackground))
        {
            currentBranchPanelBackground = data.BranchPanelBackground;
            Debug.Log($"📋 保存分支面板背景: {currentBranchPanelBackground}");
        }

        // -------- 显示立绘（AvatarImage1，独立于对话框和文字，优先处理） --------
        DisplayAvatarImage1(data.AvatarImage1);

        // -------- Command 处理 --------
        // 注意：这里使用的是上面声明的 data 变量，而不是不存在的 line，也不是 storyData（后者是 List）
        // 请确保 ExcelReaderNew.ExcelData 中命令字段确实叫 "Command"（大小写敏感）
        if (!string.IsNullOrEmpty(data.Command))
        {
            bool stop = HandleCommand(data.Command);
            if (stop)
            {
                return; // 有些 command（如 EndBranch / JumpTo）会立即停止当前行的常规显示流程
            }
        }

        // -------- 清屏处理 --------
        if (data.ClearScreen)
        {
            ClearScreenUI();
            // 清屏后重新显示AvatarImage1（如果存在）
            DisplayAvatarImage1(data.AvatarImage1);
            return;
        }

        bool canReuseCharacter = lastAvatarDisplayed && !string.IsNullOrEmpty(lastDisplayedCharacter);
        bool shouldReuseCharacter = string.IsNullOrEmpty(data.AvatarImageFileName) &&
                                    string.IsNullOrEmpty(data.speaker) &&
                                    canReuseCharacter;

        if (!shouldReuseCharacter)
        {
            // 进入本行前先默认隐藏对话框，只有真正显示立绘后才重新打开
            SetDialogueVisibility(false);
        }

        // 判断是否为过渡行（speaker为"Transition"或"过渡"）
        bool isTransition = !string.IsNullOrEmpty(data.speaker) &&
                           (data.speaker.Equals("Transition", StringComparison.OrdinalIgnoreCase) ||
                            data.speaker.Equals("过渡", StringComparison.OrdinalIgnoreCase));

        if (isTransition)
        {
            dialogueBox.gameObject.SetActive(false);
            speakingContent.gameObject.SetActive(false);
            // 处理背景切换（如果有）
            if (!string.IsNullOrEmpty(data.backgroundImageFileName))
            {
                SetupBackgroundSequence(data.backgroundImageFileName);
            }
            // 过渡行也可以显示AvatarImage1
            DisplayAvatarImage1(data.AvatarImage1);
            return;
        }

        // 判断是否为旁白
        bool isNarration = !shouldReuseCharacter &&
                           (string.IsNullOrEmpty(data.speaker) || data.speaker == "Narrator");

        if (!shouldReuseCharacter)
        {
            // 清除头像（如果角色切换）
            if (!string.IsNullOrEmpty(data.speaker) && data.speaker != lastDisplayedCharacter)
            {
                protagonistAvatar.gameObject.SetActive(false);
                otherAvatar.gameObject.SetActive(false);
                protagonistAvatar.sprite = null;
                otherAvatar.sprite = null;
            }
            lastDisplayedCharacter = string.IsNullOrEmpty(data.speaker) ? lastDisplayedCharacter : data.speaker;
        }

        // 处理换行（旁白首行缩进）
        string displayText = isNarration && !data.content.StartsWith("\n")
            ? "\n" + data.content
            : data.content;

        // 设置文本颜色
        if (isNarration)
        {
            speakingContent.color = Color.white;
        }
        else
        {
            speakingContent.color = Color.black;
        }

        // 显示立绘
        bool avatarDisplayed = shouldReuseCharacter ? lastAvatarDisplayed : false;

        if (!shouldReuseCharacter)
        {
            protagonistAvatar.gameObject.SetActive(false);
            otherAvatar.gameObject.SetActive(false);

            if (!isNarration && !string.IsNullOrEmpty(data.AvatarImageFileName))
            {
                Image targetAvatar = data.IsProtagonist ? protagonistAvatar : otherAvatar;
                string folder = data.IsProtagonist ? ConstantsNew.PROTAGONIST_PATH : ConstantsNew.CHARACTERS_PATH;
                string path = $"{folder}{data.AvatarImageFileName}";

                Sprite sprite = Resources.Load<Sprite>(path);

                if (sprite != null)
                {
                    targetAvatar.sprite = sprite;
                    targetAvatar.gameObject.SetActive(true);
                    avatarDisplayed = true;
                    lastCharacterIsProtagonist = data.IsProtagonist;
                    lastAvatarDisplayed = true;
                    lastDisplayedCharacter = string.IsNullOrEmpty(data.speaker) ? lastDisplayedCharacter : data.speaker;
                    Debug.Log($"成功加载立绘: {path}");
                }
                else
                {
                    Debug.LogError($"立绘加载失败！检查路径: {path}");
                }
            }
            else
            {
                lastAvatarDisplayed = false;
            }

            SetDialogueVisibility(avatarDisplayed);
        }
        else
        {
            SetDialogueVisibility(true);
        }

        // 设置名字，仅在有立绘时显示
        protagonistName.gameObject.SetActive(false);
        otherCharacterName.gameObject.SetActive(false);

        if ((avatarDisplayed && !isNarration) || shouldReuseCharacter)
        {
            TextMeshProUGUI targetName = lastCharacterIsProtagonist ? protagonistName : otherCharacterName;
            targetName.text = string.IsNullOrEmpty(data.speaker) ? lastDisplayedCharacter : data.speaker;
            targetName.gameObject.SetActive(true);
            lastCharacterIsProtagonist = string.IsNullOrEmpty(data.speaker) ? lastCharacterIsProtagonist : data.IsProtagonist;
        }

        lastAvatarDisplayed = avatarDisplayed;

        // 调整对话框位置（主角在右，其他角色在左）
        if (!isNarration && dialogueBox != null)
        {
            RectTransform dialogueBoxRect = dialogueBox.GetComponent<RectTransform>();
            if (dialogueBoxRect != null)
            {
                Vector3 scale = dialogueBoxRect.localScale;
                scale.x = data.IsProtagonist ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                dialogueBoxRect.localScale = scale;
            }
        }

        // 更新背景（支持多图顺序播放）
        SetupBackgroundSequence(data.backgroundImageFileName);

        // 更新背景音乐
        if (!string.IsNullOrEmpty(data.backgroundMusicFileName))
        {
            UpdateBackgroundMusic(data.backgroundMusicFileName);
        }

        // 启动打字机效果
        typeWriterEffect.StartTyping(displayText);
    }

    /// <summary>
    /// 初始化当前行的背景播放序列
    /// </summary>
    private void SetupBackgroundSequence(string backgroundField)
    {
        if (string.IsNullOrWhiteSpace(backgroundField))
        {
            return;
        }

        ClearBackgroundLayers();
        currentBackgroundSequenceIndex = -1;
        currentBackgroundSequence.Clear();

        string[] names = backgroundField.Split(ConstantsNew.BACKGROUND_NAME_DELIMITER);
        foreach (string rawName in names)
        {
            string trimmed = rawName.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                currentBackgroundSequence.Add(trimmed);
            }
        }

        if (currentBackgroundSequence.Count == 0)
        {
            return;
        }

        currentBackgroundSequenceIndex = 0;
        ShowBackgroundFromSequence(currentBackgroundSequence[currentBackgroundSequenceIndex]);
    }

    /// <summary>
    /// 若当前行仍有未播放的背景，则继续播放而不推进剧情
    /// </summary>
    private bool TryAdvanceBackgroundSequence()
    {
        if (currentBackgroundSequence == null || currentBackgroundSequence.Count == 0)
            return false;

        if (currentBackgroundSequenceIndex < 0 ||
            currentBackgroundSequenceIndex >= currentBackgroundSequence.Count - 1)
        {
            return false;
        }

        currentBackgroundSequenceIndex++;
        ShowBackgroundFromSequence(currentBackgroundSequence[currentBackgroundSequenceIndex]);
        return true;
    }

    private void ShowBackgroundFromSequence(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return;

        CreateBackgroundLayer(imageName);
    }

    private void CreateBackgroundLayer(string imageName)
    {
        if (backgroundLayerRoot == null || backgroundLayerTemplate == null)
        {
            Debug.LogError("背景层未配置！请在检查器中设置 backgroundLayerRoot 和 backgroundLayerTemplate。");
            return;
        }

        string imagePath = ConstantsNew.BACKGROUND_PATH + imageName;
        Debug.Log($"[VNManager] Loading bg: {imagePath}");
        Sprite sprite = Resources.Load<Sprite>(imagePath);

        if (sprite == null)
        {
            Debug.LogError(ConstantsNew.IMAGE_LOAD_FAILED + imagePath);
            return;
        }

        Image newLayer = Instantiate(backgroundLayerTemplate, backgroundLayerRoot);
        newLayer.gameObject.SetActive(true);
        newLayer.sprite = sprite;

        Color color = newLayer.color;
        color.a = (fadeDuration > 0f) ? 0f : 1f;
        newLayer.color = color;

        activeBackgroundLayers.Add(newLayer);
        EnforceLayerLimit();

        if (fadeDuration > 0f)
        {
            StartCoroutine(FadeInBackgroundLayer(newLayer));
        }
        else
        {
            color.a = 1f;
            newLayer.color = color;
        }
    }

    private void ClearBackgroundLayers()
    {
        foreach (var layer in activeBackgroundLayers)
        {
            if (layer != null)
            {
                Destroy(layer.gameObject);
            }
        }
        activeBackgroundLayers.Clear();
    }

    private void EnforceLayerLimit()
    {
        while (activeBackgroundLayers.Count > maxBackgroundLayers)
        {
            var oldest = activeBackgroundLayers[0];
            activeBackgroundLayers.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest.gameObject);
            }
        }
    }

    private IEnumerator FadeInBackgroundLayer(Image layer)
    {
        if (layer == null) yield break;

        float elapsedTime = 0f;
        Color color = layer.color;

        while (elapsedTime < fadeDuration)
        {
            if (layer == null)
            {
                yield break;
            }

            float progress = fadeDuration <= 0f ? 1f : elapsedTime / fadeDuration;
            color.a = Mathf.Lerp(0f, 1f, progress);
            layer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (layer != null)
        {
            color.a = 1f;
            layer.color = color;
        }
    }

    /// <summary>
    /// 显示立绘（AvatarImage1，独立于对话框和文字）
    /// </summary>
    private void DisplayAvatarImage1(string imageFileName)
    {
        if (avatarImage1 == null)
        {
            Debug.LogWarning("⚠️ avatarImage1 组件未绑定！请在Inspector中设置 Avatar Image1 字段。");
            return;
        }

        // 如果文件名为空，隐藏立绘
        if (string.IsNullOrEmpty(imageFileName))
        {
            avatarImage1.gameObject.SetActive(false);
            avatarImage1.sprite = null;
            Debug.Log("📝 AvatarImage1 文件名为空，已隐藏立绘");
            return;
        }

        // 加载立绘图片
        string path = $"{ConstantsNew.AVATAR_IMAGE1_PATH}{imageFileName}";
        // 移除可能的文件扩展名
        path = path.Replace(".png", "").Replace(".jpg", "").Replace(".jpeg", "");
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite != null)
        {
            avatarImage1.sprite = sprite;
            avatarImage1.gameObject.SetActive(true);
            Debug.Log($"✅ 成功加载立绘（AvatarImage1）: {path}");
        }
        else
        {
            Debug.LogError($"❌ 立绘加载失败（AvatarImage1）！\n" +
                          $"   - 检查路径: Resources/{path}\n" +
                          $"   - 检查文件名: {imageFileName}\n" +
                          $"   - 确保图片在 Resources/AvatarImage1/ 目录下\n" +
                          $"   - 确保图片已导入Unity（不是文件夹中的原始文件）");
            avatarImage1.gameObject.SetActive(false);
            avatarImage1.sprite = null;
        }
    }

    /// <summary>
    /// 更新背景音乐
    /// </summary>
    private void UpdateBackgroundMusic(string musicFileName)
    {
        if (backgroundMusic == null) return;

        // 移除文件后缀
        musicFileName = musicFileName.TrimStart('/').Replace(".mp3", "").Replace(".wav", "");
        string musicPath = Path.Combine(ConstantsNew.MUSIC_PATH, musicFileName).Replace("\\", "/");

        AudioClip audioClip = Resources.Load<AudioClip>(musicPath);
        if (audioClip != null)
        {
            backgroundMusic.clip = audioClip;
            backgroundMusic.Play();
            backgroundMusic.loop = true;
            Debug.Log($"成功加载背景音乐: {musicPath}");
        }
        else
        {
            Debug.LogError($"音乐加载失败！检查路径: Resources/{musicPath}");
        }
    }

    /// <summary>
    /// 统一控制对话框与文本显隐
    /// </summary>
    private void SetDialogueVisibility(bool isVisible)
    {
        if (dialogueBox != null)
        {
            dialogueBox.gameObject.SetActive(isVisible);
        }

        if (speakingContent != null)
        {
            speakingContent.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// 清空屏幕上的对话与立绘
    /// </summary>
    private void ClearScreenUI()
    {
        Debug.Log("🧼 执行清屏操作");

        if (typeWriterEffect != null && typeWriterEffect.IsTyping())
        {
            typeWriterEffect.CompleteLine();
        }

        if (speakingContent != null)
        {
            speakingContent.text = string.Empty;
            speakingContent.gameObject.SetActive(false);
        }

        if (dialogueBox != null)
        {
            dialogueBox.gameObject.SetActive(false);
        }

        if (protagonistAvatar != null)
        {
            protagonistAvatar.sprite = null;
            protagonistAvatar.gameObject.SetActive(false);
        }

        if (otherAvatar != null)
        {
            otherAvatar.sprite = null;
            otherAvatar.gameObject.SetActive(false);
        }

        if (avatarImage1 != null)
        {
            avatarImage1.sprite = null;
            avatarImage1.gameObject.SetActive(false);
        }

        if (protagonistName != null)
        {
            protagonistName.text = string.Empty;
            protagonistName.gameObject.SetActive(false);
        }

        if (otherCharacterName != null)
        {
            otherCharacterName.text = string.Empty;
            otherCharacterName.gameObject.SetActive(false);
        }

        lastAvatarDisplayed = false;
        lastDisplayedCharacter = string.Empty;
    }

    /// <summary>
    /// 加载默认故事
    /// </summary>
    public void LoadDefaultStory()
    {
        string path = Path.Combine(storyPath, defaultStoryFileName);
        storyData = ExcelReaderNew.ReadExcel(path);
        Debug.Log($"数据加载状态: {IsDataReady}，行数: {storyData?.Count ?? 0}");
    }

    /// <summary>
    /// 跳转到指定行
    /// </summary>
    public void JumpToLine(int lineNumber)
    {
        if (storyData == null) return;
        currentLine = Mathf.Clamp(lineNumber, 0, storyData.Count - 1);
        DisplayThisLine();
    }

    /// <summary>
    /// 获取故事数据
    /// </summary>
    public List<ExcelReaderNew.ExcelData> GetStoryData()
    {
        return storyData;
    }

    /// <summary>
    /// 获取当前分支面板背景文件名
    /// </summary>
    public string GetCurrentBranchPanelBackground()
    {
        return currentBranchPanelBackground;
    }
    /// <summary>
    /// 解析 Excel 中的 Command 字段
    /// 格式示例：
    /// Unlock:branch2
    /// EndBranch
    /// JumpTo:branch3.xlsx,10
    /// </summary>

    private bool HandleCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
            return false;

        Debug.Log($"【VNManager】处理命令: {command}");

        string[] parts = command.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        bool shouldStop = false;

        foreach (var raw in parts)
        {
            string cmd = raw.Trim();
            if (string.IsNullOrEmpty(cmd)) continue;

            if (cmd.Equals("EndBranch", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("【VNManager】执行 EndBranch");
                // 在结束分支前，应用当前行的分支面板背景
                var currentData = storyData[currentLine];
                if (!string.IsNullOrEmpty(currentData.BranchPanelBackground))
                {
                    currentBranchPanelBackground = currentData.BranchPanelBackground;
                    Debug.Log($"📋 EndBranch: 设置分支面板背景为: {currentBranchPanelBackground}");
                }
                if (BranchManager.Instance != null)
                {
                    BranchManager.Instance.CompleteCurrentBranch();
                }
                shouldStop = true;
            }
            else if (cmd.Equals("ShowBranchSelection", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("🎯 执行 ShowBranchSelection 命令");

                // 立即隐藏对话UI
                if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
                if (speakingContent != null) speakingContent.gameObject.SetActive(false);

                // 延迟显示面板
                StartCoroutine(DelayedShowPanel());
            }
            else if (cmd.StartsWith("Unlock:", StringComparison.OrdinalIgnoreCase))
            {
                string key = cmd.Substring("Unlock:".Length).Trim();
                Debug.Log($"【VNManager】执行 Unlock: {key}");
                if (BranchManager.Instance != null)
                {
                    BranchManager.Instance.Unlock(key);
                }
            }
            // ⭐ 删除下面这个重复的 ShowBranchSelection 处理
        }

        return shouldStop;
    }
    private IEnumerator DelayedShowPanel()
    {
        yield return new WaitForSeconds(0.5f); // 等待半秒，确保剧情结束
        if (BranchManager.Instance != null)
        {
            BranchManager.Instance.ShowBranchSelection();
        }
        else
        {
            Debug.LogError("❌ BranchManager.Instance 为 null");
        }
    }


    public void ShowBranchSelection()
    {
        Debug.Log("显示章节选择面板");

        // 隐藏对话相关UI
        if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
        if (speakingContent != null) speakingContent.gameObject.SetActive(false);
        if (protagonistAvatar != null) protagonistAvatar.gameObject.SetActive(false);
        if (otherAvatar != null) otherAvatar.gameObject.SetActive(false);
        if (avatarImage1 != null) avatarImage1.gameObject.SetActive(false);

        // 显示章节选择面板
        if (BranchManager.Instance != null && BranchManager.Instance.branchSelectionPanel != null)
        {
            BranchManager.Instance.branchSelectionPanel.SetActive(true);
            BranchManager.Instance.RefreshAllButtons(); // 刷新按钮状态
        }
    }
    public void LoadStoryFile(string fullPath, int startLine = 0)
    {
        Debug.Log($"🔄 VNManagerNew.LoadStoryFile: {fullPath}");

        // 先清空旧数据
        storyData = null;
        currentLine = startLine;

        StartCoroutine(ExcelReaderNew.ReadExcelAsync(fullPath, (result) =>
        {
            if (result == null || result.Count == 0)
            {
                Debug.LogError($"❌ 故事文件加载失败: {fullPath}");
                return;
            }

            storyData = result;
            Debug.Log($"✅ 加载成功，行数: {storyData.Count}");

            // ⭐ 修复：调用 DisplayNextLine() 而不是 DisplayThisLine()
            // 这样会显示当前行，然后自动移动到下一行
            DisplayNextLine();
        }));
    }
}

