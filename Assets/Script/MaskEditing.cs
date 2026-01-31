using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;
using System.Collections.Generic;

public class MaskEditing : MonoBehaviour
{
    //Undo functionality
    private Stack<GameObject> undoStack = new Stack<GameObject>();
    public int maxUndoSteps = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private GameObject instanceSticker;
    private Vector3 mousePos;
    private bool isDragged;

    public GameObject parentMask;
    public SpriteRenderer maskWhite;

 
    // Update is called once per frame
    void Update()
    {

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySpawnSticker();
        }

        if (Mouse.current.leftButton.isPressed && isDragged)
        {
            DragSticker();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragged = false;
            if (!CheckMaskBounds()) {
                Destroy(instanceSticker); // Delete a stucker if its out of mask bounds. We are staying clean overhere.
            }
            else
            {
                RegisterUndo(instanceSticker);
            }

        }
    }

    void DragSticker()
    {
        if (instanceSticker == null) return;

        Vector2 mouseWorld =
            Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        instanceSticker.transform.position = mouseWorld;
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
           // Debug.Log("No hit");

            return;
        }

        UndoButton undo = hit.collider.GetComponentInParent<UndoButton>();
        if (undo != null)
        {
            Undo();
            return;
        }


        ButtonSprite button = hit.collider.GetComponent<ButtonSprite>();
        if (button == null) {
            button = hit.collider.GetComponentInParent<ButtonSprite>();
            
        }
       

       // Debug.Log("Hit: " + hit.collider.name);
        instanceSticker = Instantiate(button.stickerPrefab, mouseWorld, Quaternion.identity, parentMask.transform);
        isDragged = true;
    }

    bool CheckMaskBounds() {

        //This check will be used to prevent unneeded stacking of the invisible decorations outside of the mask
        Bounds maskBounds = maskWhite.bounds;
        if (instanceSticker == null){ return false; }
        Vector3 stickerPos = instanceSticker.transform.position;

        return maskBounds.Contains(stickerPos);
    }

    void RegisterUndo(GameObject stickersTransform) {
        undoStack.Push(stickersTransform);

        // Limit undo stack size
        while (undoStack.Count > maxUndoSteps)
        {
            // Remove the oldest entry
            GameObject[] temp = undoStack.ToArray();
            undoStack.Clear();

            for (int i = Mathf.Min(temp.Length - 1, maxUndoSteps - 1); i >= 0; i--)
                undoStack.Push(temp[i]);
        }
        Debug.Log("Undo works?: " + undoStack.Count);
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        GameObject last = undoStack.Pop();
        Destroy(last);
    }

}

