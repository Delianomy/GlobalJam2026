using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
public class CaptureImage : MonoBehaviour
{

    public Camera captureCamera;
    public RenderTexture renderTexture;
    public bool IsCaptureDone { get; private set; } = false;

    public void CaptureObject(GameObject obj)
    {
        IsCaptureDone = false;

        // Instantiate the object inside the capture scene
        GameObject captureInstance = Instantiate(obj);
        captureInstance.transform.position = Vector3.zero; // Reset position
        captureInstance.transform.rotation = Quaternion.identity;

        // Calculate bounds if needed
        Bounds bounds = CalculateBounds(captureInstance);

        // Setup render texture
        int width = Mathf.CeilToInt(bounds.size.x * 100);
        int height = Mathf.CeilToInt(bounds.size.y * 100);
        RenderTexture rt = new RenderTexture(width, height, 24);
        captureCamera.targetTexture = rt;

        // Optional: position camera to fully see object
        captureCamera.transform.position = bounds.center + new Vector3(0, 0, -10);
        captureCamera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) / 2f;

        // Render
        captureCamera.Render();

        // Convert to Texture2D
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Save as PNG
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, "capture.png");
        File.WriteAllBytes(path, bytes);
        Debug.Log("Saved capture to: " + path);

        // Cleanup
        RenderTexture.active = null;
        captureCamera.targetTexture = null;
        Destroy(rt);
        Destroy(captureInstance);

        IsCaptureDone = true;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}

