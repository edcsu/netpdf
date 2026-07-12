namespace NetPdf.Layout.Text;

/// <summary>
/// Pragmatic Unicode bidirectional reordering (UAX #9 subset): resolves embedding levels
/// for strong left-to-right, strong right-to-left, number, and neutral characters, then
/// reorders one wrapped line into visual order. Explicit embedding controls (LRE/RLE,
/// isolates) are treated as neutral.
/// </summary>
internal static class BidiAlgorithm
{
    private enum CharClass
    {
        Left,
        Right,
        Number,
        Neutral,
    }

    /// <summary>True when the text contains any strong right-to-left character.</summary>
    internal static bool HasRtl(string text) => text.Any(c => Classify(c) == CharClass.Right);

    /// <summary>
    /// Returns the line in visual order for the given base direction. Right-to-left runs
    /// are reversed character-wise (renderers draw glyphs left to right); numbers keep
    /// their internal left-to-right order.
    /// </summary>
    internal static string ReorderVisual(string line, ContentDirection baseDirection)
    {
        if (line.Length == 0)
            return line;
        var baseLevel = baseDirection == ContentDirection.RightToLeft ? 1 : 0;
        if (baseLevel == 0 && !HasRtl(line))
            return line;

        var levels = ResolveLevels(line, baseLevel);

        // L2: from the highest level down to the lowest odd level, reverse every
        // maximal run of characters at or above that level.
        var chars = line.ToCharArray();
        var indices = Enumerable.Range(0, chars.Length).ToArray();
        var max = levels.Max();
        for (var level = max; level >= 1; level--)
        {
            var i = 0;
            while (i < chars.Length)
            {
                if (levels[indices[i]] >= level)
                {
                    var j = i;
                    while (j < chars.Length && levels[indices[j]] >= level)
                        j++;
                    Array.Reverse(indices, i, j - i);
                    i = j;
                }
                else
                {
                    i++;
                }
            }
        }

        return new string(indices.Select(i => chars[i]).ToArray());
    }

    private static int[] ResolveLevels(string line, int baseLevel)
    {
        var classes = line.Select(Classify).ToArray();

        // W-ish rules: numbers adjacent to right-to-left context stay LTR internally
        // (level base+2); neutrals take the surrounding direction when both sides agree,
        // otherwise the base direction.
        var resolved = new CharClass[classes.Length];
        Array.Copy(classes, resolved, classes.Length);
        for (var i = 0; i < resolved.Length; i++)
        {
            if (resolved[i] != CharClass.Neutral)
                continue;
            var prev = PrevStrong(resolved, i);
            var next = NextStrong(resolved, i);
            resolved[i] = prev == next && prev != CharClass.Neutral
                ? prev
                : baseLevel == 1 ? CharClass.Right : CharClass.Left;
        }

        var levels = new int[resolved.Length];
        for (var i = 0; i < resolved.Length; i++)
            levels[i] = resolved[i] switch
            {
                CharClass.Left => baseLevel == 1 ? 2 : 0,
                CharClass.Right => 1,
                CharClass.Number => baseLevel == 1 || InRtlContext(resolved, i) ? 2 : 0,
                _ => baseLevel,
            };
        return levels;
    }

    private static bool InRtlContext(CharClass[] classes, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (classes[i] == CharClass.Right)
                return true;
            if (classes[i] == CharClass.Left)
                return false;
        }
        return false;
    }

    private static CharClass PrevStrong(CharClass[] classes, int index)
    {
        for (var i = index - 1; i >= 0; i--)
            if (classes[i] is CharClass.Left or CharClass.Right)
                return classes[i];
        return CharClass.Neutral;
    }

    private static CharClass NextStrong(CharClass[] classes, int index)
    {
        for (var i = index + 1; i < classes.Length; i++)
            if (classes[i] is CharClass.Left or CharClass.Right)
                return classes[i];
        return CharClass.Neutral;
    }

    private static CharClass Classify(char c) => c switch
    {
        >= '0' and <= '9' => CharClass.Number,
        >= '֐' and <= '׿' => CharClass.Right,           // Hebrew
        >= '؀' and <= 'ۿ' => c is >= '٠' and <= '٩'
            ? CharClass.Number                                     // Arabic-Indic digits
            : CharClass.Right,                                     // Arabic
        >= '܀' and <= 'ࣿ' => CharClass.Right,           // Syriac..Arabic Extended
        >= 'יִ' and <= '﷿' => CharClass.Right,           // Hebrew/Arabic presentation forms
        >= 'ﹰ' and <= '﻿' => CharClass.Right,           // Arabic presentation forms B
        _ when char.IsLetter(c) => CharClass.Left,
        _ => CharClass.Neutral,
    };
}
