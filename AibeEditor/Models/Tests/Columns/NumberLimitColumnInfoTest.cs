using Extension.Models;
using Extension.String;
using System.Collections.Generic;

namespace Aibe.Models.Tests {
  public class NumberLimitColumnInfoTest : CommonBaseInfo {
    public double Min { get; private set; } = double.MinValue; //preset as extreme values
    public double Max { get; private set; } = double.MaxValue;
    public NumberLimitColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Number Limit Column Info";
      if (!HasRightSide) //TODO, remember to change the number limits parser to use "|" in the docs and also in the code. Done in the code, in the docs remain unchanged yet (2017-09-12)
        return;

      List<string> parts = RightSide.GetTrimmedNonEmptyParts('|');
      foreach (string part in parts) { //parts can be one can be two
        SimpleExpression exp = new SimpleExpression(part, ":");
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Expression",
          Description = part,
        };
        if (!exp.IsValid || exp.IsSingular) {
          subResult.Result = false;
          subResult.Message = "Invalid Expression";
          CheckerResult.SubResults.Add(subResult);
          continue;
        }
        double value;
        bool result;
        if (exp.LeftSide.EqualsIgnoreCaseTrim(Aibe.DH.Min)) {
          result = double.TryParse(exp.RightSide, out value);
          if (result) { //if the parse is successful, then use it as number limit
            Min = value;
            subResult.Result = true;
            subResult.Message = "Valid Range Keyword [" + exp.LeftSide + "], Valid Value [" + exp.RightSide + "]";
            CheckerResult.SubResults.Add(subResult);
          } else {
            subResult.Result = false;
            subResult.Message = "Valid Range Keyword [" + exp.LeftSide + "], Invalid Value [" + exp.RightSide + "]";
            CheckerResult.SubResults.Add(subResult);
          }
        } else if (exp.LeftSide.EqualsIgnoreCaseTrim(Aibe.DH.Max)) {
          result = double.TryParse(exp.RightSide, out value);
          if (result) { //if the parse is successful, then use it as number limit
            Max = value;
            subResult.Result = true;
            subResult.Message = "Valid Range Keyword [" + exp.LeftSide + "], Valid Value [" + exp.RightSide + "]";
            CheckerResult.SubResults.Add(subResult);
          } else {
            subResult.Result = false;
            subResult.Message = "Valid Range Keyword [" + exp.LeftSide + "], Invalid Value [" + exp.RightSide + "]";
            CheckerResult.SubResults.Add(subResult);
          }
        } else {
          subResult.Result = false;
          subResult.Message = "Invalid Range Keyword [" + exp.LeftSide + "]. Valid Range Keywords: " + string.Join(", ", Aibe.DH.MinMax);
          CheckerResult.SubResults.Add(subResult);
        }
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}