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


    //UI elements
    public GameObject rotateRight;
    public GameObject rotateLeft;
    public GameObject mirror;
    public GameObject undo;


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
            if (isDragged && instanceSticker != null)
            {
                isDragged = false;

                if (!CheckMaskBounds())
                {
                    Destroy(instanceSticker);
                }
                else
                {
                    RegisterUndo(instanceSticker);
                }

                instanceSticker = null; // clear the reference
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

 
        if (hit.collider.gameObject == undo)
        {
            Undo();
            return;
        }


        ButtonSprite button = hit.collider.GetComponent<ButtonSprite>();
        if (button == null) {
            button = hit.collider.GetComponentInParent<ButtonSprite>();
            
        }


        if (hit.collider.gameObject == rotateRight) {
            RotateLastSticker(10.0f);
                return;
        }
        if (hit.collider.gameObject == rotateLeft) {
            RotateLastSticker(-10.0f);
            return;
        }
        if (hit.collider.gameObject == mirror)
        {
            Mirror();
            return;
        }

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

    void RegisterUndo(GameObject stickersTransform)
    {
        undoStack.Push(stickersTransform);
        Debug.Log("Undo works?: " + undoStack.Count);

        // Limit undo stack size
        while (undoStack.Count > 10)
        {
            // Remove the oldest entry
            GameObject[] temp = undoStack.ToArray();
            undoStack.Clear();

            for (int i = Mathf.Min(temp.Length - 1, maxUndoSteps - 1); i >= 0; i--)
                undoStack.Push(temp[i]);
        }
    }

    void Undo()
    {
        if (undoStack.Count == 0) return;

        GameObject last = undoStack.Pop();
        Destroy(last);
    }

    void RotateLastSticker(float angle) { 
         if (undoStack.Count == 0) return;
        undoStack.Peek().transform.Rotate(0f, 0f, angle);
    }

    void Mirror()
    {
      
    }

}

