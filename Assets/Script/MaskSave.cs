using UnityEngine;
using System.IO;
public class MaskSave : MonoBehaviour
{
    public SpriteRenderer parentMask;
    public SpriteRenderer[] stickerChildren;

    public void SaveComposedTexture(string fileName = "NewMask")
    {
        // Get bounds to determine render texture size
        Bounds bounds = CalculateBounds();

        int width = Mathf.CeilToInt(bounds.size.x * 100); // Adjust multiplier for resolution
        int height = Mathf.CeilToInt(bounds.size.y * 100);

        // Create render texture
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        Camera renderCamera = new GameObject("TempCamera").AddComponent<Camera>();
        renderCamera.targetTexture = renderTexture;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = bounds.size.y / 2;
        renderCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;

        // Render
        renderCamera.Render();

        // Read pixels from render texture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // Cleanup
        RenderTexture.active = null;
        DestroyImmediate(renderCamera.gameObject);
        renderTexture.Release();

        // Save as PNG asset
        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/GeneratedTextures/{fileName}.png";

        // Create directory if it doesn't exist
        Directory.CreateDirectory("Assets/GeneratedTextures");

        File.WriteAllBytes(path, bytes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log($"Texture saved to: {path}");
    }

    private Bounds CalculateBounds()
    {
        Bounds bounds = parentMask.bounds;

        foreach (var sticker in stickerChildren)
        {
            bounds.Encapsulate(sticker.bounds);
        }

        return bounds;
    }
}

