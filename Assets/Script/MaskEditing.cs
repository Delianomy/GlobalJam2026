using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class MaskEditing : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject prefSticker;
    private GameObject instanceSticker;
    private Vector3 mousePos;
    private bool isDragged;

    public Transform parentMask;
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
                Destroy(instanceSticker);
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
        Vector2 mouseWorld =
             Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider == null)
        {
            Debug.Log("No hit");
            return;
        }

        Debug.Log("Hit: " + hit.collider.name);

        instanceSticker = Instantiate(prefSticker, mouseWorld, Quaternion.identity);
        isDragged = true;
    }

    bool CheckMaskBounds() {
        Bounds maskBounds = maskWhite.bounds;
        Vector3 stickerPos = instanceSticker.transform.position;

        return maskBounds.Contains(stickerPos);
    }



}

