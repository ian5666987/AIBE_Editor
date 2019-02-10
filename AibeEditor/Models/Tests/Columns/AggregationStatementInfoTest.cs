using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class AggregationStatementInfoTest : CommonBaseInfo {
    public List<KeyValuePair<string, string>> GroupByColumns { get; private set; } = new List<KeyValuePair<string, string>>();
    public string AggregationQueryScript { get; private set; }
    private string tableSource { get; set; }
    public AggregationStatementInfoTest(string desc, string tableSource) : base(desc){
      this.tableSource = tableSource;
      applyDesc(desc);
    }

    public AggregationStatementInfoTest(string desc) : base(desc) {
      applyDesc(desc);
    }

    private void applyDesc(string desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Aggregation Statement";
      if (!HasRightSide) { //must have the right side to be valid
        CheckerResult.Message = "Aggregation Statement Does Not Exist";
        IsValid = false;
        return;
      }

      AggregationQueryScript = RightSide;
      SyntaxCheckerResult aggQueryResult = new SyntaxCheckerResult {
        DisplayText = "Aggregation Query Script",
        Description = AggregationQueryScript,
        Result = !string.IsNullOrWhiteSpace(AggregationQueryScript),
      };
      aggQueryResult.Message = aggQueryResult.Result ? "(Loosely-Checked) Valid Aggregation Query Script" :
        "Aggregation Query Script Not Found";

      //The "Name" here is in the form of GroupByColumn31:AutoDirective31,GroupByColumn32:AutoDirective32,... thus use further divider
      var parts = Name.GetTrimmedNonEmptyParts(',');
      if (parts == null || parts.Count <= 0 || string.IsNullOrWhiteSpace(parts[0])) { //at least the first part must have something
        CheckerResult.Message = "Group By Columns Not Found";
        CheckerResult.SubResults.Add(aggQueryResult);
        IsValid = false;
        return;
      }

      //List of GroupByColumns
      if (parts.Any()) {
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Group By Columns",
          Description = Name,
          Result = true, //always true
        };
        CheckerResult.SubResults.Add(subResult);
        for (int i = 0; i < parts.Count; ++i) {
          SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
            DisplayText = "Group By Column",
            Description = parts[i],
          };
          subResult.SubResults.Add(subSubResult);
          SimpleExpression exp = new SimpleExpression(parts[i], ":");
          if (!exp.IsValid) {//do not process invalid expression
            subSubResult.Message = "Invalid Expression";
            continue;
          }
          subSubResult.Result = true;
          bool hasChecking = logic.UseDataDB && !string.IsNullOrWhiteSpace(tableSource);
          SyntaxCheckerResult leftSubSubResult = new SyntaxCheckerResult {
            DisplayText = "Column Name",
            Description = exp.LeftSide,
            Result = hasChecking ? logic.FindDataTableColumn(tableSource, exp.LeftSide) : true,
          };
          leftSubSubResult.Message = leftSubSubResult.Result ? hasChecking ? "Valid Column Name" :
            "(Loosely-Checked) Valid Column Name" : ("Column Name Not Found In [Data] Table [" + tableSource + "]");
          subSubResult.SubResults.Add(leftSubSubResult);
          if (exp.IsSingular) {
            GroupByColumns.Add(new KeyValuePair<string, string>(exp.LeftSide, null));
            continue;
          }
          SyntaxCheckerResult rightSubSubResult = new SyntaxCheckerResult {
            DisplayText = "Group By Auto Directive",
            Description = exp.RightSide,
          };
          subSubResult.SubResults.Add(rightSubSubResult);
          if (!Aibe.DH.GroupByAutoDirectives.Any(x => exp.RightSide.EqualsIgnoreCase(x))) { //not among the listed directive, not allowed
            GroupByColumns.Add(new KeyValuePair<string, string>(exp.LeftSide, null)); //so the exp.RightSide is NOT included here
            rightSubSubResult.Message = "Invalid Group By Auto Directive. Valid Directives: " + string.Join(", ", Aibe.DH.GroupByAutoDirectives);
            continue;
          }
          rightSubSubResult.Result = true;
          rightSubSubResult.Message = "Valid Group By Auto Directive";
          GroupByColumns.Add(new KeyValuePair<string, string>(exp.LeftSide, exp.RightSide));
        }
      }

      //If there is no column found, then put IsValid as false
      if (GroupByColumns.Count <= 0) {
        CheckerResult.Message = "Valid Group By Columns Not Found";
        CheckerResult.SubResults.Add(aggQueryResult);
        IsValid = false;
        return;
      }

      CheckerResult.SubResults.Add(aggQueryResult);
      CheckerResult.Result = true; //only true if it can reach this point
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}
