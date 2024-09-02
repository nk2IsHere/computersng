using StardewModdingAPI.Enums;

namespace Computers.Game.Boundary;

public interface IRecipeRequirement {
    public string PatchString { get; }

    public record NoneRequired : IRecipeRequirement {
        public string PatchString => "default";
    }
    
    public record SkillRequired(SkillType Skill, int Level) : IRecipeRequirement {
        public string PatchString => $"{Skill} {Level}";
    }
}

public record Recipe(
    string YieldId,
    Dictionary<string, int> Ingredients,
    bool IsBigCraftable,
    IRecipeRequirement SkillRequirement,
    string DisplayName,
    string? Description = null
) {
    
    //[Wood Fence, 388 2/Field/322/false/default/]

    public string PatchKey {
        get {
            return DisplayName;
        }
    }
    
    public string PatchString {
        get {
            var ingredients = string.Join(" ", Ingredients.Select(pair => $"{pair.Key} {pair.Value}"));
            var skillRequirement = SkillRequirement.PatchString;
            return $"{ingredients}/Field/{YieldId}/{IsBigCraftable}/{skillRequirement}/{Description}";
        }
    }
}
