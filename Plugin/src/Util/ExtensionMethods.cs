using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace ViralCompany.Util;
internal static class ExtensionMethods {
    internal static string NextString(this Random rnd, string allowedChars, int length) {
        return rnd.NextString(allowedChars, (length, length));
    }

    internal static string NextString(this Random rnd, string allowedChars, (int Min, int Max) length) {
        (int min, int max) = length;
        char[] chars = new char[max];
        int setLength = allowedChars.Length;

        int stringLength = rnd.Next(min, max + 1);

        for(int i = 0; i < stringLength; ++i) {
            chars[i] = allowedChars[rnd.Next(setLength)];
        }

        return new string(chars, 0, stringLength);
    }

    internal static Texture2D GetTexture2D(this RenderTexture texture) {
        Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        RenderTexture.active = texture;
        tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        tex.Apply();
        return tex;
    }
}
