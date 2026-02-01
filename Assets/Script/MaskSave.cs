using UnityEngine;
using System.IO;
public class MaskSave : MonoBehaviour
{
    public SpriteRenderer parentMask;
    public SpriteRenderer[] stickerChildren;
    private string savedTexturePath;
    public void SaveRuntimeTexture(string fileName = "ComposedTexture")
    {
        // Save to persistent data path (not in Assets folder)
        string path = Path.Combine(Application.persistentDataPath, $"{fileName}.png");

        // Your texture creation code here...
        Texture2D texture = CreateComposedTexture();

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        savedTexturePath = path;
        Debug.Log($"Runtime texture saved to: {path}");
        Destroy(texture);
    }

    public Texture2D LoadRuntimeTexture()
    {
        if (string.IsNullOrEmpty(savedTexturePath) || !File.Exists(savedTexturePath))
        {
            Debug.LogWarning("No saved texture found!");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(savedTexturePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        return texture;
    }

    // Clean up when done
    public void DeleteRuntimeTexture()
    {
        if (!string.IsNullOrEmpty(savedTexturePath) && File.Exists(savedTexturePath))
        {
            File.Delete(savedTexturePath);
            Debug.Log("Runtime texture deleted");
        }
    }
    public Texture2D CreateComposedTexture()
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
        renderCamera.cullingMask = LayerMask.GetMask("ImageCapture");
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
        Debug.Log($"Created texture");
        return texture;
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

    void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(savedTexturePath) && File.Exists(savedTexturePath))
        {
            File.Delete(savedTexturePath);
            Debug.Log("Runtime texture deleted");
            savedTexturePath = null;
        }
    }

}

