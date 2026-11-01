using UnityEngine;

public class SpritePreviewAttribute : PropertyAttribute
{
    public float PreviewSize { get; private set; }

    public SpritePreviewAttribute(float previewSize = 64f)
    {
        PreviewSize = previewSize;
    }
}
