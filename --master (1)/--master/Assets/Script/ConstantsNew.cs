using System.IO;
using UnityEngine;

public class ConstantsNew
{
    // 故事文件路径
    public static string STORY_PATH = Path.Combine(Application.streamingAssetsPath, "Story");
    public static string DEFAULT_STORY_FILE_NAME = "main.xlsx";
    public static int DEFAULT_START_LINE = 1;

    // 资源路径
    public static string PROTAGONIST_PATH = "Protagonist/";
    public static string CHARACTERS_PATH = "Characters/";
    public static string BACKGROUND_PATH = "Background/";
    public static string MUSIC_PATH = "Music/";
    public const char BACKGROUND_NAME_DELIMITER = '/';

    // 错误信息
    public static string IMAGE_LOAD_FAILED = "Failed to load image:";
    public static string MUSIC_LOAD_FAILED = "Failed to load music:";
    public static string NO_DATA_FOUND = "No data found";
    public static string END_OF_STORY = "End of story";

    // 打字机效果设置
    public static float DEFAULT_WAITING_SECONDS = 0.05f;
}

