namespace NetPdf.Layout.Text;

/// <summary>
/// Contextual Arabic shaping via Unicode presentation forms: picks the isolated, final,
/// initial, or medial form of each letter based on its joining context and composes the
/// mandatory lam-alef ligatures. Rendering Arabic requires a font that carries the
/// presentation-form glyphs (U+FB50–FEFF), e.g. Arial, Noto Naskh Arabic, or Amiri.
/// </summary>
internal static class ArabicShaper
{
    private enum Joining
    {
        None,       // does not join (hamza)
        Right,      // joins only to the preceding letter (alef, dal, reh, waw, …)
        Dual,       // joins on both sides
        Transparent, // diacritics: invisible to joining
    }

    // char → (isolated, final, initial, medial); initial/medial are 0 for right-joining letters.
    private static readonly Dictionary<char, (char Iso, char Fin, char Ini, char Med)> Forms = new()
    {
        ['ء'] = ('ﺀ', '\0', '\0', '\0'),      // hamza
        ['آ'] = ('ﺁ', 'ﺂ', '\0', '\0'),  // alef madda
        ['أ'] = ('ﺃ', 'ﺄ', '\0', '\0'),  // alef hamza above
        ['ؤ'] = ('ﺅ', 'ﺆ', '\0', '\0'),  // waw hamza
        ['إ'] = ('ﺇ', 'ﺈ', '\0', '\0'),  // alef hamza below
        ['ئ'] = ('ﺉ', 'ﺊ', 'ﺋ', 'ﺌ'), // yeh hamza
        ['ا'] = ('ﺍ', 'ﺎ', '\0', '\0'),  // alef
        ['ب'] = ('ﺏ', 'ﺐ', 'ﺑ', 'ﺒ'), // beh
        ['ة'] = ('ﺓ', 'ﺔ', '\0', '\0'),  // teh marbuta
        ['ت'] = ('ﺕ', 'ﺖ', 'ﺗ', 'ﺘ'), // teh
        ['ث'] = ('ﺙ', 'ﺚ', 'ﺛ', 'ﺜ'), // theh
        ['ج'] = ('ﺝ', 'ﺞ', 'ﺟ', 'ﺠ'), // jeem
        ['ح'] = ('ﺡ', 'ﺢ', 'ﺣ', 'ﺤ'), // hah
        ['خ'] = ('ﺥ', 'ﺦ', 'ﺧ', 'ﺨ'), // khah
        ['د'] = ('ﺩ', 'ﺪ', '\0', '\0'),  // dal
        ['ذ'] = ('ﺫ', 'ﺬ', '\0', '\0'),  // thal
        ['ر'] = ('ﺭ', 'ﺮ', '\0', '\0'),  // reh
        ['ز'] = ('ﺯ', 'ﺰ', '\0', '\0'),  // zain
        ['س'] = ('ﺱ', 'ﺲ', 'ﺳ', 'ﺴ'), // seen
        ['ش'] = ('ﺵ', 'ﺶ', 'ﺷ', 'ﺸ'), // sheen
        ['ص'] = ('ﺹ', 'ﺺ', 'ﺻ', 'ﺼ'), // sad
        ['ض'] = ('ﺽ', 'ﺾ', 'ﺿ', 'ﻀ'), // dad
        ['ط'] = ('ﻁ', 'ﻂ', 'ﻃ', 'ﻄ'), // tah
        ['ظ'] = ('ﻅ', 'ﻆ', 'ﻇ', 'ﻈ'), // zah
        ['ع'] = ('ﻉ', 'ﻊ', 'ﻋ', 'ﻌ'), // ain
        ['غ'] = ('ﻍ', 'ﻎ', 'ﻏ', 'ﻐ'), // ghain
        ['ف'] = ('ﻑ', 'ﻒ', 'ﻓ', 'ﻔ'), // feh
        ['ق'] = ('ﻕ', 'ﻖ', 'ﻗ', 'ﻘ'), // qaf
        ['ك'] = ('ﻙ', 'ﻚ', 'ﻛ', 'ﻜ'), // kaf
        ['ل'] = ('ﻝ', 'ﻞ', 'ﻟ', 'ﻠ'), // lam
        ['م'] = ('ﻡ', 'ﻢ', 'ﻣ', 'ﻤ'), // meem
        ['ن'] = ('ﻥ', 'ﻦ', 'ﻧ', 'ﻨ'), // noon
        ['ه'] = ('ﻩ', 'ﻪ', 'ﻫ', 'ﻬ'), // heh
        ['و'] = ('ﻭ', 'ﻮ', '\0', '\0'),  // waw
        ['ى'] = ('ﻯ', 'ﻰ', '\0', '\0'),  // alef maksura
        ['ي'] = ('ﻱ', 'ﻲ', 'ﻳ', 'ﻴ'), // yeh
    };

    // lam + alef variant → (isolated, final) ligature.
    private static readonly Dictionary<char, (char Iso, char Fin)> LamAlef = new()
    {
        ['آ'] = ('ﻵ', 'ﻶ'),
        ['أ'] = ('ﻷ', 'ﻸ'),
        ['إ'] = ('ﻹ', 'ﻺ'),
        ['ا'] = ('ﻻ', 'ﻼ'),
    };

    /// <summary>Shapes Arabic letters into presentation forms; other text passes through unchanged.</summary>
    internal static string Shape(string text)
    {
        if (!text.Any(c => c is >= 'ء' and <= 'ي'))
            return text;

        var chars = text.ToCharArray();
        var result = new System.Text.StringBuilder(chars.Length);
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (JoiningOf(c) is Joining.None or Joining.Transparent && !Forms.ContainsKey(c))
            {
                result.Append(c);
                continue;
            }
            if (!Forms.TryGetValue(c, out var forms))
            {
                result.Append(c);
                continue;
            }

            // Lam-alef ligature: lam followed (skipping diacritics) by an alef variant.
            if (c == 'ل')
            {
                var nextIdx = NextSkippingTransparent(chars, i);
                if (nextIdx >= 0 && LamAlef.TryGetValue(chars[nextIdx], out var lig))
                {
                    var joinsPrev = JoinsToPrevious(chars, i);
                    result.Append(joinsPrev ? lig.Fin : lig.Iso);
                    // Emit any diacritics between lam and alef, then skip the alef.
                    for (var k = i + 1; k < nextIdx; k++)
                        result.Append(chars[k]);
                    i = nextIdx;
                    continue;
                }
            }

            var connectsPrev = JoinsToPrevious(chars, i);
            var connectsNext = forms.Ini != '\0' && JoinsToNext(chars, i);
            result.Append((connectsPrev, connectsNext) switch
            {
                (false, false) => forms.Iso,
                (true, false) => forms.Fin,
                (false, true) => forms.Ini,
                (true, true) => forms.Med,
            });
        }
        return result.ToString();
    }

    private static bool JoinsToPrevious(char[] chars, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            var j = JoiningOf(chars[i]);
            if (j == Joining.Transparent)
                continue;
            return j == Joining.Dual;
        }
        return false;
    }

    private static bool JoinsToNext(char[] chars, int index)
    {
        for (var i = index + 1; i < chars.Length; i++)
        {
            var j = JoiningOf(chars[i]);
            if (j == Joining.Transparent)
                continue;
            return j is Joining.Dual or Joining.Right;
        }
        return false;
    }

    private static int NextSkippingTransparent(char[] chars, int index)
    {
        for (var i = index + 1; i < chars.Length; i++)
        {
            if (JoiningOf(chars[i]) == Joining.Transparent)
                continue;
            return i;
        }
        return -1;
    }

    private static Joining JoiningOf(char c) => c switch
    {
        >= 'ً' and <= 'ٟ' or 'ٰ' => Joining.Transparent, // harakat
        'ء' => Joining.None,
        'آ' or 'أ' or 'ؤ' or 'إ' or 'ا' or 'ة'
            or 'د' or 'ذ' or 'ر' or 'ز' or 'و' or 'ى' => Joining.Right,
        >= 'ئ' and <= 'ي' => Joining.Dual,
        _ => Joining.None,
    };
}
