using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class TimeStampColumnInfoTest : CommonBaseInfo {
    private static List<string> defaultRowActionsApplied = new List<string> { Aibe.DH.CreateActionName, Aibe.DH.EditActionName };
    public List<TimeStampColumnRowActionInfoTest> RowActionsApplied { get; private set; } = new List<TimeStampColumnRowActionInfoTest>();
    public TimeStampColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Timestamp Column";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      CheckerResult.Message = "Valid Syntax";
      CheckerResult.Result = true;
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Timestamp Description",
        Description = RightSide,
      };
      if (HasRightSide) { //take whatever is in the right side
        RowActionsApplied = RightSide.GetTrimmedNonEmptyParts(',')
          .Select(x => new TimeStampColumnRowActionInfoTest(x))
          .Where(x => x.IsValid)
          .ToList();
        CheckerResult.SubResults.Add(subResult);
        if (RowActionsApplied == null || RowActionsApplied.Count <= 0) {
          subResult.Message = "Incorrect Description. Default [NOW] Timestamps Are Applied For [" + string.Join(", ", defaultRowActionsApplied) + "] Row Actions";
        } else {
          var testApplied = RightSide.GetTrimmedNonEmptyParts(',')
            .Select(x => new TimeStampColumnRowActionInfoTest(x))
            .ToList();
          subResult.Result = true;
          CheckerResult.SubResults.AddRange(testApplied.Select(x => x.CheckerResult));
        }
      } else {
        subResult.Message = "No Description. Default [NOW] Timestamps Are Applied For [" + string.Join(", ", defaultRowActionsApplied) + "] Row Actions";
        subResult.Result = true;
        CheckerResult.SubResults.Add(subResult);
      }

      if (RowActionsApplied == null || RowActionsApplied.Count <= 0) { //valid but no row actions, means we apply to both
        RowActionsApplied = defaultRowActionsApplied
          .Select(x => new TimeStampColumnRowActionInfoTest(x))
          .ToList();
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}