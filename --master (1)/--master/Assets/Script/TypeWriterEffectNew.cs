using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 打字机效果 - 纯净版本
/// </summary>
public class TypeWriterEffectNew : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    public float waitingSeconds = ConstantsNew.DEFAULT_WAITING_SECONDS;

    private Coroutine typingCoroutine;
    private bool isTyping;

    /// <summary>
    /// 开始打字机效果
    /// </summary>
    public void StartTyping(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(text));
    }

    /// <summary>
    /// 打字机协程
    /// </summary>
    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
        textDisplay.text = text;
        textDisplay.maxVisibleCharacters = 0;

        for (int i = 0; i <= text.Length; i++)
        {
            textDisplay.maxVisibleCharacters = i;
            yield return new WaitForSeconds(waitingSeconds);
        }

        isTyping = false;
    }

    /// <summary>
    /// 立即完成当前行
    /// </summary>
    public void CompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        textDisplay.maxVisibleCharacters = textDisplay.text.Length;
        isTyping = false;
    }

    /// <summary>
    /// 检查是否正在打字
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }
}

