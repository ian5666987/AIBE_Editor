using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class ActionInfoTest : CommonBaseInfo {
    public List<string> Roles { get; private set; } = new List<string>();
    public ActionInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Action Info";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      List<string> rowActions = Aibe.DH.DefaultRowActions.Union(Aibe.DH.DefaultGroupByRowActions).ToList();
      CheckerResult.Result = true;
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Action",
        Description = Name,
        Result = true,
        Message = (rowActions.Any(x => x.EqualsIgnoreCase(Name)) ? "Default" : "Custom") + " Row Action",
      };
      CheckerResult.SubResults.Add(subResult);

      if (HasRightSide) {
        Roles = RightSide.GetTrimmedNonEmptyParts(',');
        foreach(var role in Roles) {
          SyntaxCheckerResult roleResult = new SyntaxCheckerResult();
          roleResult.DisplayText = "Role";
          bool isValidRole = logic.Roles.Any(x => x.EqualsIgnoreCase(role));
          bool isSpecialRole = Aibe.DH.SpecialRoles.Any(x => x.EqualsIgnoreCase(role));
          roleResult.Result = isValidRole || isSpecialRole;
          roleResult.Description = role;
          roleResult.Message = roleResult.Result ? isValidRole ? "Valid Role" : "Special Role" : 
            "Invalid Role. Valid Roles: " + string.Join(", ", logic.CompleteRoles);
          CheckerResult.SubResults.Add(roleResult);
        }
        if (!Roles.Any())
          subResult.Message += ", Roles: All";
      } else
        subResult.Message += ", Roles: All";
    }

    public bool IsAllowed(string role) {
      return Aibe.DH.MainAdminRoles.Any(x => x.EqualsIgnoreCase(role)) || //main admin roles are always allowed
          (!string.IsNullOrWhiteSpace(role) &&
        (Roles == null || !Roles.Any() || Roles.Any(x => x.EqualsIgnoreCase(role))));
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}