using UnityEngine;

public class PictureReferences
{
    public string pictureID;
    public Texture2D texture2D;
    private Sprite picture;

    public Sprite Picture => picture;


    public PictureReferences() { }

    public PictureReferences(string pictureID, Texture2D texture2D) 
    { 
        this.pictureID = pictureID;
        this.texture2D = texture2D;

        picture = Sprite.Create(texture2D,
            new Rect(0, 0, texture2D.width, texture2D.height),
            new Vector2(0.5f, 0.5f));
    }

    public PictureReferences(string pictureID, Sprite sprite)
    {
        this.pictureID = pictureID;
        picture = sprite;
        texture2D = sprite != null ? sprite.texture : null;
    }
}
