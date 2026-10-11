using System;
using UnityEngine;
public static class NpcImageDecodeCases
{
    public static void Run()
    {
        byte[] bad = new byte[] { 1, 2, 3, 4, 5 };
        if (Game1.Image.createImage(bad) != null || Game2.Image.createImage(bad) != null)
            throw new Exception("Invalid NPC image must return null in both tabs");
        var texture = new Texture2D(16,24);
        byte[] png = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        var first = Game1.Image.createImage(png);
        var second = Game2.Image.createImage(png);
        if (first == null || second == null || first.w != 16 || first.h != 24 || second.w != 16 || second.h != 24)
            throw new Exception("Valid NPC image failed decoding");
        UnityEngine.Object.DestroyImmediate(first.texture);
        UnityEngine.Object.DestroyImmediate(second.texture);
        Debug.Log("NPC_IMAGE_ACTUAL_UNITY_DECODE_PASSED both tabs: invalid bytes and valid PNG");
    }
}
