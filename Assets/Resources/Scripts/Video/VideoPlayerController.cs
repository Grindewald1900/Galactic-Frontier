using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    public RawImage rawImage; // 将RawImage拖拽到这个字段中
    public VideoPlayer videoPlayer; // 将VideoPlayer组件拖拽到这个字段中

    void Start()
    {
        // 将VideoPlayer的TargetTexture设置为RawImage的texture
        videoPlayer.targetTexture = new RenderTexture((int)rawImage.rectTransform.rect.width, (int)rawImage.rectTransform.rect.height, 24);
        rawImage.texture = videoPlayer.targetTexture;

        // 播放视频
        videoPlayer.Play();
    }

    // 暂停视频播放
    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    // 停止视频播放
    public void StopVideo()
    {
        videoPlayer.Stop();
    }
}
