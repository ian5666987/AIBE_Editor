using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class InclusionInfoTest : CommonBaseInfo {
    public List<string> Roles { get; private set; } = new List<string>();
    public InclusionInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Inclusion Info";
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

    /// <summary>
    /// If roles (distinction) is not specificed, or the specific role is specified, or the role is admin, then it is true
    /// </summary>
    /// <param name="role">
    /// The role to check the inclusion
    /// </param>
    /// <returns></returns>
    public bool IsForcelyIncluded(string role) { 
      if (Aibe.DH.MainAdminRoles.Any(x => x.EqualsIgnoreCase(role)))
        return true;
      return Roles == null || !Roles.Any() || Roles.Any(x => x.EqualsIgnoreCase(role));
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}