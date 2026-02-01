using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;

public class DressingUpManager : MonoBehaviour
{

    [SerializeField] private Transform snapPoint;
    private GameObject instanceOutfitPiece;

    private GameObject currentHair;
    private GameObject currentBottom;

    private Stack<GameObject> undoStack = new Stack<GameObject>();
    public SpriteRenderer dollSprite;
    private bool isDragged;
    public int maxUndoSteps = 10;
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
                if (currentHair != null) Destroy(currentHair);
                currentHair = prefab;
                // Just match the doll's world position
                currentHair.transform.position = snapPoint.transform.position;
                currentHair.transform.rotation = snapPoint.transform.rotation;
                break;

            case ClothingType.Bottom:
                if (currentBottom != null) Destroy(currentBottom);
                currentBottom = prefab;
                currentBottom.transform.position = snapPoint.transform.position;
                currentBottom.transform.rotation = snapPoint.transform.rotation;
                break;

                // Add other cases as needed
        }
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