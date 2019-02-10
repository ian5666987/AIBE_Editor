using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System.Linq;

namespace Aibe.Models.Tests {
  public class OrderByInfoTest : CommonBaseInfo {
    public string OrderDirection { get; private set; }
    public string Script { get; private set; }
    public bool IsScript { get { return !string.IsNullOrWhiteSpace(Script) && IsValid; } }
    public OrderByInfoTest(string desc, bool scripted) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.Result = IsValid;
      CheckerResult.DisplayText = "Order By Item";
      if (scripted) { //special orderBy, this must be declared outside!
        CheckerResult.DisplayText += " (Scripted)";
        string testScript = OriginalDesc.Substring(Aibe.DH.SQLScriptDirectivePrefix.Length).Trim(); //thus, this is assumed to be always working
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Script",
          Description = testScript,
        };
        CheckerResult.SubResults.Add(subResult);
        bool result = Extension.Checker.DB.ContainsUnenclosedDangerousElement(testScript);
        IsValid = !result;
        if (result) {
          subResult.Message = "Script Contains Unenclosed Dangerous Element(s)";
          return;
        }
        subResult.Result = true;
        subResult.Message = "Valid Script";
        CheckerResult.Result = true;
        Script = testScript;
      } else if (HasRightSide) {//legacy way of getting the info
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Column Name",
          Description = Name,
          Result = IsValid,
        };
        subResult.Message = "(Loosely-Checked) " + (subResult.Result ? "Valid" : "Invalid") + " Column Name";
        CheckerResult.SubResults.Add(subResult);
        subResult = new SyntaxCheckerResult {
          DisplayText = "Order By Direction",
          Description = RightSide,
        };
        CheckerResult.SubResults.Add(subResult);
        if (Aibe.DH.ValidOrderDirections.Any(x => x.EqualsIgnoreCaseTrim(RightSide))) {
          OrderDirection = Aibe.DH.ValidOrderDirections.FirstOrDefault(x => x.EqualsIgnoreCaseTrim(RightSide));
          subResult.Message = "Valid Order By Direction";
          subResult.Result = true;
        } else {
          subResult.Message = "Invalid Order By Direction. Valid Directions: " + string.Join(", ", Aibe.DH.ValidOrderDirections);
          subResult.Result = false;
        }
        CheckerResult.Result = CheckerResult.GetDirectSubResults();
      }
    }

    public string GetOrderDirection () { return string.IsNullOrWhiteSpace(OrderDirection) ? string.Empty : OrderDirection; }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}