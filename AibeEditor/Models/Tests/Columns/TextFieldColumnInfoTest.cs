using Extension.Models;

namespace Aibe.Models.Tests {
  public class TextFieldColumnInfoTest : CommonBaseInfo {
    public const int DefaultRowSize = 4;
    public int RowSize { get; private set; } = DefaultRowSize;
    public TextFieldColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Textfield Column";
      CheckerResult.Result = IsValid;
      CheckerResult.Message = "(Loosely-Checked) Valid Column";
      if (HasRightSide) {
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          Description = RightSide,
          DisplayText = "Row"
        };
        CheckerResult.SubResults.Add(subResult);
        int value;
        bool result = int.TryParse(RightSide, out value);
        if (result && value > 0) {//if the parsing is successful and the value is positive, only then we can take it
          RowSize = value;
          subResult.Message = "Valid Row Size Description";
          subResult.Result = true;
        } else {
          subResult.Message = "Invalid Row Size Description";
        }
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}