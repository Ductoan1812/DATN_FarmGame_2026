using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

public static class CaptureGameView
{
    [MenuItem("Tools/DATN/Utilities/Capture Game View")]
    public static void Execute()
    {
        // Focus the Game View window first
        var assembly = typeof(EditorWindow).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
        {
            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            if (gameView != null)
                gameView.Focus();
        }

        // Use EditorApplication.delayCall to wait for render
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex != null)
                    {
                        var bytes = tex.EncodeToPNG();
                        var path = Path.Combine(Application.dataPath, "..", "Temp", "game_capture.png");
                        File.WriteAllBytes(path, bytes);
                        Object.DestroyImmediate(tex);
                        Debug.Log($"[CaptureGameView] Saved to: {Path.GetFullPath(path)}");
                    }
                    else
                    {
                        // Fallback: render from main camera
                        var cam = Camera.main;
                        if (cam == null)
                        {
                            Debug.LogError("[CaptureGameView] No main camera found.");
                            return;
                        }

                        int w = 1280, h = 720;
                        var rt = new RenderTexture(w, h, 24);
                        cam.targetTexture = rt;
                        var screenShot = new Texture2D(w, h, TextureFormat.RGB24, false);
                        cam.Render();
                        RenderTexture.active = rt;
                        screenShot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                        screenShot.Apply();
                        cam.targetTexture = null;
                        RenderTexture.active = null;

                        var bytes = screenShot.EncodeToPNG();
                        var path = Path.Combine(Application.dataPath, "..", "Temp", "game_capture.png");
                        File.WriteAllBytes(path, bytes);

                        Object.DestroyImmediate(rt);
                        Object.DestroyImmediate(screenShot);
                        Debug.Log($"[CaptureGameView] Camera fallback saved to: {Path.GetFullPath(path)}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CaptureGameView] Error: {ex.Message}");
                }
            };
        };
    }
}
