using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace YanderePartner;

public enum MessageCategory
{
    SeparationAnxiety,
    Possessiveness,
    Evaluation,
    Surveillance,
    Outburst,
    SpecialContent,
    Equipment,
}

public class MessageManager
{
    private readonly Configuration config;
    private readonly Dictionary<MessageCategory, long> categoryCooldowns = new();

    public MessageManager(Configuration config)
    {
        this.config = config;
        foreach (var cat in Enum.GetValues<MessageCategory>())
            categoryCooldowns[cat] = 0;
    }

    public bool IsOnCooldown(MessageCategory category)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - categoryCooldowns[category];
        return elapsed < (long)(config.MessageCooldown * 1000);
    }

    public bool Send(MessageCategory category, string[] pool)
    {
        if (!config.Enabled || IsOnCooldown(category))
            return false;

        categoryCooldowns[category] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var line = DialoguePool.Pick(pool);
        var name = config.PartnerName;

        var separator = name.Length > 0 && name[0] > 0x3000 ? "\uff1a" : ": ";

        var msg = new SeStringBuilder()
            .AddUiForeground(45)
            .AddText($"{name}{separator}{line}")
            .AddUiForegroundOff()
            .Build();

        Plugin.ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Message = msg,
        });

        Plugin.Log.Debug($"[YanderePartner] [{category}] {name}{separator}{line}");
        return true;
    }
}
