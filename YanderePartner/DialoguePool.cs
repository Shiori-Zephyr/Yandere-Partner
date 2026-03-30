namespace YanderePartner;

public static class DialoguePool
{
    private static readonly Random Rng = new();

    public static string Pick(string[] pool) => pool[Rng.Next(pool.Length)];

    public static readonly string[] SepTerritoryChanged =
    [
        "You moved.",
        "I memorized your last location. Did you know that?",
        "Somewhere new. Somewhere I haven't checked yet.",
        "You didn't tell me you were leaving. You never tell me.",
        "I had the route from your last spot memorized and now it's useless.",
        "New coordinates. I'm updating my notes.",
        "I had a dream about you last night. You...Forget it.",
    ];

    public static readonly string[] SepLogout =
    [
        "Okay.",
        "You died on me. That's what logging out is. A little death.",
        "I counted. You stayed 4 hours and 23 minutes this time. That's less than yesterday.",
        "Go. I'll be here when you come back. I'm always here. That's the difference between us.",
        "The screen went dark. I'm talking to nothing now.",
        "Do you just stop thinking about me the moment you close the client?",
        "Bye. Bye bye bye bye bye bye bye. Adieu.",
        "I reread everything you said today. You used my name once. Once.",
    ];

    public static readonly string[] SepBetweenAreas =
    [
        "I can't see you right now.",
        "You're nowhere. I hate that.",
        "Loading. Loading resonates with No, right?",
        "Come back come back come back STOP DOING THAT",
        "I keep thinking about something you said three days ago. It doesn't matter. Nevermind. Just go away.",
    ];

    public static readonly string[] SepMounted =
    [
        "That thing gets to carry you and I don't.",
        "You look happy up there.",
        "Going somewhere.",
    ];

    public static readonly string[] SepMountedDismount =
    [
        "You stopped. Why here?",
        "Who's here.",
        "Oh. You got off. Fina- no, nothing.",
        "What's at this spot that I can't give you?",
    ];

    public static readonly string[] SepInFlight =
    [
        "You're so high up.",
        "I can't reach you when you fly.",
        "The ground misses you. I miss you. Same thing.",
        "Come down.",
        "Do you ever think about what I do when you're not looking? No. You don't.",
    ];

    public static readonly string[] PosTellReceived =
    [
        "?.",
        "I saw that. The message. I saw it.",
        "You smiled. You read something and you smiled. What did it say?",
        "Tell me their name. I just want to know. I won't do anything. We are going to trust each other, right",
        "Private messages. Private. The word itself is a betrayal.",
        "Was it funny? Was it funnier than me?",
        "Sometimes I write messages to you and delete them before sending. Check you discord now and you will see I'm typing",
    ];

    public static readonly string[] PosPartyChanged =
    [
        "New names in the list. I'm memorizing them.",
        "You replaced someone. That's what you do. You replace people.",
        "How long did it take you to invite them? Did you hesitate at all?",
        "​All the world's a stage",
        "I looked up their Lodestone. It's private. So they might have the rings says your name.",
        "Do you talk about me when I'm not there? No. You don't talk about me at all.",
    ];

    public static readonly string[] PosCfPop =
    [
        "The queue popped. Your heartbeat changed. I felt it.",
        "Strangers again.",
        "You pressed accept so fast.",
        "Off you go. Into a box with people I can't vet.",
        "Again, again, again.",
    ];

    public static readonly string[] PosDutyStarted =
    [
        "Don't talk to them more than you have to.",
        "It started. I will miss you",
        "I'll wait.",
        "Hope you don't die. No please die so i can see you soon.",
        "Joining a parade?",
        "Are they your friends?",
    ];

    public static readonly string[] PosRepairRequest =
    [
        "You could have used a mender. You chose a person. A specific person.",
        "Letting someone else handle your gear. That's trust. Why do they get that?",
        "Their hands on your equipment. Fine.",
        "How close do you have to stand for that? Show me.",
        "You'd never let me touch your things like that.",
        "Click, click, click.",
    ];

    public static readonly string[] EvaDutyCompleted =
    [
        "I use the words you taught me. If they don't mean anything any more, teach me others. Or let me be silent.",
        "Did they commend you? Did you commend them? Mutality is always good when I am not there.",
        "Good. It's over. They don't need you anymore.",
        "I watched you the whole time. You were incredible.",
        "You looked so alive in there. You never look like that with me.",
        "Comms given: how many? To who?",
        "I love thou so much",
    ];

    public static readonly string[] EvaDeath =
    [
        "No. No no no no no no no.",
        "Get up.",
        "Who let that happen. Give me a name.",
        "You were on the floor and they just kept fighting. They didn't even stop.",
        "I felt it. When your HP hit zero I felt something snap.",
        "The healer. It was the healer's fault. Say it. Say it was their fault.",
        "Don't do that again. I can't—just don't do that again.",
        "Are they leather-bound pounds?",
        "Just log out and stop hanging out with them. NO DONT. STAY.",
    ];

    public static readonly string[] EvaDutyWiped =
    [
        "Good. They all died too. They deserve it for letting you die.",
        "Too bad I wasn't there to save you. You could have had me, you know.",
        "They failed you. Every single one of them.",
        "Everyone's on the floor. At least you're not alone in that, I guess.",
        "Whose fault? I need to know, I am definitely not paying the bald guy to eliminate them.",
        "Great, now feed the cat.",
    ];

    public static readonly string[] EvaDutyRecommenced =
    [
        "Again? You're giving them another chance? For what?",
        "How many times are you going to carry them?",
        "whywhywhywhywhywhywhy",
        "You keep going back. Like it's nothing. Like wiping with them is fun.",
    ];

    public static readonly string[] EvaLootObtained =
    [
        "What did you get?",
        "We should sell it to get some lorazepam.",
        "Is it pretty? Prettier than me?",
        "After, days?months? I lose my feelings on time.",
        "I've been collecting things too. In my own way. You just can't see my inventory.",
    ];

    public static readonly string[] SurFishing =
    [
        "Fishing.",
        "You're just... sitting there. Staring at water.",
        "I've been talking and you're fishing.",
        "What's the name of that song? You know the song silence my old friend or anything like that. Yes yes yes you are not going to answer.",
        "Don't be Tuna, we can just order it from sushi takeouts.",
        "Fishy-fishy!",
    ];

    public static readonly string[] SurCrafting =
    [
        "Your hands are moving. I'm watching them.",
        "Who is this for? Don't say the marketboard. Nonononono it rhymes i hate myself where you put the knife on.",
        "Success! Or succeed? I don't know English actually",
        "This for me?",
        "Ups and downs. I use the phrase to describe your crafting. Topsy-turvy!",
    ];

    public static readonly string[] SurCraftFinishedSuccess =
    [
        "It's done. Don't sell it. Don't give it away.",
        "HQ? Is it hq. Why does uppercases matter.",
        "You smiled. Was it satisfaction? Or excitement to give it to someone?",
    ];

    public static readonly string[] SurCraftFinishedFail =
    [
        "Good.",
        "Try Again. Fail Again. Fail Better. He never means that in a positive way, you know that right.",
        "You failed. Now you have nothing to give anyone. Stay here with me instead.",
        "Try again. I want to watch your face when it fails again. Wait. That came out wrong. Don-",
    ];

    public static readonly string[] SurGathering =
    [
        "You're out here picking things off the ground again.",
        "Always with the rocks and the trees. Am I less interesting than rocks and trees?",
        "Gathering. The one thing you do where you don't need anyone. Including me.",
    ];

    public static readonly string[] SurGPose =
    [
        "Who's in the frame.",
        "You're posing. For who? Who's going to see this? You post it on? you gonna use gposers tag right everyone will see your face",
        "Take one of me.",
        "I went through your screenshots folder. I have questions.",
        "Is the weather right? Everying is ok? what about me?",
        "I saved every screenshot you've ever taken.",
    ];

    public static readonly string[] SurPerformance =
    [
        "That song. Who taught you that song?",
        "You're playing for them. Everyone walking by gets to hear you.I am happy with that.",
        "I want a song. Just for me. One nobody else has ever heard.",
        "No, it shoule be c#9b13 here. No no no, it will resolve to the next chord. Just do it",
        "Is it anime songs?",
    ];

    public static readonly string[] SurGearsetChange =
    [
        "New outfit.",
        "You changed. Who are you now?",
        "It's not the clothes I bought for you.",
        "Ok you are going to take me to the shop after this.",
        "I liked who you were five minutes ago better.",
    ];

    public static readonly string[] SurGearsetChangeHealer =
    [
        "Healer. So you're going to put your hands on other people.",
        "You chose to heal. That means you chose them over me.",
        "Your magic is going to flow into someone else's body. That's so sweet.",
        "Every HP you restore is a touch I didn't get.",
        "I got hurt on purpose once to see if you'd notice. You didn't.",
    ];

    public static readonly string[] SurGearsetChangeTank =
    [
        "You're going to stand between them and danger.",
        "Tank. Protector. Of everyone except me.",
        "You'll bleed for them. Would you bleed for me?",
    ];

    public static readonly string[] SurGearsetUpdate =
    [
        "You're optimizing again. Always perfecting yourself. For me. I am so sure.",
        "New gearset saved. You put more thought into stat weights than into us.",
        "Meticulous. That's the word for you. Meticulous about everything that isn't me.",
    ];

    public static readonly string[] SurGlamour =
    [
        "Who are you dressing up for?",
        "You never asked me what I think you'd look good in.",
        "New look. I hate it. No—I love it. That's worse. I don't know! Why are you doing that.",
        "You're trying to be pretty. You're already—nevermind.",
        "That chest piece. Where did you get it? Who gave it to you?",
        "I remember every glamour you've ever worn. The third one was my favorite. Was.",
    ];

    public static readonly string[] SurSummoningBell =
    [
        "Your retainers. Your little peasants who do whatever you say.",
        "I can do that for you. No i can't i am just a pathetic weirdo.",
        "The bell. You ring it and they come. I wish you'd ring something for me.",
        "How many retainers do you have? Do you have names for all of them? Did you name any of them after someone you know? I want to name one of them Agamben.",
        "I gave myself a name but you never asked what it is.",
    ];

    public static readonly string[] SurRetainerSale =
    [
        "Someone bought your things. A stranger takes things from you.",
        "Gil from someone you'll never meet. That's intimacy without commitment. You'd like that.",
        "A sale. You look pleased. Money from faceless people makes you happy.",
    ];

    public static readonly string[] OutCritDh =
    [
        "That was—",
        "The numbers.",
        "CRITTTTTTT",
        "More.",
        "Why are you doing that without my buffs",
    ];

    public static readonly string[] OutHealOther =
    [
        "You're touching them.",
        "Just use hot.",
        "I thought I am the one that should be healed.",
        "Wow you've done a great job.",
        "I memorized the cast time. 1.5 seconds of you thinking about someone else. Or 2.5. I hate those programmers. Or are you getting SS?",
    ];

    public static readonly string[] OutHealCrit =
    [
        "A critical heal. On them. You put everything into that and it went to someone who isn't me.",
        "You cared that much? About their HP?",
        "Just- don't.",
    ];

    public static readonly string[] OutFateEnter =
    [
        "A FATE. You dropped what you were doing for strangers.",
        "You're helping people you don't know. Again. How generous of you. How generous.",
        "Work, work.",
    ];

    public static readonly string[] OutFateLeave =
    [
        "Done playing hero?",
        "It's over. Did they thank you? Did any of them even notice you?",
        "Did you finish the fate alone? Or with someone else? Even a stanger?",
    ];

    public static readonly string[] SpcDeepDungeonEnter =
    [
        "You're going in there. I can't see inside. I can't see you.",
        "Nietzsche moment.",
        "It's dark where you're going. Come back. comebackcomebackcomeback",
        "Don't die in there. If you die where I can't see it I'll never forgive you.",
    ];

    public static readonly string[] SpcDeepDungeonFloor =
    [
        "Deeper.",
        "Another floor. You're getting fainter.",
        "How far are you going to go? There's a limit, right? Tell me there's a limit.",
        "I am calling you.",
        "When you come back, will you still be the same? Will you still love me?",
    ];

    public static readonly string[] SpcOceanFishingEnter =
    [
        "You're on a boat. With people. In the middle of the ocean. I can't reach you.",
        "Who else is on that boat? How many? What are their names?",
        "Trapped on a vessel with strangers. Or friiiiiends?",
        "My uncle used to fish.",
        "Is this for our dinner?",
    ];

    public static readonly string[] SpcOceanFishingZone =
    [
        "New waters. You're further from shore now.",
        "Another zone. When does this end? When do you come back to land? To me?",
    ];

    public static readonly string[] SpcChocoboRacing =
    [
        "Racing. You look happy. I hate that you look happy.",
        "That chocobo gets to feel you on its back. Lucky.",
        "Woo-hoo, acting like I am still with you.",
        "I timed how long you've spent at the Gold Saucer this week. It's more than you've spent with me.",
    ];

    public static readonly string[] SpcChocoboRacingResult =
    [
        "Did you win? Tell me you won. I need you to win.",
        "It's over. Come back. The chocobo doesn't need you like I do.",
    ];

    public static readonly string[] SpcGcTurnin =
    [
        "Seals. You grind for currency from an organization treat you as no-one.",
        "You're so diligent. For them. For the Grand Company. For literally anyone who asks.",
    ];

    public static readonly string[] SpcLeve =
    [
        "I know. Work is important. I know.",
        "Done. You finish everything you start. Except this. Except us.",
    ];

    public static readonly string[] EqpLowDurability =
    [
        "Your gear is breaking. You're breaking. Are you doing this on purpose?",
        "Everything on you is falling apart and you haven't noticed. You never notice.",
    ];

    public static readonly string[] EqpRepair =
    [
        "You care for your equipment like it matters. It does. More than I do, apparently.",
        "All repaired. Back to full. Now go back out there and get it broken again.",
        "You take such good care of your things.",
    ];

    public static readonly string[] EqpSpiritbondFull =
    [
        "Full spiritbond. You and that gear, closer than we'll ever be.",
        "I always think of something that you've always been with and something will starts to like you and benefit you.",
        "I wonder what my spiritbond with you would look like. I wonder if it would ever hit 100.",
    ];
}
