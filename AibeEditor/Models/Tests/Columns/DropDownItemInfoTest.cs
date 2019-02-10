using Extension.Models;
using Extension.String;

namespace Aibe.Models.Tests {
  public class DropDownItemInfoTest : BaseInfo {
    public bool IsItem { get { return !string.IsNullOrWhiteSpace(Item); } } //either item or table reference
    public string Item { get; private set; } //to store item
    public TableValueRefInfoTest RefInfo { get; private set; } //to store table value reference
    public DropDownItemInfoTest(string desc) : base(desc) {
      CheckerResult.DisplayText = "Dropdown Item";
      CheckerResult.Description = desc;
      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Empty Text";
        return;
      }
      string descContent = desc.GetNonEmptyTrimmedInBetween("[", "]");
      if (string.IsNullOrWhiteSpace(descContent)) { //if not found, then considers this an item, immediately returns it
        Item = desc;
        IsValid = true;
        CheckerResult.Result = true;
        CheckerResult.Message = "Valid Item";
        return;
      }

      //Otherwise it must be table value reference
      TableValueRefInfoTest testRefInfo = new TableValueRefInfoTest(descContent);
      CheckerResult.SubResults.Add(testRefInfo.CheckerResult);
      if (!testRefInfo.IsValid) {
        CheckerResult.Message = "Invalid Table Value Reference";
        return;
      }

      RefInfo = testRefInfo;
      IsValid = true;
      CheckerResult.Result = true;
      CheckerResult.Message = "Valid Table Value Reference";
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}