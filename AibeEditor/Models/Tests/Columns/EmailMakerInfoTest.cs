using Extension.Models;
using System.Collections.Generic;

namespace Aibe.Models.Tests {
  public class EmailMakerInfoTest : CommonBaseInfo {
    public string TemplateName { get; private set; }
    public EmailMakerInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Email Maker";
      if (!HasRightSide) {
        CheckerResult.Message = "Template Name Not Found";
        IsValid = false;
        return;
      }
      CheckerResult.Result = true;

      SyntaxCheckerResult nameResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Name",
        Description = Name,
        Result = true,
        Message = "Valid Trigger Name",
      };
      CheckerResult.SubResults.Add(nameResult);

      SyntaxCheckerResult tempResult = new SyntaxCheckerResult {
        DisplayText = "Template Name",
        Description = RightSide,
        Result = true,
        Message = "(Loosely-Checked) Valid Template Name",
      };
      CheckerResult.SubResults.Add(tempResult);

      TemplateName = RightSide;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}