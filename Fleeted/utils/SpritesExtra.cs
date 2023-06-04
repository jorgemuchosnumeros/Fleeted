using System.IO;
using System.Reflection;
using UnityEngine;

namespace Fleeted.utils;

public static class SpritesExtra
{
    public static Sprite SpriteFromName(string name)
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        using var resourceMemory = new MemoryStream();
        resourceMemory.SetLength(0);
        resource.CopyTo(resourceMemory);
        var imageBytes = resourceMemory.ToArray();
        Texture2D tex2D = new Texture2D(2, 2);
        tex2D.LoadImage(imageBytes);
        return Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), Vector2.zero, 50f);
    }
}