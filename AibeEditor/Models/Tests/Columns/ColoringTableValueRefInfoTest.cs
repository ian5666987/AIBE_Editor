using System.Collections.Generic;
using Extension.String;
using Extension.Models;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class ColoringTableValueRefInfoTest : BaseInfo { //It is actually used only by ColoringInfo, nevertheless, leave it separated for now
    //valid self keyword
    private static string self = Aibe.DH.Self;
    //valid comparators
    private static List<string> operators = new List<string> { "+", "-" };
    public string RefTableName { get; private set; }
    public string RefTableColumn { get; private set; }
    public bool IsSelf { get; private set; }
    public int Cid { get; private set; }
    public string Operator { get; private set; }
    public int ShiftValue { get; private set; }
    public bool HasLastExpression { get; private set; }
    public const string SpecialInsufficientErrorName = "Insufficient";
    public ColoringTableValueRefInfoTest(string desc) : base(desc) {
      CheckerResult.DisplayText = "Coloring Table Value Reference";
      CheckerResult.Description = desc;

      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Empty Text";
        return;
      }
      var parts = desc.GetTrimmedNonEmptyParts(':');
      if (parts.Count < 2) {//minimum contains of two parts
        CheckerResult.Name = SpecialInsufficientErrorName; //this is a unique cause of error, returned and then shall be deleted...
        CheckerResult.Message = "Insufficient Number Of Components";
        return;
      }
      IsValid = true;
      RefTableName = parts[0];
      RefTableColumn = parts[1];
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Table Name",
        Description = parts[0],
        Result = logic.UseDataDB ? logic.FindDataTable(RefTableName) : true,
      };
      subResult.Message = subResult.Result ? "Valid Table Name" : "Table Name Not Found In Data DB";
      CheckerResult.SubResults.Add(subResult);
      subResult = new SyntaxCheckerResult {
        DisplayText = "Reference Table Column",
        Description = parts[1],
        Result = logic.UseDataDB ? logic.FindDataTableColumn(RefTableName, RefTableColumn) : true,
      };
      subResult.Message = subResult.Result ? "Valid Column Name" : "Column Name Not Found In Table [" + RefTableName + "]";
      CheckerResult.SubResults.Add(subResult);
      if (parts.Count < 3)
        return;

      string lastExpression = parts[2];

      SimpleExpression exp = new SimpleExpression(lastExpression, operators);
      if (!exp.IsValid) {
        CheckerResult.Message = "Invalid Expression On [" + lastExpression + "]";
        IsValid = false;
        return;
      }

      subResult = new SyntaxCheckerResult {
        DisplayText = "Expression",
        Description = lastExpression,
      };

      //get either self or cid
      IsSelf = exp.LeftSide.ToUpper().Equals(self);
      if (!IsSelf) {
        int value;
        bool result = int.TryParse(exp.LeftSide, out value);
        if (result && value > 0) { //this is proven to be a Cid value
          Cid = value;
          SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
            DisplayText = "Cid",
            Description = exp.LeftSide,
            Result = true,
            Message = "Valid Cid Expression",
          };
          subResult.SubResults.Add(subSubResult);
        }
      }

      //If not singular, then operates further the right hand side
      if (!exp.IsSingular) {
        int value;
        bool result = int.TryParse(exp.RightSide, out value);
        if (result && value > 0) {
          ShiftValue = value;
          SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
            DisplayText = "Shift Value",
            Description = exp.RightSide,
            Result = true,
            Message = "Valid Shift Value Expression",
          };
          subResult.SubResults.Add(subSubResult);
        }
      }

      //if the third expression part exists
      IsValid = (IsSelf || Cid > 0) && (exp.IsSingular || ShiftValue > 0); //can only be valid if Cid is found or it is self...
                                                                           //And must either be singular or (if not singular) contains the valid shift value
      HasLastExpression = IsValid; //up to this point assume to have last expression, if it is valid
      subResult.Result = IsValid;
      if (!IsValid) {
        CheckerResult.Message = "Invalid Self Or Cid Expression On [" + lastExpression + "]";
        subResult.Message = "Invalid Self Or Cid Expression";
      } else {
        subResult.Message = "Valid Self Or Cid Expression";
      }
      CheckerResult.SubResults.Add(subResult);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}