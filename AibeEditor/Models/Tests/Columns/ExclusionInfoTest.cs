using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class ExclusionInfoTest : CommonBaseInfo {
    public List<string> Roles { get; private set; } = new List<string>();
    public ExclusionInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Exclusion Info";
      if (HasRightSide) {
        Roles = RightSide.GetTrimmedNonEmptyParts(',');
        foreach (var role in Roles) {
          SyntaxCheckerResult subResult = new SyntaxCheckerResult();
          subResult.DisplayText = "Role";
          bool isValidRole = logic.Roles.Any(x => x.EqualsIgnoreCase(role));
          bool isSpecialRole = Aibe.DH.SpecialRoles.Any(x => x.EqualsIgnoreCase(role));
          subResult.Result = isValidRole || isSpecialRole;
          subResult.Description = role;
          subResult.Message = subResult.Result ? isValidRole ? "Valid Role" : "Special Role" : 
            "Invalid Role. Valid Roles: " + string.Join(", ", logic.CompleteRoles);
          CheckerResult.SubResults.Add(subResult);
        }
      }

      CheckerResult.Result = true;
    }

    public bool IsExcluded(string role) { //if roles are empty means no body is allowed
      if (Aibe.DH.MainAdminRoles.Any(x => x.EqualsIgnoreCase(role))) //role in the main admin roles cannot be excluded
        return false;
      return Roles == null || Roles.Any(x => x.EqualsIgnoreCase(role));
    }

    public override string ToString() {
      return Name.ToString() + (Roles != null && Roles.Count > 0 ? " {" + string.Join(", ", Roles) + "}" : " {All}");
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}