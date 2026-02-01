using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;
using System.IO;

public class DressingUpManager : MonoBehaviour
{

    [SerializeField] public GameObject snapPoint;

    private GameObject instanceOutfitPiece;

    private GameObject currentHairFront;
    private GameObject currentHairBack;
    private GameObject currentBottom;
    private GameObject currentTop;
    private GameObject currentSocks;
    private GameObject currentMakeup;
    private GameObject currentShoes;

    private Stack<GameObject> undoStack = new Stack<GameObject>();
    public SpriteRenderer dollSprite;
    private bool isDragged;
    public int maxUndoSteps = 10;

    public GameObject toMaskSceneBut;

    private bool loadedCaptureScene = false;


    public GameObject objectToCapture;
    public int width = 2000;
    public int height = 2000;

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
        int pixelsPerUnit = 100;
        Bounds bounds = CalculateBounds(objectToCapture.GetComponentsInChildren<SpriteRenderer>());
        int width = Mathf.CeilToInt(bounds.size.x * pixelsPerUnit);
        int height = Mathf.CeilToInt(bounds.size.y * pixelsPerUnit);

        foreach (GameObject root in SceneManager.GetSceneByName("DressingScene").GetRootGameObjects())
        {
            root.SetActive(false);
        }

        GameObject captureInstance = Instantiate(objectToCapture);
        captureInstance.SetActive(true);

        GameObject camObj = new GameObject("TempCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;

        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        cam.orthographicSize = bounds.size.y / 2f;
        cam.orthographicSize *= 1.3f;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // Save
        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/CapturedSprite.png";
        System.IO.File.WriteAllBytes(path, bytes);

        // **THIS IS THE KEY FIX**
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log("Saved to: " + path);

        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        DestroyImmediate(rt);
        DestroyImmediate(camObj);
        Destroy(captureInstance);
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
                EquipToSlot(ref currentHairBack, prefab, snapPoint.transform);
                break;
            case ClothingType.HairFront:
                EquipToSlot(ref currentHairFront, prefab, snapPoint.transform);
                break;

            case ClothingType.Bottom:
                EquipToSlot(ref currentBottom, prefab, snapPoint.transform);
                break;

            case ClothingType.Top:
                EquipToSlot(ref currentTop, prefab, snapPoint.transform);
                break;
            case ClothingType.Socks:
                EquipToSlot(ref currentSocks, prefab, snapPoint.transform);
                break;
            case ClothingType.Makeup:
                EquipToSlot(ref currentMakeup, prefab, snapPoint.transform);
                break;
            case ClothingType.Shoes:
                EquipToSlot(ref currentShoes, prefab, snapPoint.transform);
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