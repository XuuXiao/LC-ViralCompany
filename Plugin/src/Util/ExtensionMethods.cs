using System;
using System.Collections.Generic;
using System.Text;

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
}
