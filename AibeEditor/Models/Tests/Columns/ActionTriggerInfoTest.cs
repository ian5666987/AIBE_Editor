using Extension.Models;
using Extension.String;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class ActionTriggerInfoTest : CommonBaseInfo {
    public string TriggerName { get; private set; }
    public List<string> RowActions { get; private set; } = new List<string>();
    public bool MustEditHaveChange { get; private set; } = true;
    public string TriggerConditionScript { get; private set; }
    public ActionTriggerInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Action Trigger";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      //The "Name" here is in the form of TriggerName3|RowAction31,RowAction32,…|MustEditHaveChange thus use further divider
      var parts = Name.GetTrimmedNonEmptyParts('|');
      if (parts == null || parts.Count <= 0 || string.IsNullOrWhiteSpace(parts[0])) { //at least the first part must have something
        CheckerResult.Message = "Valid Component(s) Not Found";
        IsValid = false;
        return;
      }

      Name = parts[0]; //just to avoid confusion and letting this having different things than TriggerName, make them the same here
      TriggerName = parts[0];

      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Name",
        Description = TriggerName,
        Result = true,
        Message = "(Loosely-Checked) Valid Trigger Name",
      };
      CheckerResult.SubResults.Add(subResult);

      CheckerResult.Result = true; //at this point onwards, this checker result would be true

      if (parts.Count > 1) {//has more items here                            
        RowActions = parts[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries) //RowAction31,RowAction32,…
          .Where(x => Aibe.DH.BaseTriggerRowActions.Any(y => y.EqualsIgnoreCaseTrim(x)))
          .Select(x => Aibe.DH.BaseTriggerRowActions.First(y => y.EqualsIgnoreCaseTrim(x)))
          .ToList();
        var actions = parts[1].Split(',');
        subResult = new SyntaxCheckerResult {
          DisplayText = "Row Actions",
          Description = parts[1],
          Result = true, //always true
        };
        CheckerResult.SubResults.Add(subResult);
        if (actions.Any()) {
          foreach (var action in actions) { //take from actions rather than Actions
            SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
              DisplayText = "Row Action",
              Description = action,
              Result = Aibe.DH.BaseTriggerRowActions.Any(y => y.EqualsIgnoreCaseTrim(action)),
            };
            subSubResult.Message = (subSubResult.Result ? "Valid" : "Invalid") + " Row Action For Action Trigger" +
              (subSubResult.Result ? "" : ". Valid Row Actions: " + string.Join(", ", Aibe.DH.BaseTriggerRowActions));
            subResult.SubResults.Add(subSubResult);
          }
        } else
          subResult.Message = "Actions: All Applied Row Actions";

        if (parts.Count > 2) { //has more items here
          bool mustEditHaveChangeTest;
          bool result = bool.TryParse(parts[2], out mustEditHaveChangeTest);
          if (result) //If exists and if the result is shown to be valid
            MustEditHaveChange = mustEditHaveChangeTest; //then takes it, whatever is the result

          subResult = new SyntaxCheckerResult {
            DisplayText = "Must Edit Have Change",
            Description = parts[2],
            Result = result,
          };
          subResult.Message = result ? "Valid Keyword" : "Invalid Keyword. Valid Keywords: " + string.Join(", ", Aibe.DH.TrueFalse);
          CheckerResult.SubResults.Add(subResult);
        }
      }

      if (!HasRightSide) {
        CheckerResult.Message = "Trigger Condition Does Not Exist. Procedure(s) With The Same [Trigger Name] Will ALWAYS Be Executed On Applied Row Action(s)";
        return;
      }

      TriggerConditionScript = RightSide;
      SyntaxCheckerResult triggerConditionResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Condition",
        Description = TriggerConditionScript,
        Result = !string.IsNullOrWhiteSpace(TriggerConditionScript),
      };
      triggerConditionResult.Message = triggerConditionResult.Result ? "(Loosely-Checked) Valid Trigger Condition" :
        "Trigger Condition Not Found";

      if (Extension.Checker.DB.ContainsUnenclosedDangerousElement(RightSide)) {
        triggerConditionResult.Message = "Trigger Script Contains Unenclosed Dangerous Element";
        IsValid = false;
      }

      //The last result to be added must be the trigger condition
      CheckerResult.SubResults.Add(triggerConditionResult);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}
