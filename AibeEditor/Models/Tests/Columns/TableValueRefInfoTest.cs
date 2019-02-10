using Extension.Database.SqlServer;
using Extension.String;
using Extension.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class TableValueRefInfoTest : BaseInfo {
    private static List<string> defaultComparators = new List<string> { "=" }; //as of now, only accepts the "=" comparator for taking reference from other tables
    private static List<string> nonAllowedStrings = new List<string> { ",", ";", ":", "--" }; //by right all but double dashes "--" should have been handled outside, but just in case
    private static string skip = Aibe.DH.Skip; //keyword to skip checking the RefAnotherColumnName=ThisOtherColumnName part
    public string RefTableName { get; private set; } 
    public string Column { get; private set; }
    public string CondColumn { get; private set; }
    public string CondComparator { get; private set; } //is sign, like =, now the only one accepted
    public string CrossTableCondColumn { get; private set; }
    public string StaticCrossTableCondColumn { get; private set; }
    public bool CrossTableColumnIsStatic { get { return !string.IsNullOrWhiteSpace(StaticCrossTableCondColumn); } }
    public string AdditionalWhereClause { get; private set; }
    public bool CrossTableCheckIsSkipped { get; private set; }
    public bool IsSelectUnconditional { get; private set; } = true; //assume true till proven otherwise
    //desc = RefTableName:Column:CondColumn {CondComparator} CrossTableCondColumn OR StaticCrossTableCondColumn
    public TableValueRefInfoTest(string desc) : base (desc) {
      CheckerResult.DisplayText = "Table Value Reference";
      CheckerResult.Description = desc;
      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Empty Text";
        return;
      }
      var parts = desc.GetTrimmedParts(':'); //This is special, it can be not filled to work!
      if (parts.Count < 2) {//minimum contains of two parts
        CheckerResult.Message = "Insufficient Number Of Components";
        return;
      }
      RefTableName = parts[0];
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Table Name",
        Description = parts[0],
        Result = logic.UseDataDB ? logic.FindDataTable(RefTableName) : true,
      };
      subResult.Message = subResult.Result ? "Valid Table Name" : "Table Name Not Found In Data DB";
      CheckerResult.SubResults.Add(subResult);
      //int sharpIndex = parts[1].IndexOf('#');
      //if (sharpIndex > 0 && parts[1].Length > sharpIndex + 1) {
      //  string columnCandidate = parts[1].Substring(0, sharpIndex).Trim();
      //  string valueColumnCandidate = parts[1].Substring(sharpIndex + 1).Trim();
      //  if (string.IsNullOrWhiteSpace(columnCandidate) || string.IsNullOrWhiteSpace(valueColumnCandidate))
      //    return; //invalid
      //  Column = columnCandidate;
      //  ValueColumn = valueColumnCandidate;
      //} else
      Column = parts[1]; //Take the column directly
      subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Table Column",
        Description = parts[1],
        Result = logic.UseDataDB ? logic.FindDataTableColumn(RefTableName, Column) : true,
      };
      subResult.Message = subResult.Result ? "Valid Column Name" : "Column Name Not Found In Table [" + RefTableName + "]";
      CheckerResult.SubResults.Add(subResult);

      IsValid = true;
      CheckerResult.Result = true;

      if (parts.Count < 3)
        return;

      subResult = new SyntaxCheckerResult {
        DisplayText = "Conditional Where Part",
        Description = parts[2],
      };
      CheckerResult.SubResults.Add(subResult);
      if (parts[2].EqualsIgnoreCaseTrim(skip) || string.IsNullOrWhiteSpace(parts[2])) {
        CrossTableCheckIsSkipped = true;
        subResult.Result = true;
        subResult.Message = "Skipped";
      } else { //Only if cross table check is not skipped then we could check for the table reference validity, else, go directly to AdditionalWhereClause
        SimpleExpression exp = new SimpleExpression(parts[2], defaultComparators); //TODO as of now, only split by equality. Subsequently could be different.
        if (!exp.IsValid || exp.IsSingular) { //if it contains false expression, it cannot be singular too.
          IsValid = false; //revoke the validity
          subResult.Message = exp.IsSingular ? "Conditional Comparison Part Is Missing" : 
            "Invalid Conditional Expression";
          CheckerResult.Result = false;
          return;
        }
        subResult.Result = true;
        subResult.Message = "Valid Conditional Expression";
        CondColumn = exp.LeftSide;
        SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
          DisplayText = "Compared Column",
          Description = exp.LeftSide,
          Result = logic.UseDataDB ? logic.FindDataTableColumn(RefTableName, CondColumn) : true,
        };
        subSubResult.Message = subSubResult.Result ? "Valid Column Name" : "Column Name Not Found In Table [" + RefTableName + "]";
        subResult.SubResults.Add(subSubResult);

        CondComparator = exp.MiddleSign;
        subSubResult = new SyntaxCheckerResult {
          DisplayText = "Comparator",
          Description = exp.MiddleSign,
          Result = true,
          Message = "Valid Comparator",
        };
        subResult.SubResults.Add(subSubResult);

        string testRightSide = exp.RightSide.GetNonEmptyTrimmedInBetween("\"", "\"");
        subSubResult = new SyntaxCheckerResult {
          DisplayText = "Compared Value",
          Description = exp.RightSide,
          Result = true,
        };
        subResult.SubResults.Add(subSubResult);
        if (!string.IsNullOrWhiteSpace(testRightSide)) {//check if it is static
          StaticCrossTableCondColumn = testRightSide;
          subSubResult.Message = "Static Value: " + testRightSide;
        } else { //then it must be dynamic
          CrossTableCondColumn = exp.RightSide;
          subSubResult.Message = "Dynamic (Column) Value: " + exp.RightSide;
        }
      }
      IsSelectUnconditional = false; //this means that all things are taken

      if (parts.Count != 4) //parts cannot be more than 4
        return;

      //At this point, there is additional where clause, but it will not change the validity if wrong. It will simply be unused.
      subResult = new SyntaxCheckerResult {
        DisplayText = "Additional Where Clause",
        Description = parts[3],
      };
      CheckerResult.SubResults.Add(subResult);
      if (nonAllowedStrings.Any(x => parts[3].IndexOf(x) != -1)) {
        CheckerResult.Message = "Additional Where Clause Contains Non-Allowed String";
        return; //cannot continue if any of such things exist
      }

      subResult.Result = true;
      subResult.Message = "Valid Additional Where Clause";
      AdditionalWhereClause = parts[3];
    }

    public List<string> GetStaticDropDownItems() {
      List<string> items = new List<string>();
      //Script initialization
      StringBuilder selectScript = new StringBuilder(string.Concat(
        "SELECT DISTINCT [", Column, "] FROM [", RefTableName, "] WHERE [",
          Column, "] IS NOT NULL"));
      SqlParameter par = null;
      if (!IsSelectUnconditional && CrossTableColumnIsStatic) { //if the select is conditional, adds the condition static condition
        string addQueryString = //if the select is conditional, only static is allowed here.
          string.Concat(" AND [", CondColumn, "]", CondComparator, "@par0");
        par = new SqlParameter("@par0", StaticCrossTableCondColumn);
        selectScript.Append(addQueryString);
      }
      if (!string.IsNullOrWhiteSpace(AdditionalWhereClause)) {
        selectScript.Append(" AND (");
        selectScript.Append(AdditionalWhereClause);
        selectScript.Append(")");
      }
      DataTable dataTable = par == null ? SQLServerHandler.GetDataTable(Aibe.DH.DataDBConnectionString, selectScript.ToString()) :
        SQLServerHandler.GetDataTable(Aibe.DH.DataDBConnectionString, selectScript.ToString(), par);
      if (dataTable == null)
        return items;
      foreach (DataRow row in dataTable.Rows)
        items.Add(row.ItemArray[0].ToString());
      return items;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}