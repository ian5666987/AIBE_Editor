using Extension.Models;

namespace Aibe.Models.Tests {
  public class RegexCheckedColumnExampleInfoTest : RegexBaseInfoTest {
    public RegexCheckedColumnExampleInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Regex Checked Column Example";
      IsValid = !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Content);
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Column Name",
        Description = Name,
        Result = !string.IsNullOrWhiteSpace(Name),
      };
      subResult.Message = (subResult.Result ? "(Loosely-Checked) Valid" : "Invalid") + " Column Name";
      CheckerResult.SubResults.Add(subResult);
      subResult = new SyntaxCheckerResult {
        DisplayText = "Content",
        Description = Content,
        Result = !string.IsNullOrWhiteSpace(Content),
      };
      subResult.Message = (subResult.Result ? "(Loosely-Checked) Valid" : "Invalid") + " Content";
      CheckerResult.SubResults.Add(subResult);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}