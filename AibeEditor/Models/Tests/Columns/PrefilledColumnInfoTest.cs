using Extension.Models;
using Extension.String;

namespace Aibe.Models.Tests {
  public class PrefilledColumnInfoTest : CommonBaseInfo {
    public string Value { get; private set; }
    public PrefilledColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Prefilled Column";
      if (!HasRightSide) {
        CheckerResult.Message = "Incomplete Description";
        IsValid = false;
        return;
      }
      CheckerResult.Message = "(Loosely-Checked) Valid Description";
      CheckerResult.Result = true;
      Value = RightSide.ExtractSqlValue();
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "SQL Value",
        Description = Value,
        Result = true,
        Message = "(Loosely-Checked) Value SQL Value",
      };
      CheckerResult.SubResults.Add(subResult);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}