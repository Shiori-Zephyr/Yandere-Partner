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
    private readonly Uwuifier uwuifier = new()
    {
        WordsModifier = 0.9,
        FacesModifier = 0.05,
        ActionsModifier = 0.03,
        StuttersModifier = 0.15,
        ExclamationsModifier = 1.0,
        AdvancedEnabled = true,
        BabyTalkModifier = 0.4,
        ElongationModifier = 0.35,
        KeysmashModifier = 0.04,
    };
    private PopupWindow? popupWindow;

    private long lastSendTimestamp;
    private long dissociationStartedAt;

    private const long IdleThresholdMs = 5 * 60 * 1000;
    private const long DissociationDurationMs = 10 * 60 * 1000;

    public MessageManager(Configuration config)
    {
        this.config = config;
        foreach (var cat in Enum.GetValues<MessageCategory>())
            categoryCooldowns[cat] = 0;
        lastSendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void SetPopupWindow(PopupWindow window) => popupWindow = window;

    public bool IsOnCooldown(MessageCategory category)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - categoryCooldowns[category];
        return elapsed < (long)(config.MessageCooldown * 1000);
    }

    public bool Send(MessageCategory category, string[] pool)
    {
        if (!config.Enabled || IsOnCooldown(category))
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        categoryCooldowns[category] = now;

        var line = DialoguePool.Pick(pool);

        if (config.Dissociation)
        {
            var idleMs = now - lastSendTimestamp;

            if (dissociationStartedAt > 0 && now - dissociationStartedAt < DissociationDurationMs)
            {
                line = uwuifier.UwuifySentence(line);
            }
            else if (idleMs >= IdleThresholdMs && dissociationStartedAt == 0)
            {
                dissociationStartedAt = now;
                line = uwuifier.UwuifySentence(line);
            }
            else if (dissociationStartedAt > 0 && now - dissociationStartedAt >= DissociationDurationMs)
            {
                dissociationStartedAt = 0;
            }
        }

        lastSendTimestamp = now;

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

        popupWindow?.QueueMessage(line, category);

        Plugin.Log.Debug($"[YanderePartner] [{category}] {name}{separator}{line}");
        return true;
    }
}
