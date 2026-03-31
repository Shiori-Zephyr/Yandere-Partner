namespace YanderePartner;

public enum TypingActionKind { Append, Delete, Pause }

public struct TypingAction
{
    public TypingActionKind Kind;
    public char Char;
    public double Duration;
}

public class TypingEngine
{
    private static readonly Random Rng = new();

    private static readonly Dictionary<char, char[]> AdjacentKeys = new()
    {
        ['a'] = ['s', 'q', 'z', 'w'],
        ['b'] = ['v', 'g', 'h', 'n'],
        ['c'] = ['x', 'd', 'f', 'v'],
        ['d'] = ['s', 'e', 'f', 'c', 'x'],
        ['e'] = ['w', 'r', 'd', 's'],
        ['f'] = ['d', 'r', 'g', 'v', 'c'],
        ['g'] = ['f', 't', 'h', 'b', 'v'],
        ['h'] = ['g', 'y', 'j', 'n', 'b'],
        ['i'] = ['u', 'o', 'k', 'j'],
        ['j'] = ['h', 'u', 'k', 'n', 'm'],
        ['k'] = ['j', 'i', 'l', 'm'],
        ['l'] = ['k', 'o', 'p'],
        ['m'] = ['n', 'j', 'k'],
        ['n'] = ['b', 'h', 'j', 'm'],
        ['o'] = ['i', 'p', 'l', 'k'],
        ['p'] = ['o', 'l'],
        ['q'] = ['w', 'a'],
        ['r'] = ['e', 't', 'f', 'd'],
        ['s'] = ['a', 'w', 'd', 'z', 'x'],
        ['t'] = ['r', 'y', 'g', 'f'],
        ['u'] = ['y', 'i', 'j', 'h'],
        ['v'] = ['c', 'f', 'g', 'b'],
        ['w'] = ['q', 'e', 's', 'a'],
        ['x'] = ['z', 's', 'd', 'c'],
        ['y'] = ['t', 'u', 'h', 'g'],
        ['z'] = ['a', 's', 'x'],
    };

    private List<TypingAction> actions = new();
    private System.Text.StringBuilder displayBuffer = new();
    private int actionIndex;
    private double lastActionTime;
    private bool done;

    public string CurrentText => displayBuffer.ToString();
    public bool Finished => done;

    public void Start(string text, MessageCategory category, double startTime)
    {
        actions = BuildSequence(text, category);
        displayBuffer.Clear();
        actionIndex = 0;
        lastActionTime = startTime;
        done = false;
    }

    public void Tick(double now)
    {
        if (done) return;

        while (actionIndex < actions.Count)
        {
            var a = actions[actionIndex];
            if (now < lastActionTime + a.Duration)
                break;

            lastActionTime += a.Duration;

            switch (a.Kind)
            {
                case TypingActionKind.Append:
                    displayBuffer.Append(a.Char);
                    break;
                case TypingActionKind.Delete:
                    if (displayBuffer.Length > 0)
                        displayBuffer.Length--;
                    break;
                case TypingActionKind.Pause:
                    break;
            }

            actionIndex++;
        }

        if (actionIndex >= actions.Count)
            done = true;
    }

    public void Reset()
    {
        actions.Clear();
        displayBuffer.Clear();
        actionIndex = 0;
        done = true;
    }

    private static (double typoChance, double baseSpeed, double speedJitter) GetEmotionProfile(MessageCategory category)
    {
        return category switch
        {
            MessageCategory.Outburst => (0.14, 0.025, 0.03),
            MessageCategory.SeparationAnxiety => (0.10, 0.035, 0.04),
            MessageCategory.Possessiveness => (0.12, 0.030, 0.04),
            MessageCategory.Evaluation => (0.08, 0.040, 0.03),
            MessageCategory.Surveillance => (0.05, 0.045, 0.03),
            MessageCategory.Equipment => (0.04, 0.050, 0.02),
            MessageCategory.SpecialContent => (0.06, 0.040, 0.03),
            _ => (0.07, 0.040, 0.03),
        };
    }

    private static List<TypingAction> BuildSequence(string text, MessageCategory category)
    {
        var (typoChance, baseSpeed, speedJitter) = GetEmotionProfile(category);
        var result = new List<TypingAction>(text.Length * 2);

        for (int i = 0; i < text.Length; i++)
        {
            if (i > 3 && char.IsLetter(text[i]) && Rng.NextDouble() < typoChance)
            {
                var typoLen = Rng.Next(1, 4);
                for (int t = 0; t < typoLen; t++)
                {
                    var typoChar = PickAdjacentKey(text[Math.Min(i + t, text.Length - 1)]);
                    result.Add(new TypingAction
                    {
                        Kind = TypingActionKind.Append,
                        Char = typoChar,
                        Duration = baseSpeed * 0.7 + Rng.NextDouble() * speedJitter * 0.5,
                    });
                }

                bool keepTypo = Rng.NextDouble() < 0.2;

                if (keepTypo)
                    continue;

                result.Add(new TypingAction
                {
                    Kind = TypingActionKind.Pause,
                    Duration = 0.25 + Rng.NextDouble() * 0.55,
                });

                var deleteSpeed = 0.03 + Rng.NextDouble() * 0.02;
                for (int t = 0; t < typoLen; t++)
                    result.Add(new TypingAction
                    {
                        Kind = TypingActionKind.Delete,
                        Duration = deleteSpeed,
                    });

                result.Add(new TypingAction
                {
                    Kind = TypingActionKind.Pause,
                    Duration = 0.08 + Rng.NextDouble() * 0.12,
                });
            }

            double dur = text[i] switch
            {
                '.' => 0.20 + Rng.NextDouble() * 0.35,
                '!' or '?' => 0.15 + Rng.NextDouble() * 0.25,
                ',' => 0.10 + Rng.NextDouble() * 0.15,
                '-' => 0.12 + Rng.NextDouble() * 0.20,
                '…' or '—' => 0.30 + Rng.NextDouble() * 0.40,
                '\n' => 0.15 + Rng.NextDouble() * 0.20,
                ' ' => baseSpeed * 0.4 + Rng.NextDouble() * speedJitter * 0.3,
                _ => baseSpeed + Rng.NextDouble() * speedJitter,
            };

            result.Add(new TypingAction
            {
                Kind = TypingActionKind.Append,
                Char = text[i],
                Duration = dur,
            });
        }

        return result;
    }

    private static char PickAdjacentKey(char original)
    {
        var lower = char.ToLowerInvariant(original);
        if (AdjacentKeys.TryGetValue(lower, out var neighbors))
        {
            var picked = neighbors[Rng.Next(neighbors.Length)];
            return char.IsUpper(original) ? char.ToUpperInvariant(picked) : picked;
        }
        return (char)('a' + Rng.Next(26));
    }
}
