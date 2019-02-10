using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class TimeStampColumnRowActionInfoTest : BaseInfo {
    private static List<string> validRowActionsApplied = new List<string> { Aibe.DH.CreateActionName, Aibe.DH.EditActionName };
    private static List<string> validTimeShiftValues = new List<string> { "+", "-" };
    public string Name { get; private set; }
    public string Operator { get; private set; }
    public double ShiftValue { get; private set; }
    public bool IsFixed { get; private set; } = true; //by default, IsFixed is chosen
    public TimeStampColumnRowActionInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Timestamp Row Action Description";
      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Empty Text";
        return;
      }
      var descParts = desc.GetTrimmedNonEmptyParts('|');
      if (descParts == null || descParts.Count <= 0) {
        CheckerResult.Message = "Component(s) Not Found";
        return;
      }

      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Row Action Name",
        Description = descParts[0],
      };
      CheckerResult.SubResults.Add(subResult);
      if (!validRowActionsApplied.Any(x => x.EqualsIgnoreCaseTrim(descParts[0]))) { //the action name must be valid
        subResult.Message = "Invalid Row Action Name Applied. Valid Row Action Names: " + 
          string.Join(", ", validRowActionsApplied);
        return;
      } else {
        subResult.Result = true;
        subResult.Message = "Valid Row Action Name Applied";
      }

      IsValid = true;
      CheckerResult.Result = true;
      Name = descParts[0];

      if (descParts.Count < 2) 
        return;

      subResult = new SyntaxCheckerResult {
        DisplayText = "Timestamp Value",
        Description = descParts[1],
      };
      CheckerResult.SubResults.Add(subResult);

      if (descParts[1].Length < 2) { //the length of the descParts must be at least two to create things like +4 or -5)
        subResult.Message = "Description Too Short";
        return;
      }

      string valuePart = descParts[1].Substring(1).Trim();

      if (string.IsNullOrWhiteSpace(valuePart) || !validTimeShiftValues.Any(x => descParts[1].StartsWith(x))) {//invalid time described
        subResult.Message = "Invalid/Empty Value In The Description";
        return;
      }

      double testValue;
      bool result = double.TryParse(valuePart, out testValue);
      if (result && testValue > 0) {
        Operator = validTimeShiftValues.FirstOrDefault(x => descParts[1].StartsWith(x));
        ShiftValue = testValue;
        SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
          DisplayText = "Operator",
          Description = Operator,
          Message = "Valid Operator",
          Result = true,
        };
        subResult.SubResults.Add(subSubResult);
        subSubResult = new SyntaxCheckerResult {
          DisplayText = "Shift Value",
          Description = ShiftValue.ToString(),
          Message = "Valid Shift Value",
          Result = true,
        };
        subResult.SubResults.Add(subSubResult);
        subResult.Result = true;
      } else {
        subResult.Message = "Invalid Description Value";
      }

      if (descParts.Count < 3)
        return;

      subResult = new SyntaxCheckerResult {
        DisplayText = "Is Fixed (Stamped Only Once)",
        Description = descParts[2],
      };
      CheckerResult.SubResults.Add(subResult);
      //only become false if it is asked to be false, otherwise remains true
      IsFixed = !descParts[2].EqualsIgnoreCaseTrim(false.ToString());
      subResult.Result = descParts[2].EqualsIgnoreCaseTrim(false.ToString()) ||
        descParts[2].EqualsIgnoreCaseTrim(true.ToString());
      subResult.Message = subResult.Result ? "Valid Description [" + descParts[2] + "]" : 
        "Invalid Description [" + descParts[2] + "]. Default Value [" + Aibe.DH.True.ToString() + "] Shall Be Applied";
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}