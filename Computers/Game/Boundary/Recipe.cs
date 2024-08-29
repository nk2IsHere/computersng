using StardewModdingAPI.Enums;

namespace Computers.Game.Boundary;

public interface IRecipeRequirement {
    public string PatchString { get; }

    public record NoneRequired : IRecipeRequirement {
        public string PatchString => "none";
    }
    
    public record SkillRequired(SkillType Skill, int Level) : IRecipeRequirement {
        public string PatchString => $"{Skill} {Level}";
    }
}

public record Recipe(
    string Id,
    string YieldId,
    Dictionary<string, int> Ingredients,
    bool IsBigCraftable,
    IRecipeRequirement SkillRequirement,
    string DisplayName
) {
    public string PatchString {
        get {
            var ingredients = string.Join(" ", Ingredients.Select(pair => $"{pair.Key} {pair.Value}"));
            var skillRequirement = SkillRequirement.PatchString;
            return $"{ingredients}/Field/{YieldId}/{IsBigCraftable}/{skillRequirement}/{DisplayName}";
        }
    }
}
