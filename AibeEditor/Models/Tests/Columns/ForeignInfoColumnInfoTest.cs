using Extension.Models;
using Extension.String;
using System;
using System.Collections.Generic;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class ForeignInfoColumnInfoTest : CommonBaseInfo {
    public string RefTableName { get; set; }
    public string ForeignKeyColumn { get; set; }
    public List<Tuple<string, string, string>> RefColumnNameTrios { get; set; } = new List<Tuple<string, string, string>>();
    public bool IsFullForeignInfo { get { return RefColumnNameTrios == null || RefColumnNameTrios.Count <= 0; } }
    private string tableSource { get; set; }

    public ForeignInfoColumnInfoTest(string desc, string tableSource) : base(desc) {
      this.tableSource = tableSource;
      applyDesc(desc);
    }

    public ForeignInfoColumnInfoTest(string desc) : base(desc) {
      applyDesc(desc);
    }

    private void applyDesc(string desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Foreign Info Column";
      if (!HasRightSide) {
        CheckerResult.Message = "Foreign Column Description Does Not Exist";
        IsValid = false;
        return;
      }
      var rightParts = RightSide.GetTrimmedNonEmptyParts(':');
      if (rightParts.Count < 2) { //there must be at least two right parts
        CheckerResult.Message = "Incomplete Foreign Column Description";
        IsValid = false;
        return;
      }
      IsValid = true;
      CheckerResult.Result = true;

      bool hasChecking = logic.UseDataDB && !string.IsNullOrWhiteSpace(tableSource);
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Column Name",
        Description = Name,
        Result = hasChecking ? logic.FindDataTableColumn(tableSource, Name) : true,
      };
      subResult.Message = subResult.Result ? hasChecking ? "Valid Column Name" :
        "(Loosely-Checked) Valid Column Name" : ("Column Name Not Found In [Data] Table [" + tableSource + "]");
      CheckerResult.SubResults.Add(subResult);

      hasChecking = logic.UseDataDB ? logic.FindDataTable(rightParts[0]) : true;
      subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Table Name",
        Description = rightParts[0],
        Result = hasChecking,
      };
      subResult.Message = subResult.Result ? hasChecking ? "Valid Table Source" :
        "(Loosely-Checked) Valid Table Source" : "Table Source Not Found In [Data] Table";
      CheckerResult.SubResults.Add(subResult);
      RefTableName = rightParts[0];

      hasChecking = logic.UseDataDB ? logic.FindDataTableColumn(rightParts[0], rightParts[1]) : true;
      subResult = new SyntaxCheckerResult {
        DisplayText = "Foreign Key Column Name",
        Description = rightParts[1],
        Result = hasChecking,
      };
      subResult.Message = subResult.Result ? hasChecking ? "Valid Column Name" :
        "(Loosely-Checked) Valid Column Name" : ("Column Name Not Found In [Data] Table [" + rightParts[0] + "]");
      CheckerResult.SubResults.Add(subResult);
      ForeignKeyColumn = rightParts[1];

      if (rightParts.Count < 3)
        return;

      var columnNames = rightParts[2].GetTrimmedNonEmptyParts(',');
      subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Column Name Descriptions",
        Description = rightParts[2],
        Result = columnNames != null && columnNames.Count > 0, //always true for this
      };
      subResult.Message = subResult.Result ? "Valid Reference Column Name Descriptions" :
        "Reference Column Name Descriptions Not Found";
      CheckerResult.SubResults.Add(subResult);

      foreach(var columnName in columnNames) {
        SyntaxCheckerResult columnResult = new SyntaxCheckerResult {
          DisplayText = "Reference Column Name Description",
          Description = columnName,
          Result = true, //always true
        };
        subResult.SubResults.Add(columnResult);
        SimpleExpression exp = new SimpleExpression(columnName, "|");
        if (!exp.IsValid) {
          columnResult.Result = false;
          columnResult.Message = "Invalid Reference Column Name Description";
          continue;
        }
        hasChecking = logic.UseDataDB ? logic.FindDataTableColumn(RefTableName, exp.LeftSide) : true;
        SyntaxCheckerResult subColumnResult = new SyntaxCheckerResult {
          DisplayText = "Reference Column Name",
          Description = exp.LeftSide,
          Result = hasChecking,
        };
        subColumnResult.Message = subColumnResult.Result ? hasChecking ? "Valid Column Name" :
          "(Loosely-Checked) Valid Column Name" : ("Column Name Not Found In [Foreign] [Data] Table [" + RefTableName + "]");
        columnResult.SubResults.Add(subColumnResult);
        if (exp.IsSingular) {
          RefColumnNameTrios.Add(new Tuple<string, string, string>(exp.LeftSide, exp.LeftSide.ToCamelBrokenString(), null));
          continue;
        }

        SimpleExpression rightExp = new SimpleExpression(exp.RightSide, "|");
        bool rightExpIsSingular = !rightExp.IsValid || rightExp.IsSingular;
        string displayName = rightExpIsSingular ? exp.RightSide : rightExp.LeftSide;

        subColumnResult = new SyntaxCheckerResult {
          DisplayText = "Reference Column Display Name",
          Description = displayName,
          Result = true,
          Message = "(Loosely-Checked) Valid Column Display Name",
        };
        columnResult.SubResults.Add(subColumnResult);
        if(rightExpIsSingular)
          RefColumnNameTrios.Add(new Tuple<string, string, string>(exp.LeftSide, displayName, null));
        else {
          RefColumnNameTrios.Add(new Tuple<string, string, string>(exp.LeftSide, displayName, rightExp.RightSide));
          hasChecking = logic.UseDataDB ? logic.FindDataTableColumn(tableSource, rightExp.RightSide) : true;
          subColumnResult = new SyntaxCheckerResult {
            DisplayText = "Assign-To Column Name",
            Description = rightExp.RightSide,
            Result = hasChecking,
          };
          subColumnResult.Message = subColumnResult.Result ? hasChecking ? "Valid Column Name" :
            "(Loosely-Checked) Valid Column Name" : ("Column Name Not Found In [Original] [Data] Table [" + tableSource + "]");
          columnResult.SubResults.Add(subColumnResult);
        }
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}