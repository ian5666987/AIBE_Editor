using Extension.Models;
using Extension.String;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class ColoringInfoTest : BaseInfo { //it is not derived from CommonBaseInfo
    public string ColumnName { get; private set; } //something like "TeamAssigned"
    public string ConditionColumnName { get; private set; }
    public string ComparatorSign { get; private set; }
    public ComparisonExpressionInfoTest CompExp { get; private set; }
    public string Color { get; private set; }
    public bool ConditionColumnIsDifferentColumn { get { return ConditionColumnName != ColumnName; } }

    public ColoringInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Coloring Info";
      string descContent = desc.GetNonEmptyTrimmedInBetween("[", "]");
      if (string.IsNullOrWhiteSpace(descContent)) {
        CheckerResult.Message = "Invalid Syntax: The Description Must Be In Between []";
        return;
      }
      var descParts = descContent.GetTrimmedNonEmptyParts('|');
      if (descParts == null || descParts.Count != 4) {//it must consists of exactly 4 parts
        CheckerResult.Message = "Invalid Syntax: Components Must Consist Of Exactly 4 Parts";
        return;
      }
      CompExp = new ComparisonExpressionInfoTest(descParts[2]);
      CompExp.CheckerResult.Result = CompExp.IsValid;
      if (!CompExp.IsValid) { //the comparison expression must be valid
        CheckerResult.SubResults.Add(CompExp.CheckerResult);
        CheckerResult.Message = "Invalid Comparison Expression";
        return;
      }
      if (!Aibe.DH.ValidComparatorSigns.Any(x => x.Equals(descParts[1]))) { //the comparison code must also be valid
        CheckerResult.SubResults.Add(CompExp.CheckerResult);
        CheckerResult.Message = "Invalid Comparison Sign. Valid Comparator Signs: " + string.Join(", ", Aibe.DH.ValidComparatorSigns);
        return;
      }

      SimpleExpression colExp = new SimpleExpression(descParts[0], ",");
      ColumnName = colExp.LeftSide;
      ConditionColumnName = colExp.IsSingular ? colExp.LeftSide : colExp.RightSide;
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        Name = "ColumnName",
        DisplayText = "Column Name",
        Description = colExp.LeftSide,
        //Pending the result for column name
      };
      CheckerResult.SubResults.Add(subResult);
      if (!colExp.IsSingular) {
        subResult = new SyntaxCheckerResult {
          Name = "ConditionColumnName",
          DisplayText = "Condition Column Name",
          Description = colExp.RightSide,
          //Pending the result for condition column name
        };
        CheckerResult.SubResults.Add(subResult);
      }

      ComparatorSign = descParts[1];
      subResult = new SyntaxCheckerResult {
        DisplayText = "Comparator Sign",
        Description = descParts[1],
        Result = true,
        Message = "Valid Compator Sign",
      };
      CheckerResult.SubResults.Add(subResult);

      CheckerResult.SubResults.Add(CompExp.CheckerResult); //enter the result here as desc[2]

      Color = descParts[3];
      subResult = new SyntaxCheckerResult {
        DisplayText = "Color",
        Description = descParts[3],
        Result = true,
        Message = "Valid Color",
      };
      CheckerResult.SubResults.Add(subResult);

      IsValid = true;
      CheckerResult.Result = true;
    }

    public class ComparisonExpressionInfoTest : BaseInfo {
      //valid now keyword
      private static string now = Aibe.DH.Now;
      //valid date time format
      public readonly static string DateTimeFormat = "M/d/yyyy HH:mm:ss";
      //valid comparators
      private static List<string> operators = new List<string> { "+", "-" };
      public string AggregateName { get; private set; }
      public ColoringTableValueRefInfoTest RefInfo { get; private set; }
      public bool IsNow { get; private set; }
      public DateTime? DateTimeValue { get; private set; }
      public string Operator { get; private set; } //If it is null, means there isn't operator
      public double NumberWithOperator { get; private set; }
      public ExpressionType ExpType { get; private set; } = ExpressionType.Raw;
      public double ShiftValue { get; private set; }
      public ComparisonExpressionInfoTest(string desc) : base(desc) {
        CheckerResult.DisplayText = "Comparison Expression";
        CheckerResult.Description = desc;

        if (string.IsNullOrWhiteSpace(desc)) {
          CheckerResult.Message = "Empty Text";
          return;
        }
        //Check if it starts with aggregation.
        bool isPossiblyAggregate = Aibe.DH.AggregateNames.Any(x => desc.ToUpper().StartsWith(x));
        if (isPossiblyAggregate) {
          string checkedAggregateValue = Aibe.DH.AggregateNames.FirstOrDefault(x => desc.ToUpper().StartsWith(x));
          if (desc.Length <= checkedAggregateValue.Length) {//no definition, not possible to be correct aggregate
            CheckerResult.Message = "Aggregate Text Is Too Short";
            return;
          }
          string descContent = desc.Substring(checkedAggregateValue.Length).GetNonEmptyTrimmedInBetween("(", ")");
          if (string.IsNullOrWhiteSpace(descContent)) {//cannot be empty
            CheckerResult.Message = "Aggregate Text Is Empty";
            return;
          }
          ColoringTableValueRefInfoTest testRefInfo = new ColoringTableValueRefInfoTest(descContent);
          testRefInfo.CheckerResult.Result = testRefInfo.IsValid;
          CheckerResult.SubResults.Add(testRefInfo.CheckerResult);
          if (testRefInfo.IsValid) {
            ExpType = ExpressionType.Aggregation;
            AggregateName = checkedAggregateValue;
            RefInfo = testRefInfo;
            IsValid = true;
            CheckerResult.Message = "Valid [" + ExpType.ToString().ToCamelBrokenString() + "] Expression";
            SyntaxCheckerResult result = new SyntaxCheckerResult {
              DisplayText = "Aggregate Name",
              Description = AggregateName,
              Result = true,
              Message = "Valid Aggregate Name",
            };
            CheckerResult.SubResults.Add(result);
          }
        } else { //it must not be aggregate, then it can be anything..., but the most important type is if NOW or having datetime value, or others...

          //The very first test must be if it is a table reference value
          ColoringTableValueRefInfoTest testRefInfo = new ColoringTableValueRefInfoTest(desc);
          testRefInfo.CheckerResult.Result = testRefInfo.IsValid;
          if (testRefInfo.IsValid) {
            ExpType = ExpressionType.TableValueReference;
            RefInfo = testRefInfo;
            IsValid = true;
            CheckerResult.Message = "Valid [" + ExpType.ToString().ToCamelBrokenString() + "] Expression";
            CheckerResult.SubResults.Add(testRefInfo.CheckerResult);
            return; //go no further, terminates here
          } else if (testRefInfo.CheckerResult.Name != ColoringTableValueRefInfoTest.SpecialInsufficientErrorName) { //only if it is not a special error it shall be added
            CheckerResult.SubResults.Add(testRefInfo.CheckerResult);
          }

          SimpleExpression exp = new SimpleExpression(desc, operators);
          if (!exp.IsValid) {//invalid expression, just return
            CheckerResult.Message = "Invalid Expression On [" + desc + "]";
            return;
          }

          //Second test is now, followed by DateTime, then the rests...
          IsNow = exp.LeftSide.EqualsIgnoreCase(now); //test if it is now
          SyntaxCheckerResult leftExpResult = new SyntaxCheckerResult {
            DisplayText = "Left Expression",
            Description = exp.LeftSide,
            Result = true,
          };
          if (IsNow) {
            ExpType = ExpressionType.Now;
            leftExpResult.Message = "Now Expression";
            CheckerResult.SubResults.Add(leftExpResult);
          } else { //test date time
            DateTime testDateTime;
            bool result = DateTime.TryParseExact(exp.LeftSide, DateTimeFormat, null, System.Globalization.DateTimeStyles.AssumeLocal, out testDateTime);
            if (result) {
              DateTimeValue = testDateTime;
              ExpType = ExpressionType.DateTime;
              leftExpResult.Message = "Date Time Expression";
              CheckerResult.SubResults.Add(leftExpResult);
            }
          }

          double testNumberFull;
          bool resultFull = double.TryParse(desc, out testNumberFull); //test the whole item, see if it is a pure number
          if (ExpType == ExpressionType.Raw && resultFull) //If it is still raw until this point, check if the expression is a pure number
            ExpType = ExpressionType.Number;

          if (exp.IsSingular) { //if it is singular expression, it is always valid here
            IsValid = true;
            CheckerResult.Message = "Valid [" + ExpType.ToString().ToCamelBrokenString() + "] Expression";
          } else { //if it is non-singular expression, the right hand must be correct number if it is not raw, or it is completely raw
                   //If it is raw and has right hand side, then it must either:
                   // 1. the left hand side is number and the right hand side too, or
                   // 2. it is truly raw
            double testNumberRight;
            bool resultRight = double.TryParse(exp.RightSide, out testNumberRight);
            if (ExpType == ExpressionType.Raw) {
              double testNumberLeft;
              bool resultLeft = double.TryParse(desc, out testNumberLeft); //test the left item, see if it is a number
              if (resultLeft && resultRight && testNumberLeft > 0 && testNumberRight > 0) { //the number must be greater than zero
                ExpType = ExpressionType.NumberWithOperator;
                NumberWithOperator = testNumberLeft;
                ShiftValue = testNumberRight;
                Operator = exp.MiddleSign; //the operator is counted here, since it is in number
                SyntaxCheckerResult subExpResult = new SyntaxCheckerResult {
                  DisplayText = "Number With Operator",
                  Result = true,
                  Description = testNumberLeft.ToString(),
                  Message = "Valid Expression",
                };
                CheckerResult.SubResults.Add(subExpResult);
                subExpResult = new SyntaxCheckerResult {
                  DisplayText = "Shift Value",
                  Result = true,
                  Description = testNumberRight.ToString(),
                  Message = "Valid Expression",
                };
                CheckerResult.SubResults.Add(subExpResult);
                subExpResult = new SyntaxCheckerResult {
                  DisplayText = "Operator",
                  Result = true,
                  Description = exp.MiddleSign.ToString(),
                  Message = "Valid Operator",
                };
                CheckerResult.SubResults.Add(subExpResult);
              } //it is not a number but have operator, the operator is not counted here, and the type remains raw
              IsValid = true; //in whichever case, the non singular expression with raw is always valid (either as raw or as number with operator)
            } else { //it is non singular and it is not raw (now or datetime), then the right hand-side must be a valid positive number, else it is invalid
              IsValid = resultRight && testNumberRight > 0; //other than these, the non singular expression will be invalid
            }
            CheckerResult.Message = (IsValid ? "Valid" : "Invalid") + " [" + ExpType.ToString().ToCamelBrokenString() + "] Expression";
          }
        }
      }
      public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();

      public int GetTrueShiftValue() { //check the value before make it integer is its additional features
        int value = 0;
        if(ExpType == ExpressionType.TableValueReference ||
          ExpType == ExpressionType.Aggregation) {
          if (RefInfo != null && RefInfo.HasLastExpression) {
            if (RefInfo.Operator == "+") {
              value = RefInfo.ShiftValue;
            } else if (RefInfo.Operator == "-") {
              value = RefInfo.ShiftValue;
              value *= -1;
            }
          }
        }
        return value;
      }

      public enum ExpressionType {
        Raw, //either string or number
        Number,
        Now,
        DateTime,
        TableValueReference,
        NumberWithOperator, //can only be true if it has operator
        Aggregation, //must also be table value reference by definition
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}