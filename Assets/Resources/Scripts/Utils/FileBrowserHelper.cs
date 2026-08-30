using UnityEngine;
using System;
using System.Collections;
using System.IO;
using Assets.Resources.Scripts.Utils;
using SimpleFileBrowser;

namespace Assets.Scripts.Utils
{
    public enum ImagePickFail
    {
        InvalidType,
        TooLarge,
        DecodeFailed
    }

    public static class FileBrowserHelper
    {
        /// <summary>Opens a PNG/JPG picker and returns a Sprite, or reports why it failed.</summary>
        public static void OpenFileBrowser(Action<Sprite> onImageSelected, Action<ImagePickFail> onFailed = null)
        {
            var runner = new GameObject("FileBrowserCoroutineRunner");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<MonoBehaviourRunner>()
                .StartCoroutine(ShowLoadDialogCoroutine(onImageSelected, onFailed, runner));
        }

        private static IEnumerator ShowLoadDialogCoroutine(
            Action<Sprite> onImageSelected,
            Action<ImagePickFail> onFailed,
            GameObject runner)
        {
            try
            {
                FileBrowser.SetFilters(false, ".png", ".jpg", ".jpeg");
                FileBrowser.SetDefaultFilter(".png");

                yield return FileBrowser.WaitForLoadDialog(
                    FileBrowser.PickMode.Files,
                    false,
                    null,
                    null,
                    "Select image",
                    "Load");

                if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
                    yield break;

                string filePath = FileBrowser.Result[0];
                string fileName = FileBrowserHelpers.GetFilename(filePath);
                if (!IsAllowedImageName(fileName))
                {
                    onFailed?.Invoke(ImagePickFail.InvalidType);
                    yield break;
                }

                string tempPath = Path.Combine(
                    Application.temporaryCachePath,
                    "gf_avatar_pick" + Path.GetExtension(fileName));
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    FileBrowserHelpers.CopyFile(filePath, tempPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[AVATAR] Copy failed: " + ex.Message);
                    onFailed?.Invoke(ImagePickFail.DecodeFailed);
                    yield break;
                }

                if (!File.Exists(tempPath))
                {
                    onFailed?.Invoke(ImagePickFail.DecodeFailed);
                    yield break;
                }

                var info = new FileInfo(tempPath);
                if (info.Length <= 0 || info.Length > ImageUtil.AvatarMaxBytes)
                {
                    TryDelete(tempPath);
                    onFailed?.Invoke(ImagePickFail.TooLarge);
                    yield break;
                }

                Sprite sprite = ImageUtil.LoadSpriteFromFile(tempPath);
                TryDelete(tempPath);
                if (sprite == null || sprite.texture == null || sprite.texture.width < 8 || sprite.texture.height < 8)
                {
                    onFailed?.Invoke(ImagePickFail.DecodeFailed);
                    yield break;
                }

                onImageSelected?.Invoke(sprite);
            }
            finally
            {
                if (runner != null)
                    UnityEngine.Object.Destroy(runner);
            }
        }

        private static bool IsAllowedImageName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception)
            {
                // Temp cleanup is best-effort.
            }
        }
    }

    public class MonoBehaviourRunner : MonoBehaviour { }
}
