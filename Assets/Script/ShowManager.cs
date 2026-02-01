using System.IO;
using UnityEngine;

public class ShowManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public SpriteRenderer outfitSpriteRenderer;
    public SpriteRenderer maskSpriteRenderer;
    void Start()
    {
        LoadAndDisplayTextures();
    }

    void LoadAndDisplayTextures() {

        // Load the girl/outfit texture
        Texture2D girlTexture = LoadTextureFromFile("ChangedOutfit.png");
        if (girlTexture != null)
        {
            ApplyTextureToSprite(outfitSpriteRenderer, girlTexture);
        }

        // Load the mask texture
        Texture2D maskTexture = LoadTextureFromFile("MaskTexture.png");
        if (maskTexture != null)
        {
            ApplyTextureToSprite(maskSpriteRenderer, maskTexture);
        }
    }

    Texture2D LoadTextureFromFile(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Texture not found at: {path}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes); // This will resize the texture automatically

        Debug.Log($"Loaded texture from: {path}");
        return texture;
    }

    void ApplyTextureToSprite(SpriteRenderer spriteRenderer, Texture2D texture)
    {
        // Create a new sprite from the texture
        Sprite newSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), // Pivot at center
            100f // Pixels per unit - adjust if needed
        );

        spriteRenderer.sprite = newSprite;
    }
}
