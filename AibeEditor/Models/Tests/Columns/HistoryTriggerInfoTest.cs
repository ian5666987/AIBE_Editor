using Extension.Models;
using Extension.Database.SqlServer;
using Extension.String;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class HistoryTriggerInfoTest : CommonBaseInfo { //likely to be unique, not common, but common do just fine when properly constructed
    public bool IsDataDeleted { get; private set; } = true;
    public bool MustEditHaveChange { get; private set; } = true;
    public List<string> RowActions { get; private set; } = new List<string>();
    public string TriggerConditionScript { get; private set; }
    public HistoryTriggerInfoTest(string desc) : base(desc) { //IsDataDeleted,MustEditHaveChange|RowActionN1,RowActionN2,…=TC-SQLS-N
      IsValid = false;
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "History Trigger";
      if (string.IsNullOrWhiteSpace(Name)) {
        CheckerResult.Message = "History Trigger Name Unavailable";
        return;
      }
      if (!HasRightSide) {
        CheckerResult.Message = "TC-SQLS Not Described";
        return;
      }
      string lowerName = Name.ToLower();
      bool isDataDeletedTrue = lowerName.StartsWith(Aibe.DH.True.ToLower());
      bool isDataDeletedFalse = lowerName.StartsWith(Aibe.DH.False.ToLower());
      if (!isDataDeletedTrue && !isDataDeletedFalse) {
        CheckerResult.Message = "IsDataDeleted Undefined/Has Invalid Definition (Neither True Nor False)";
        return;
      }
      if (Extension.Checker.DB.ContainsUnenclosedDangerousElement(RightSide)) {
        CheckerResult.Message = "TC-SQLS Contains Unenclosed Dangerous Element";
        return;
      }
      IsDataDeleted = Name.ToLower().StartsWith(Aibe.DH.True.ToLower());

      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Is Data Deleted",
        Result = true,
        Description = Name.Substring(0, isDataDeletedTrue ? 4 : 5),
        Message = "Valid Value",
      };
      CheckerResult.SubResults.Add(subResult);

      int index = Name.IndexOf('|');
      if (index >= 0) {
        SimpleExpression exp = new SimpleExpression(Name, "|");
        if (!exp.IsValid) {
          subResult = new SyntaxCheckerResult {
            DisplayText = "Expression",
            Result = false,
            Description = Name,
            Message = "[" + Name + "] Has Some Invalid Expression(s)",
          };
          CheckerResult.SubResults.Add(subResult);
          return;
        }
        int leftCommaIndex = exp.LeftSide.IndexOf(',');
        if (leftCommaIndex >= 0 && exp.LeftSide.Length > leftCommaIndex + 1) {
          string mustEditHaveChangeString = exp.LeftSide.Substring(leftCommaIndex + 1).Trim();
          subResult = new SyntaxCheckerResult {
            DisplayText = "Must Edit Have Change",
            Description = mustEditHaveChangeString,
          };
          if (mustEditHaveChangeString.EqualsIgnoreCase(Aibe.DH.True) || mustEditHaveChangeString.EqualsIgnoreCase(Aibe.DH.False)) {//validity check
            MustEditHaveChange = mustEditHaveChangeString.EqualsIgnoreCase(Aibe.DH.True);
            subResult.Message = "Valid Keyword";
            subResult.Result = true;
          } else {
            subResult.Message = "Invalid Keyword. Valid Keywords: " + string.Join(", ", Aibe.DH.TrueFalse);
          }
          CheckerResult.SubResults.Add(subResult);
        }
        if (!exp.IsSingular) {
          var rowActionsTest = exp.RightSide.GetTrimmedNonEmptyParts(',');
          RowActions = exp.RightSide.GetTrimmedNonEmptyParts(',')
            .Select(x => Aibe.DH.BaseTriggerRowActions.FirstOrDefault(y => y.EqualsIgnoreCase(x)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
          subResult = new SyntaxCheckerResult {
            DisplayText = "Row Actions",
            Description = exp.RightSide,
          };
          if (rowActionsTest.Any()) {
            subResult.Result = true;
            CheckerResult.SubResults.Add(subResult);
            foreach (var rowAction in rowActionsTest) {
              bool validRowAction = Aibe.DH.BaseTriggerRowActions.Any(y => y.EqualsIgnoreCase(rowAction));
              SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
                DisplayText = "Row Action",
                Description = rowAction,
                Result = validRowAction,
                Message = validRowAction ? "Valid Row Action [" + rowAction + "]" :
                  "Invalid Row Action [" + rowAction + "]. Valid Row Actions: " + string.Join(", ", Aibe.DH.BaseTriggerRowActions)
              };
              subResult.SubResults.Add(subSubResult);              
            }
          } else {
            subResult.Message = "Invalid Description";
            CheckerResult.SubResults.Add(subResult);
          }
        }
      } else { //it is still possible to have MustEditHaveChanged here
        SimpleExpression exp = new SimpleExpression(Name, ",");
        if (exp.IsValid && !exp.IsSingular) {//nothing is in the right of the name
          subResult = new SyntaxCheckerResult {
            DisplayText = "Must Edit Have Change",
            Description = exp.RightSide,
          };
          if (exp.RightSide.EqualsIgnoreCase(Aibe.DH.True) || exp.RightSide.EqualsIgnoreCase(Aibe.DH.False)) {
            MustEditHaveChange = exp.RightSide.EqualsIgnoreCase(Aibe.DH.True);
            subResult.Message = "Valid Keyword";
            subResult.Result = true;
          } else {
            subResult.Message = "Invalid Keyword. Valid Keywords: " + string.Join(", ", Aibe.DH.TrueFalse);
          }
          CheckerResult.SubResults.Add(subResult);
        }
      }

      Name = string.Empty; //emptified the Name to avoid confusion outside...
      TriggerConditionScript = RightSide;
      subResult = new SyntaxCheckerResult {
        DisplayText = "TC-SQLS",
        Description = RightSide,
        Result = true,
        Message = "Checked TC-SQLS",
      };
      CheckerResult.SubResults.Add(subResult);
      CheckerResult.Result = true;
      IsValid = true; //as long as it does not contain unenclosed dangerous element, it is OK to have
    }

    public DataTable GetTriggeredDataTable(string tableSource, string rowAction, int cid, DataRow originalRow) {
      if (RowActions != null && RowActions.Count > 0 && !RowActions.Any(x => x.EqualsIgnoreCase(rowAction)))
        return null; //if there is something in row action, and nont of them is not what is allowed to trigger, return null
      string usedTriggerCondition = string.Concat(Aibe.DH.Cid, "=", cid, string.IsNullOrWhiteSpace(TriggerConditionScript) ? string.Empty :
        string.Concat(" AND (", TriggerConditionScript, ")"));
      DataTable table = SQLServerHandler.GetFullDataTableWhere(Aibe.DH.DataDBConnectionString, tableSource, usedTriggerCondition);
      if (table == null || table.Rows == null || table.Rows.Count <= 0)
        return null;
      if (originalRow != null && MustEditHaveChange) { //only if edit must have changed is true we could check this. Otherwise, skip checking.
        bool isIdentical = Extension.Checker.DB.DataRowEquals(originalRow, table.Rows[0]);
        if (isIdentical)
          return null; //do not trigger data table when everything is identical
      }
      return table;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}