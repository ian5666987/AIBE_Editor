using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class UserRelatedFilterInfoTest : BaseInfo { //this is specially parsed and thus not derived from CommonBaseInfo, but BaseInfo
    public string ThisColumnName { get; private set; } //something like "TeamAssigned"
    public bool HasColumnFreeCandidate { get { return ThisColumnFreeCandidates != null && ThisColumnFreeCandidates.Count >= 0; } }
    public List<string> ThisColumnFreeCandidates { get; private set; } //having like {All:Any,Unassigned}
    public string UserInfoColumnName { get; private set; } //something like "Team"
    public bool HasUserInfoColumnFreeCandidate { get { return UserInfoColumnFreeCandidates != null && UserInfoColumnFreeCandidates.Count >= 0; } }
    public List<string> UserInfoColumnFreeCandidates { get; private set; } //having like {All:Home,Ruler}
    public string Relationship { get; private set; } = "=";
    public UserRelatedFilterInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "User Related Filter";
      var strs = desc.Split('=');
      if (strs.Length != 2) {
        CheckerResult.Message = "Insufficient Number of Components";
        return;
      }

      string leftStr = strs[0].Trim();
      string rightStr = strs[1].Trim();
      if (string.IsNullOrWhiteSpace(leftStr) || string.IsNullOrWhiteSpace(rightStr)) {
        CheckerResult.Message = "Null/Invalid Components";
        return;
      }

      var leftDivs = leftStr.GetTrimmedNonEmptyParts('|');
      var rightDivs = rightStr.GetTrimmedNonEmptyParts('|');

      if (leftDivs == null || rightDivs == null || leftDivs.Count <= 0 || rightDivs.Count <= 0) {
        CheckerResult.Message = "Null/Invalid Components";
        return;
      }

      ThisColumnName = leftDivs[0];
      SyntaxCheckerResult subResultOutside = new SyntaxCheckerResult {
        DisplayText = "Column (Data)",
        Description = leftDivs[0],
        Result = true,
        Message = "Column (Data): " + leftDivs[0],
      };
      CheckerResult.SubResults.Add(subResultOutside);

      if (leftDivs.Count > 1) {
        for (int i = 1; i < leftDivs.Count; ++i) {
          string leftDiv = leftDivs[i];
          SyntaxCheckerResult subResult = new SyntaxCheckerResult { };
          subResult.DisplayText = "Free Candidates (Data)";
          subResult.Description = leftDiv;
          if (string.IsNullOrWhiteSpace(leftDiv) || !leftDiv.StartsWith("{") || !leftDiv.EndsWith("}") || leftDiv.Length < 5) {
            subResult.Result = false;
            subResult.Message = "Invalid Candidates (Data) Description";
            CheckerResult.SubResults.Add(subResult);
            continue;
          }
          leftDiv = leftDiv.Substring(1, leftDiv.Length - 2);
          var parts = leftDiv.GetTrimmedNonEmptyParts(':');
          if (parts.Count < 2) {
            subResult.Result = false;
            subResult.Message = "Incomplete Candidates (Data) Description";
            CheckerResult.SubResults.Add(subResult);
            continue;
          }
          if (Aibe.DH.UserRelatedDirectives.Any(x => x.EqualsIgnoreCase(parts[0]))) {//as of now, only one candidate is acceptable: All
            SyntaxCheckerResult directiveResult = new SyntaxCheckerResult { };
            directiveResult.DisplayText = "Directive";
            directiveResult.Result = true;
            directiveResult.Description = parts[0];
            directiveResult.Message = "Valid Directive (Data)";
            subResult.SubResults.Add(directiveResult);
            ThisColumnFreeCandidates = parts[1].GetTrimmedNonEmptyParts(',');
            subResult.Result = true;
            foreach(var free in ThisColumnFreeCandidates) {
              SyntaxCheckerResult subSubResult = new SyntaxCheckerResult { };
              subSubResult.DisplayText = "Candidate";
              subSubResult.Result = true;
              subSubResult.Description = free;
              subSubResult.Message = "Valid Candidate (Data)";
              subResult.SubResults.Add(subSubResult);
            }
            CheckerResult.SubResults.Add(subResult);
          } else {
            subResult.Result = false;
            subResult.Message = "Invalid Directive (Data). Valid Directives: " + Aibe.DH.All;
            CheckerResult.SubResults.Add(subResult);
          }
        }
      }

      UserInfoColumnName = rightDivs[0];
      subResultOutside = new SyntaxCheckerResult {
        DisplayText = "Column (User)",
        Description = rightDivs[0],
        Result = true,
        Message = "Column (User): " + rightDivs[0],
      };
      CheckerResult.SubResults.Add(subResultOutside);

      if (rightDivs.Count > 1) {
        for (int i = 1; i < rightDivs.Count; ++i) {
          string rightDiv = rightDivs[i].Trim();
          SyntaxCheckerResult subResult = new SyntaxCheckerResult { };
          subResult.DisplayText = "Free Candidates (User)";
          subResult.Description = rightDiv;
          if (string.IsNullOrWhiteSpace(rightDiv) || !rightDiv.StartsWith("{") || !rightDiv.EndsWith("}") || rightDiv.Length < 5) {
            subResult.Result = false;
            subResult.Message = "Invalid Candidates (User) Description";
            CheckerResult.SubResults.Add(subResult);
            continue;
          }
          rightDiv = rightDiv.Substring(1, rightDiv.Length - 2);
          var parts = rightDiv.GetTrimmedNonEmptyParts(':');
          if (parts.Count < 2) {
            subResult.Result = false;
            subResult.Message = "Incomplete Candidates (User) Description";
            CheckerResult.SubResults.Add(subResult);
            continue;
          }
          if (Aibe.DH.UserRelatedDirectives.Any(x => x.EqualsIgnoreCase(parts[0]))) {//as of now, only one candidate is acceptable: All
            SyntaxCheckerResult directiveResult = new SyntaxCheckerResult { };
            directiveResult.DisplayText = "Directive";
            directiveResult.Result = true;
            directiveResult.Description = parts[0];
            directiveResult.Message = "Valid Directive (User)";
            subResult.SubResults.Add(directiveResult);
            UserInfoColumnFreeCandidates = parts[1].GetTrimmedNonEmptyParts(',');
            subResult.Result = true;
            foreach (var free in UserInfoColumnFreeCandidates) {
              SyntaxCheckerResult subSubResult = new SyntaxCheckerResult { };
              subSubResult.DisplayText = "Candidate";
              subSubResult.Result = true;
              subSubResult.Description = free;
              subSubResult.Message = "Valid Candidate (User)";
              subResult.SubResults.Add(subSubResult);
            }
            CheckerResult.SubResults.Add(subResult);
          } else {
            subResult.Result = false;
            subResult.Message = "Invalid Directive (User). Valid Directives: " + Aibe.DH.All;
            CheckerResult.SubResults.Add(subResult);
          }
        }
      }

      IsValid = true;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}