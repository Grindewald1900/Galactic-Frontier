using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser;

namespace Assets.Scripts.Utils
{
    public static class FileBrowserHelper
    {
        /// <summary>
        /// 打开文件浏览器选择图片，并通过回调返回 `Sprite`
        /// </summary>
        public static void OpenFileBrowser(System.Action<Sprite> onImageSelected)
        {
            // 启动协程处理文件选择
            GameObject coroutineRunner = new GameObject("FileBrowserCoroutineRunner");
            coroutineRunner.AddComponent<MonoBehaviourRunner>().StartCoroutine(ShowLoadDialogCoroutine(onImageSelected));
        }

        private static IEnumerator ShowLoadDialogCoroutine(System.Action<Sprite> onImageSelected)
        {
            // 显示文件选择窗口，允许选择 PNG / JPG
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "选择图片", "加载");

            if (FileBrowser.Success && FileBrowser.Result.Length > 0)
            {
                string filePath = FileBrowser.Result[0]; // 获取用户选择的图片路径
                string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(filePath));

                // 复制文件到本地存储（防止原文件被删除）
                FileBrowserHelpers.CopyFile(filePath, destinationPath);

                // 读取图片并转换为 Sprite
                Sprite sprite = LoadSpriteFromFile(destinationPath);

                // 调用回调函数，返回 `Sprite`
                onImageSelected?.Invoke(sprite);
            }
        }

        /// <summary>
        /// 读取本地图片并转换为 `Sprite`
        /// </summary>
        public static Sprite LoadSpriteFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] imageData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
    }

    /// <summary>
    /// `MonoBehaviourRunner` 让 `static` 方法也能使用 `Coroutine`
    /// </summary>
    public class MonoBehaviourRunner : MonoBehaviour { }
}