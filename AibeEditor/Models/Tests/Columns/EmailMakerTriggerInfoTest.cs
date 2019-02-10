using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Aibe.Models.Tests {
  public class EmailMakerTriggerInfoTest : CommonBaseInfo {
    public List<string> RowActions { get; private set; } = new List<string>();
    public string TriggerConditionScript { get; private set; }
    public EmailMakerTriggerInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Email Maker Trigger";
      IsValid = false;
      if (!HasRightSide) {
        CheckerResult.Message = "Trigger Script Not Found";
        return;
      }
      SyntaxCheckerResult nameResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Name",
        Description = Name,
        Result = true,
        Message = "Valid Trigger Name",
      };
      CheckerResult.SubResults.Add(nameResult);

      SyntaxCheckerResult triggerResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Script",
        Description = RightSide,
      };

      if (Extension.Checker.DB.ContainsUnenclosedDangerousElement(RightSide)) {
        triggerResult.Message = "Trigger Script Contains Unenclosed Dangerous Element";
        CheckerResult.SubResults.Add(triggerResult);
        return;
      }
      int index = Name.IndexOf('|');
      if (index >= 0) {
        SimpleExpression exp = new SimpleExpression(Name, "|");
        if (!exp.IsValid) {
          nameResult.Result = false;
          nameResult.Message = "Fail To Parse";
          CheckerResult.SubResults.Add(triggerResult);
          return;
        }
        Name = exp.LeftSide;
        nameResult.Description = Name;
        if (!exp.IsSingular) {
          RowActions = exp.RightSide.GetTrimmedNonEmptyParts(',')
            .Select(x => Aibe.DH.BaseTriggerRowActions.FirstOrDefault(y => y.EqualsIgnoreCase(x)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
          var items = exp.RightSide.GetTrimmedNonEmptyParts(',');
          SyntaxCheckerResult actionsResult = new SyntaxCheckerResult {
            DisplayText = "Row Actions",
            Description = exp.RightSide,
            Result = true,
          };
          CheckerResult.SubResults.Add(actionsResult);
          foreach(var item in items) {
            SyntaxCheckerResult actionResult = new SyntaxCheckerResult {
              DisplayText = "Row Action",
              Description = item,
            };
            actionResult.Result = Aibe.DH.BaseTriggerRowActions.Any(x => x.EqualsIgnoreCase(item));
            actionResult.Message = actionResult.Result ? "Valid Row Action [" + item + "]" : 
              "Invalid Row Action [" + item + "]. Valid Row Actions: " + string.Join(", ", Aibe.DH.BaseTriggerRowActions);
            actionsResult.SubResults.Add(actionResult);
          }
        }
      }
      TriggerConditionScript = RightSide;
      triggerResult.Result = true;
      triggerResult.Message = "Valid Trigger Script";
      if (!RowActions.Any())
        triggerResult.Message += ", Roles: All";
      CheckerResult.SubResults.Add(triggerResult);
      CheckerResult.Result = true;
      IsValid = true;
    }

    public DataTable GetTriggeredDataTable(string tableSource, string rowAction, int cid, DataRow originalRow) {
      if (RowActions != null && RowActions.Count > 0 && !RowActions.Any(x => x.EqualsIgnoreCase(rowAction)))
        return null; //if there is something in row action, and nont of them is not what is allowed to trigger, return null
      string usedTriggerCondition = string.Concat(Aibe.DH.Cid, "=", cid, string.IsNullOrWhiteSpace(TriggerConditionScript) ? string.Empty :
        string.Concat(" AND (", TriggerConditionScript, ")"));
      DataTable table = SQLServerHandler.GetFullDataTableWhere(Aibe.DH.DataDBConnectionString, tableSource, usedTriggerCondition);
      if (table == null || table.Rows == null || table.Rows.Count <= 0)
        return null;
      if (originalRow != null) {
        bool isIdentical = Extension.Checker.DB.DataRowEquals(originalRow, table.Rows[0]);
        if (isIdentical)
          return null; //do not trigger data table when everything is identical
      }
      return table;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}