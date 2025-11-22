using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using System.Text;
using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Excel读取器 - 纯净版本
/// 第一行为表头，第二行开始为数据内容
/// </summary>
public static class ExcelReaderNew
{
    [System.Serializable]
    public struct ExcelData
    {
        // 对话字段
        public string speaker;              // 说话者
        public string content;              // 对话内容

        // 资源字段
        public string AvatarImageFileName;  // 头像文件名
        public string backgroundImageFileName;  // 背景图片文件名
        public string backgroundMusicFileName;  // 背景音乐文件名
        public string AvatarImage1;  // 立绘图片文件名（独立于对话框）

        // 状态字段
        public bool IsProtagonist;          // 是否为主角
        //单元跳转指令
        public string Command;// 指令
        // 清屏指令
        public bool ClearScreen;            // 是否清空屏幕

    }

    /// <summary>
    /// 读取Excel文件（同步）
    /// </summary>
    public static List<ExcelData> ReadExcel(string filePath)
    {
        List<ExcelData> excelData = new List<ExcelData>();

        // 注册编码提供程序（支持中文）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                bool isFirstRow = true; // 跳过第一行表头

                do
                {
                    while (reader.Read())
                    {
                        // 跳过表头行
                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            continue;
                        }

                        ExcelData data = new ExcelData();

                        // 读取字段（从第0列开始）
                        data.speaker = SafeGetString(reader, 0);
                        string rawContent = SafeGetString(reader, 1);
                        data.content = ProcessNewlines(rawContent); // 处理换行符

                        // 资源字段
                        data.AvatarImageFileName = SafeGetString(reader, 2);
                        data.backgroundImageFileName = SafeGetString(reader, 3);
                        data.backgroundMusicFileName = SafeGetString(reader, 4);

                        // 状态字段
                        data.IsProtagonist = SafeGetBool(reader, 5, "Y");
                        //单元跳转指令
                        data.Command = SafeGetString(reader, 6);
                        // 清屏指令（第7列，值为1时触发）
                        data.ClearScreen = SafeGetBool(reader, 7, "1");
                        // 立绘图片（第8列，独立于对话框）
                        data.AvatarImage1 = SafeGetString(reader, 8);


                        excelData.Add(data);
                    }
                } while (reader.NextResult());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"读取Excel文件失败: {ex.Message}");
        }

        return excelData;
    }

    /// <summary>
    /// 异步读取Excel文件
    /// </summary>
    public static IEnumerator ReadExcelAsync(string filePath, Action<List<ExcelData>> onComplete)
    {
        List<ExcelData> excelData = new List<ExcelData>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if UNITY_ANDROID && !UNITY_EDITOR
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("读取Excel失败: " + www.error);
            onComplete?.Invoke(null);
            yield break;
        }

        using (var stream = new MemoryStream(www.downloadHandler.data))
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            ParseExcel(reader, ref excelData);
        }
#else
        try
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                ParseExcel(reader, ref excelData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("读取Excel失败: " + e.Message);
            onComplete?.Invoke(null);
            yield break;
        }
#endif

        onComplete?.Invoke(excelData);
    }

    /// <summary>
    /// 解析Excel数据
    /// </summary>
    /// <summary>
    /// 解析Excel数据
    /// </summary>
    private static void ParseExcel(IExcelDataReader reader, ref List<ExcelData> excelData)
    {
        bool isFirstRow = true; // 跳过第一行表头

        do
        {
            while (reader.Read())
            {
                // 跳过表头行
                if (isFirstRow)
                {
                    isFirstRow = false;
                    continue;
                }

                ExcelData data = new ExcelData();

                // 读取字段
                data.speaker = SafeGetString(reader, 0);
                string rawContent = SafeGetString(reader, 1);
                data.content = ProcessNewlines(rawContent);

                // ⭐ 修复：确保 content 不为 null
                if (data.content == null)
                    data.content = "";

                data.AvatarImageFileName = SafeGetString(reader, 2);
                data.backgroundImageFileName = SafeGetString(reader, 3);
                data.backgroundMusicFileName = SafeGetString(reader, 4);
                data.IsProtagonist = SafeGetBool(reader, 5, "Y");

                // ⭐ 重要：读取 Command 字段（第6列）
                data.Command = SafeGetString(reader, 6);
                // ⭐ 新增：读取清屏字段（第7列，值为1时清屏）
                data.ClearScreen = SafeGetBool(reader, 7, "1");
                // ⭐ 新增：读取立绘图片字段（第8列，独立于对话框）
                data.AvatarImage1 = SafeGetString(reader, 8);

                excelData.Add(data);

                Debug.Log($"📝 读取行: speaker='{data.speaker}', content='{data.content}', Command='{data.Command}', ClearScreen={data.ClearScreen}");
            }
        } while (reader.NextResult());
    }

    /// <summary>
    /// 处理换行符
    /// </summary>
    private static string ProcessNewlines(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("\\n", "\n");
    }

    #region 安全读取辅助方法

    private static string SafeGetString(IExcelDataReader reader, int index)
    {
        if (reader.FieldCount <= index || reader.IsDBNull(index))
            return null;

        string value = reader.GetValue(index)?.ToString();
        return string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }

    private static bool SafeGetBool(IExcelDataReader reader, int index, string trueValue)
    {
        string value = SafeGetString(reader, index);
        return !string.IsNullOrEmpty(value) &&
               value.Trim().Equals(trueValue, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

