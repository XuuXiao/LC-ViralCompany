using FFMpegCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.Image;
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

    internal static FFMpegArgumentProcessor LogArguments(this FFMpegArgumentProcessor args) {
        Plugin.Logger.LogDebug(args.Arguments.Replace(Path.GetTempPath(), "%TEMP%" + Path.PathSeparator));
        return args;
    }

    internal static Texture2D GetTexture2D(this RenderTexture texture) {
        Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        RenderTexture.active = texture;
        tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        tex.Apply();
        tex.FlipVertically();
        return tex;
    }

    internal static void FlipVertically(this Texture2D texture) {
        var originalPixels = texture.GetPixels();

        var newPixels = new Color[originalPixels.Length];

        var width = texture.width;
        var rows = texture.height;

        for(var x = 0; x < width; x++) {
            for(var y = 0; y < rows; y++) {
                newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
            }
        }

        texture.SetPixels(newPixels);
        texture.Apply();
    }
}
