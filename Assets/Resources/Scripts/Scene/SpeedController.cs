using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedController : MonoBehaviour
{
    // 在 Inspector 中指定该按钮
    public Button speedButton;
    public TextMeshProUGUI speedText;

    // 定义可用的速度倍数
    private float[] speeds = new float[] { 1f, 2f, 0.2f };
    private Color[] speedColors = new Color[] { ColorUtil.speedColorOne, ColorUtil.speedColorTwo, ColorUtil.speedColorThree };
    // 当前的速度索引，初始为0，对应1倍速
    private int currentSpeedIndex = 0;

    void Start()
    {
        // 初始化全局游戏速度
        Time.timeScale = speeds[currentSpeedIndex];

        if (speedButton != null)
        {
            // 添加按钮点击事件监听
            speedButton.onClick.AddListener(ChangeSpeed);
            // 更新按钮文本显示当前速度
            UpdateButtonText();
        }
    }

    // 每次点击按钮调用，循环切换速度
    void ChangeSpeed()
    {
        // 计算下一个速度索引（在0、1、2之间循环）
        currentSpeedIndex = (currentSpeedIndex + 1) % speeds.Length;
        // 设置全局游戏速度
        Time.timeScale = speeds[currentSpeedIndex];
        Debug.Log("Game speed set to: " + speeds[currentSpeedIndex] + "x");

        // 更新按钮上的文本显示（假设按钮上有 Text 组件）
        UpdateButtonText();
    }

    // 更新按钮显示文本
    void UpdateButtonText()
    {
        if (speedText != null)
        {
            speedText.text = "Speed " + speeds[currentSpeedIndex] + "x";
            speedText.color = speedColors[currentSpeedIndex];
        }

    }
}