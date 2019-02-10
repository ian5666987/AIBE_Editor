using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class CustomDateTimeFormatInfoTest : CommonBaseInfo {
    public Dictionary<string, string> DtFormatDictionary { get; private set; } = new Dictionary<string, string>();
    public CustomDateTimeFormatInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Custom Date-Time Format";
      IsValid = false;
      if (!HasRightSide) {
        CheckerResult.Message = "Incomplete Description";
        return;
      }
      var rightParts = RightSide.GetTrimmedNonEmptyParts('|');
      if (rightParts == null) {
        CheckerResult.Message = "Description Not Found";
        return;
      }
      if (rightParts.Count < 2) {
        CheckerResult.Message = "Incomplete Description Found";
        return;
      }
      for (int i = 0; i < rightParts.Count; i += 2) {
        string pageName = rightParts[i];
        SyntaxCheckerResult pageResult = new SyntaxCheckerResult {
          DisplayText = "Page Name",
          Description = pageName,
          Result = Aibe.DH.AcceptablePageNames.Any(x => x.EqualsIgnoreCase(rightParts[i])),          
        };
        pageResult.Message = pageResult.Result ? "Valid Page Name" : 
          "Unacceptable Page Name. Acceptable Page Names: " + string.Join(", ", Aibe.DH.AcceptablePageNames.ToArray());
        CheckerResult.SubResults.Add(pageResult);
        SyntaxCheckerResult dtResult = new SyntaxCheckerResult {
          DisplayText = "Date-Time Format",
        };
        CheckerResult.SubResults.Add(dtResult);
        if (i + 1 >= rightParts.Count) {
          dtResult.Message = "Date-Time Format Unavailable";
          break;
        }
        string dtFormat = rightParts[i + 1];
        dtResult.Description = dtFormat;
        if (string.IsNullOrWhiteSpace(rightParts[i + 1])) {
          dtResult.Message = "Empty Date-Time Format";
          continue;
        }
        dtResult.Result = true;
        dtResult.Message = "Valid Format";
        if (!Aibe.DH.AcceptablePageNames.Any(x => x.EqualsIgnoreCase(rightParts[i])))
          continue;
        DtFormatDictionary.Add(Aibe.DH.AcceptablePageNames
          .FirstOrDefault(x => x.EqualsIgnoreCase(rightParts[i])), rightParts[i + 1]);
      }
      IsValid = DtFormatDictionary.Any();
      CheckerResult.Result = IsValid;
    }

    public bool IsAppliedFor(string pageName) {
      return DtFormatDictionary.Any(x => x.Key.EqualsIgnoreCase(pageName));
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}