using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class AttachmentInfoTest : CommonBaseInfo {
    public List<string> Formats { get; private set; } = new List<string>();
    public AttachmentInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Attachment";
      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Empty Text";
        return;
      }
      CheckerResult.Result = true;
      SyntaxCheckerResult nameResult = new SyntaxCheckerResult {
        DisplayText = "Column Name",
        Description = Name,
        Result = true,
      };
      CheckerResult.SubResults.Add(nameResult);
      if (HasRightSide) {
        Formats = RightSide.GetTrimmedNonEmptyParts(',')
          .Select(x => string.Concat(".", x)).ToList();
        var items = RightSide.GetTrimmedNonEmptyParts(',');
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Formats",
          Description = RightSide,
          Result = true,
        };
        CheckerResult.SubResults.Add(subResult);
        foreach(var item in items) {
          SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
            DisplayText = "Format",
            Description = item,
            Result = true,
            Message = "(Loosely-Checked) Valid Format",
          };
          subResult.SubResults.Add(subSubResult);
        }
      } else {
        CheckerResult.Message = "Valid Column [" + Name + "]. Formats: All";
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}