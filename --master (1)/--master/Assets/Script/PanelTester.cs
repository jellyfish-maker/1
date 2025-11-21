using UnityEngine;

public class PanelTester : MonoBehaviour
{
    void Update()
    {
        // 按空格键手动测试面板显示
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("🔄 手动测试面板显示");
            if (BranchManager.Instance != null)
            {
                BranchManager.Instance.ShowBranchSelection();
            }
            else
            {
                Debug.LogError("❌ BranchManager.Instance 为 null");
            }
        }

        // 按H键手动隐藏面板
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("🔄 手动测试面板隐藏");
            if (BranchManager.Instance != null)
            {
                BranchManager.Instance.HideBranchSelection();
            }
        }
    }
}