using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.DebugUI;
using System.IO;

public class DressingUpManager : MonoBehaviour
{

    [SerializeField] public GameObject snapPoint;

    private GameObject instanceOutfitPiece;

    private GameObject currentHair;
    private GameObject currentBottom;

    private Stack<GameObject> undoStack = new Stack<GameObject>();
    public SpriteRenderer dollSprite;
    private bool isDragged;
    public int maxUndoSteps = 10;

    public GameObject toMaskSceneBut;

    private bool loadedCaptureScene = false;


    public GameObject objectToCapture;
    public int width = 512;
    public int height = 512;

    void Update()
    {

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySpawnSticker();
            Debug.Log("Test");
        }

        if (Mouse.current.leftButton.isPressed && isDragged)
        {
            DragItem();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isDragged && instanceOutfitPiece != null)
            {
                isDragged = false;

                // FIRST check if in bounds
                if (!CheckMaskBounds())
                {
                    // Out of bounds - destroy and exit
                    Destroy(instanceOutfitPiece);
                }
                else
                {
                    // In bounds - equip the item
                    ClothingItem itemData = instanceOutfitPiece.GetComponent<ClothingItem>();
                    if (itemData != null)
                    {
                        EquipItem(instanceOutfitPiece, itemData.type);
                    }
                    RegisterUndo(instanceOutfitPiece);
                }

                instanceOutfitPiece = null; // clear the reference
            }
        }
    }


    void TrySpawnSticker()
    {
        //Click on an Icon and try get a prefab
        //Icons are not UI Buttons, just Sprites with Colliders I raycast upon.
        Vector2 mouseWorld =
             Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider == null)
        {
            Debug.Log("No hit");
            return;
        }
        if (hit.collider.gameObject == toMaskSceneBut)
        {
            CaptureSprites();
            SceneManager.LoadScene("MaskEditingScene");
            return;
        }



        //if (hit.collider.gameObject == undo)
        //{
        //    Debug.Log("Clicked Undo");
        //    Undo();
        //    return;
        //}
        //if (hit.collider.gameObject == save)
        //{
        //    MaskSave maskSave = save.GetComponent<MaskSave>();
        //    maskSave.SaveRuntimeTexture();
        //    return;
        //}


        ClothingItem clothesPiece = hit.collider.GetComponent<ClothingItem>();
        if (clothesPiece == null)
        {
            clothesPiece = hit.collider.GetComponentInParent<ClothingItem>();
            Debug.Log("Clicked Create Prefab");
        }



        instanceOutfitPiece = Instantiate(clothesPiece.prefab, mouseWorld, Quaternion.identity);
        isDragged = true;
    }


    public void CaptureSprites()
    {
        // Get all SpriteRenderers in children
        SpriteRenderer[] renderers = objectToCapture.GetComponentsInChildren<SpriteRenderer>();

        // Calculate bounds
        Bounds bounds = CalculateBounds(renderers);

        // Create a temporary camera
        GameObject camObj = new GameObject("TempCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0, 0, 0, 0); // Transparent

        // Position camera
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);

        foreach (GameObject root in SceneManager.GetSceneByName("Dressingscene").GetRootGameObjects())
        {
            root.SetActive(false);
        }

        // Instantiate your capture object
        GameObject captureInstance = Instantiate(objectToCapture);
        captureInstance.SetActive(true);

        // Create RenderTexture with transparency
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        // Render
        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // Save as PNG (supports transparency)
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/CapturedSprite.png", bytes);

        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        DestroyImmediate(rt);
        DestroyImmediate(camObj);

        Debug.Log("Sprite saved to: " + Application.dataPath + "/CapturedSprite.png");
    }


    private Bounds CalculateBounds(SpriteRenderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }


















    void DragItem()
    {
        if (instanceOutfitPiece == null) return;

        Vector2 mouseWorld =
            Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        instanceOutfitPiece.transform.position = mouseWorld;
    }

    bool CheckMaskBounds()
    {

        //This check will be used to prevent unneeded stacking of the invisible decorations outside of the mask
        Bounds maskBounds = dollSprite.bounds;
        //maskBounds.size *= 1.f;
        if (instanceOutfitPiece == null) { return false; }
        Vector3 stickerPos = instanceOutfitPiece.transform.position;

        return maskBounds.Contains(stickerPos);
    }

    void RegisterUndo(GameObject itemTransform)
    {
        undoStack.Push(itemTransform);
        Debug.Log("Undo works?: " + undoStack.Count);

        // Limit undo stack size
        while (undoStack.Count > maxUndoSteps)
        {
            // Remove the oldest entry
            GameObject[] temp = undoStack.ToArray();
            undoStack.Clear();

            for (int i = Mathf.Min(temp.Length - 1, maxUndoSteps - 1); i >= 0; i--)
                undoStack.Push(temp[i]);
        }
    }

    void EquipItem(GameObject prefab, ClothingType type)
    {

        Debug.Log(type);

        switch (type)
        {
            case ClothingType.HairBack:
                EquipToSlot(ref currentHair, prefab, snapPoint.transform);
                break;

            case ClothingType.Bottom:
                EquipToSlot(ref currentBottom, prefab, snapPoint.transform);
                break;

                // Add other cases as needed
        }
    }

    void EquipToSlot(ref GameObject currentItem, GameObject newItem, Transform slot)
    {
        if (currentItem != null) Destroy(currentItem);
        currentItem = newItem;
        currentItem.transform.SetParent(slot);
        currentItem.transform.localPosition = slot.localPosition;
    }

    //private IEnumerator CaptureCurrentDress( ) {

    //    // 1. Load the capture scene additively
    //    AsyncOperation loadOp = SceneManager.LoadSceneAsync("CaptureScene", LoadSceneMode.Additive);
    //    while (!loadOp.isDone)
    //        yield return null;


    //    // 2. Get the loaded scene
    //    Scene captureScene = SceneManager.GetSceneByName("CaptureScene");
    //    if (!captureScene.IsValid())
    //    {
    //        Debug.LogError("CaptureScene not loaded properly!");
    //        yield break;
    //    }

    //    // 3. Find the CaptureManager in the loaded scene
    //    CaptureImage captureManager = null;
    //    foreach (GameObject root in captureScene.GetRootGameObjects())
    //    {
    //        captureManager = root.GetComponentInChildren<CaptureImage>();
    //        if (captureManager != null) break;
    //    }

    //    if (captureManager == null)
    //    {
    //        Debug.LogError("CaptureManager not found in CaptureScene!");
    //        yield break;
    //    }

    //    // 4. Pass object to capture
    //    captureManager.CaptureObject(snapPoint);

    //    // 5. Wait until capture is done
    //    yield return new WaitUntil(() => captureManager.IsCaptureDone);

    //    // 6. Unload capture scene
    //    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(captureScene);
    //    while (!unloadOp.isDone)
    //        yield return null;

    //    Debug.Log("Capture complete and scene unloaded!");

    //}





    //    IEnumerator LoadCaptureSceneAndCapture(GameObject roots)
    //    {
    //        if (loadedCaptureScene)
    //        {
    //            yield return null;
    //        }
    //        AsyncOperation load = SceneManager.LoadSceneAsync("CaptureScene", LoadSceneMode.Additive);

    //        // Wait until scene is fully loaded
    //        while (!load.isDone)
    //            yield return null;

    //        Scene captureScene = SceneManager.GetSceneByName("CaptureScene");

    //        if (!captureScene.isLoaded)
    //        {
    //            Debug.LogError("CaptureScene not loaded!");
    //            yield break;
    //        }

    //        // Now it's safe to search
    //        CaptureImage capture = FindObjectOfType<CaptureImage>();

    //        if (capture == null)
    //        {
    //            Debug.LogError("CaptureImage NOT found in CaptureScene!");
    //            yield break;
    //        }

    //        Debug.Log("i did it :D");

    //        loadedCaptureScene = true;

    //        capture.Capture(roots);
    //    }


    //}


}

public enum ClothingType
{
    HairBack,
    HairFront,
    Top,
    Bottom,
    Makeup,
    Socks,
    Shoes
   
}