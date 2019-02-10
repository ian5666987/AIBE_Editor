using Extension.Models;

namespace Aibe.Models.Tests {
  public class AffixColumnInfoTest : CommonBaseInfo {
    public string AffixValue { get; private set; }
    public AffixColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Affix Column Info";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      if (!HasRightSide) {
        IsValid = false;
        CheckerResult.Message = "Value Not Found";
        return;
      }

      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Affix Value",
        Description = RightSide,
        Result = true,
        Message = "Valid Affix Value",
      };
      CheckerResult.SubResults.Add(subResult);
      CheckerResult.Result = true;
      AffixValue = RightSide;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}