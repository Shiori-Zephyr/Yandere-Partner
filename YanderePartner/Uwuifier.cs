using System.Text;
using System.Text.RegularExpressions;

namespace YanderePartner;

public class Uwuifier
{
    public string[] Faces =
    [
        "(・`ω´・)", ";;w;;", "OwO", "UwU", ">w<",
        "^w^", "ÚwÚ", "^-^", ":3", "x3",
    ];

    public string[] Exclamations = ["!?", "?!!", "?!?1", "!!11", "?!?!"];

    public string[] Actions =
    [
        "*blushes*", "*whispers to self*", "*cries*", "*screams*",
        "*sweats*", "*twerks*", "*runs away*", "*screeches*",
        "*walks away*", "*sees bulge*", "*looks at you*",
        "*notices buldge*", "*starts twerking*", "*huggles tightly*",
        "*boops your nose*",
        "*nuzzles*", "*pounces on you*", "*wiggles*", "*hides*",
        "*peeks*", "*giggles*", "*falls over*", "*melts*",
        "*purrs*", "*rolls around*", "*squeaks*",
    ];

    private static readonly string[] Keysmashes =
    [
        "asjkfhg", "skjdhfg", "dkfjghsd", "ajshdgf", "ksjdhfgj",
        "hhhhhh", "aaaaaaa", "ahhhhhh",
    ];

    private static readonly (Regex Pat, string Rep)[] UwuMap =
    [
        (new Regex("[rl]", RegexOptions.Compiled), "w"),
        (new Regex("[RL]", RegexOptions.Compiled), "W"),
        (new Regex("n([aeiou])", RegexOptions.Compiled), "ny$1"),
        (new Regex("N([aeiou])", RegexOptions.Compiled), "Ny$1"),
        (new Regex("N([AEIOU])", RegexOptions.Compiled), "Ny$1"),
        (new Regex("ove", RegexOptions.Compiled), "uv"),
    ];

    private static readonly (string Word, string Rep)[] BabyTalkMap =
    [
        ("the", "da"),
        ("The", "Da"),
        ("THE", "DA"),
        ("this", "dis"),
        ("This", "Dis"),
        ("THIS", "DIS"),
        ("that", "dat"),
        ("That", "Dat"),
        ("THAT", "DAT"),
        ("though", "doe"),
        ("Though", "Doe"),
        ("THOUGH", "DOE"),
        ("you", "u"),
        ("You", "U"),
        ("YOU", "U"),
        ("your", "ur"),
        ("Your", "Ur"),
        ("YOUR", "UR"),
        ("my", "mah"),
        ("My", "Mah"),
        ("MY", "MAH"),
    ];

    private static readonly HashSet<char> Elongatable = ['a', 'e', 'i', 'o', 'u', 's', 'n', 'w'];

    private static readonly Regex UriPattern = new(@"https?://\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ExclPattern = new(@"[?!]+$", RegexOptions.Compiled);
    private static readonly Regex PuncPattern = new(@"[.!?\-]$", RegexOptions.Compiled);

    public double WordsModifier { get; set; } = 0.9;
    public double FacesModifier { get; set; } = 0.04;
    public double ActionsModifier { get; set; } = 0.02;
    public double StuttersModifier { get; set; } = 0.1;
    public double ExclamationsModifier { get; set; } = 1.0;

    public bool AdvancedEnabled { get; set; }
    public double BabyTalkModifier { get; set; } = 0.5;
    public double ElongationModifier { get; set; } = 0.3;
    public double KeysmashModifier { get; set; } = 0.03;

    public string UwuifySentence(string sentence)
    {
        var words = sentence.Split(' ');
        UwuifyWordsPass(words);
        if (AdvancedEnabled)
            BabyTalkPass(words);
        return InsertSpaceEffects(words);
    }

    private void UwuifyWordsPass(string[] words)
    {
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (w.StartsWith('@') || UriPattern.IsMatch(w) || HasPayloadChar(w))
                continue;

            var rng = new SeededRandom(w);

            foreach (var (pat, rep) in UwuMap)
            {
                if (rng.Random() <= WordsModifier)
                    w = pat.Replace(w, rep);
            }

            if (ExclPattern.IsMatch(w))
            {
                var rng2 = new SeededRandom(w);
                if (rng2.Random() <= ExclamationsModifier)
                {
                    w = ExclPattern.Replace(w, "");
                    w += Exclamations[rng2.RandomInt(0, Exclamations.Length - 1)];
                }
            }

            words[i] = w;
        }
    }

    private void BabyTalkPass(string[] words)
    {
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (w.StartsWith('@') || UriPattern.IsMatch(w) || HasPayloadChar(w))
                continue;

            var stripped = w.TrimEnd('.', ',', '!', '?', '~');
            var suffix = w[stripped.Length..];

            var btRng = new SeededRandom("bt" + w + i);
            foreach (var (word, rep) in BabyTalkMap)
            {
                if (stripped.Equals(word, StringComparison.Ordinal) && btRng.Random() <= BabyTalkModifier)
                {
                    stripped = rep;
                    break;
                }
            }

            var elRng = new SeededRandom("el" + w + i);
            if (elRng.Random() <= ElongationModifier && stripped.Length > 1)
            {
                var last = char.ToLower(stripped[^1]);
                if (Elongatable.Contains(last))
                {
                    var count = elRng.RandomInt(2, 5);
                    stripped += new string(stripped[^1], count);
                }
            }

            if (suffix == ".")
            {
                var punctRng = new SeededRandom("punc" + w + i);
                if (punctRng.Random() <= ElongationModifier)
                    suffix = punctRng.Random() < 0.5 ? "~" : "...";
            }

            words[i] = stripped + suffix;
        }
    }

    private string InsertSpaceEffects(string[] words)
    {
        var faceThresh = FacesModifier;
        var actionThresh = ActionsModifier + faceThresh;
        var stutterThresh = StuttersModifier + actionThresh;

        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (string.IsNullOrEmpty(w) || HasPayloadChar(w)) continue;

            var rng = new SeededRandom(w);
            var roll = rng.Random();
            var first = w[0];

            if (roll <= faceThresh && Faces.Length > 0)
            {
                w += " " + Faces[rng.RandomInt(0, Faces.Length - 1)];
                w = FixCapital(words, i, w, first);
            }
            else if (roll <= actionThresh && Actions.Length > 0)
            {
                w += " " + Actions[rng.RandomInt(0, Actions.Length - 1)];
                w = FixCapital(words, i, w, first);
            }
            else if (roll <= stutterThresh && !UriPattern.IsMatch(w))
            {
                var n = rng.RandomInt(0, 2);
                w = string.Concat(Enumerable.Repeat(first + "-", n)) + w;
            }

            if (AdvancedEnabled)
            {
                var kRng = new SeededRandom("ks" + w + i);
                if (kRng.Random() <= KeysmashModifier)
                {
                    if (kRng.Random() < 0.6)
                        w += " " + Keysmashes[kRng.RandomInt(0, Keysmashes.Length - 1)];
                    else
                        w = w + " " + w + " " + w;
                }
            }

            words[i] = w;
        }
        return string.Join(" ", words);
    }

    private static string FixCapital(string[] words, int idx, string word, char first)
    {
        if (!char.IsUpper(first)) return word;

        int upper = 0;
        foreach (var c in word) { if (char.IsUpper(c)) upper++; }
        if ((double)upper / word.Length > 0.5) return word;

        if (idx == 0)
        {
            word = char.ToLower(first) + word[1..];
        }
        else
        {
            var prev = words[idx - 1];
            if (prev.Length > 0 && PuncPattern.IsMatch(prev[^1].ToString()))
                word = char.ToLower(first) + word[1..];
        }
        return word;
    }

    private static bool HasPayloadChar(string s)
    {
        foreach (var c in s)
            if (c is >= '\uE000' and <= '\uE0FF') return true;
        return false;
    }
}
