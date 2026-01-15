using System;
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
        LoadImage(tex2D, imageBytes);
        return Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), Vector2.zero, 50f);
    }

    private static void LoadImage(Texture2D texture, byte[] data)
    {
        // avoid a compile-time dependency on ReadOnlySpan<T>.
        var imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", false);
        if (imageConversionType == null)
        {
            throw new InvalidOperationException("UnityEngine.ImageConversionModule not found.");
        }

        var method = imageConversionType.GetMethod(
            "LoadImage",
            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }
        ) ?? imageConversionType.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

        if (method == null)
        {
            throw new MissingMethodException("UnityEngine.ImageConversion.LoadImage");
        }

        var args = method.GetParameters().Length == 3
            ? new object[] { texture, data, false }
            : new object[] { texture, data };
        method.Invoke(null, args);
    }
}
