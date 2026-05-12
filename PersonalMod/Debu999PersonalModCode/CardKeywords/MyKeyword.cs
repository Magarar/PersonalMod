using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace PersonalMod.PersonalModCode.CardKeywords;

[RegisterOwnedCardKeyword(nameof(Combo), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Rapid), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(BURST), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Overcharge), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Accelerate), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Crystallize), IconPath = "res://icon.svg", CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
public class MyKeywords
{
    public static readonly string Combo = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Combo));
    public static readonly string Rapid = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Rapid));
    public static readonly string BURST = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(BURST));
    public static readonly string Overcharge = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Overcharge));
    public static readonly string Accelerate = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Accelerate));
    public static readonly string Crystallize = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Crystallize));
}