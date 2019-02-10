using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Tests {
  public class ActionProcedureInfoTest : CommonBaseInfo {
    public string TriggerName { get; private set; }
    public bool IsUser { get; private set; }
    public string FullProcedureString { get; private set; }
    public string ProcedureName { get; private set; }
    public Dictionary<string, string> ProcedureParameters { get; private set; } = new Dictionary<string, string>();
    public ActionProcedureInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Action Procedure";
      if (!HasRightSide) { //must have the right side to be valid
        CheckerResult.Message = "Procedure Does Not Exist";
        IsValid = false;
        return;
      }
      TriggerName = Name;
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Trigger Name",
        Description = TriggerName,
        Result = true,
        Message = "(Loosely-Checked) Valid Trigger Name",
      };
      CheckerResult.SubResults.Add(subResult);

      if (RightSide.StartsWith(Aibe.DH.UserPrefix)) {
        subResult = new SyntaxCheckerResult {
          DisplayText = "User DB Reference Prefix",
          Description = Aibe.DH.UserPrefix,
          Result = true,
          Message = "Valid User DB Reference Prefix",
        };
        CheckerResult.SubResults.Add(subResult);
        if (RightSide.Length <= Aibe.DH.UserPrefix.Length) {
          CheckerResult.Message = "Procedure Not Found";
          IsValid = false;
          return;
        }
        IsUser = true;
      }

      FullProcedureString = IsUser ? RightSide.Substring(Aibe.DH.UserPrefix.Length).Trim() : RightSide;

      subResult = new SyntaxCheckerResult {
        DisplayText = "Procedure Description",
        Description = FullProcedureString,
        Result = !string.IsNullOrWhiteSpace(FullProcedureString),
      };
      subResult.Message = subResult.Result ? "(Loosely-Checked) Valid Procedure Description" :
        "Procedure Not Found";
      CheckerResult.Result = subResult.Result; //at this point, this will depend on the existence of the procedure string
      CheckerResult.SubResults.Add(subResult);

      if (string.IsNullOrWhiteSpace(FullProcedureString)) {
        IsValid = false;
        return;
      }

      SimpleExpression exp = new SimpleExpression(FullProcedureString, "(");
      if (!exp.IsValid) {
        IsValid = false;
        CheckerResult.Result = false;
        CheckerResult.Message = "Invalid Procedure Description";
        return;
      }
      ProcedureName = exp.LeftSide;
      SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
        DisplayText = "Procedure Name",
        Description = ProcedureName,
        Result = logic.UseDataDB ?
        (IsUser ? logic.FindUserProcedure(ProcedureName) : logic.FindDataProcedure(ProcedureName)) :
          true, //always true if 
      };
      subSubResult.Message = subSubResult.Result ? 
        (logic.UseDataDB ? "(Loosely-Checked) " : "") + "Valid Procedure Name" : 
        "Procedure Name Not Found In [" + (IsUser ? "User" : "Data") + "] Database";
      subResult.SubResults.Add(subSubResult);

      if (exp.IsSingular) //has no parameter
        return; //simply returns, it has finished

      subSubResult = new SyntaxCheckerResult {
        DisplayText = "Procedure Parameter Descriptions",
        Description = "(" + exp.RightSide,
      };
      subResult.SubResults.Add(subSubResult);

      if (!exp.RightSide.EndsWith(")") || exp.RightSide.Length <= 1) {
        IsValid = false; //false because the right side does not contain the last ")"
        subSubResult.Message = "Procedure Descriptions With Parameters Must Be Enclosed Within Parentheses ( )";
        return;
      }

      string rightSide = exp.RightSide.Substring(0, exp.RightSide.Length - 1).Trim();
      if (string.IsNullOrWhiteSpace(rightSide)) {
        IsValid = false;
        subSubResult.Message = "Procedure Parameter Descriptions Not Found";
        return;
      }
      //has parameters
      var parPairs = rightSide.ParseComponents(',');
      int index = 0;
      string expectedProcedureName = string.Empty;
      List<string> expectedPars = new List<string>();
      if (!string.IsNullOrWhiteSpace(ProcedureName))
        if (IsUser && logic.UserDBProcedureParameterNames.Any(x => x.Key.EqualsIgnoreCase(ProcedureName)))
          expectedPars = logic.UserDBProcedureParameterNames.First(x => x.Key.EqualsIgnoreCase(ProcedureName)).Value;
        else if (!IsUser && logic.DataDBProcedureParameterNames.Any(x => x.Key.EqualsIgnoreCase(ProcedureName)))
          expectedPars = logic.DataDBProcedureParameterNames.First(x => x.Key.EqualsIgnoreCase(ProcedureName)).Value;
      string expectedPar = string.Empty;
      foreach (var parPair in parPairs) { //@SpPar=@@SomeItem.Something
        if (index < expectedPars.Count)
          expectedPar = expectedPars[index]; //get expected par for this particular index
        SimpleExpression subExp = new SimpleExpression(parPair, "=");
        SyntaxCheckerResult ppdResult = new SyntaxCheckerResult {
          DisplayText = "Procedure Parameter Description",
          Description = parPair,
        };
        subSubResult.SubResults.Add(ppdResult);
        if (!subExp.IsValid || subExp.IsSingular) {
          IsValid = false; //invalid, not immediately returns in the checking
          ppdResult.Message = "Invalid/Empty Procedure Parameter Description";
          if (subExp.IsValid && subExp.IsSingular) {
            SyntaxCheckerResult ppdResultParName = getPpnResult(subExp.LeftSide, expectedPar, index);
            ppdResult.SubResults.Add(ppdResultParName);
          }
        } else {
          bool isDuplicate = ProcedureParameters.ContainsKey(subExp.LeftSide);
          if (!isDuplicate)
            ProcedureParameters.Add(subExp.LeftSide, subExp.RightSide);
          SyntaxCheckerResult ppdResultParName = getPpnResult(subExp.LeftSide, expectedPar, index);
          ppdResult.SubResults.Add(ppdResultParName);
          SyntaxCheckerResult ppdResultParValue = new SyntaxCheckerResult {
            DisplayText = "Procedure Parameter Value",
            Description = subExp.RightSide,
            Result = true,
            Message = "(Loosely-Checked) Valid Procedure Parameter Value",
          };
          ppdResult.SubResults.Add(ppdResultParValue);
          ppdResult.Result = true;
        }
        ++index;
      }
      if (ProcedureParameters.Count > 0)
        subSubResult.Result = true;
      else
        subSubResult.Message = "Valid Procedure Parameter Descriptions Not Found";
    }

    //Get Procedure Parameter Name Result
    private SyntaxCheckerResult getPpnResult(string desc, string expectedPar, int index) {
      SyntaxCheckerResult ppdResultParName = new SyntaxCheckerResult {
        DisplayText = "Procedure Parameter Name",
        Description = desc,
        Result = logic.UseDataDB ?
        (IsUser ? logic.FindUserProcedureParameter(ProcedureName, desc) :
          logic.FindDataProcedureParameter(ProcedureName, desc)) :
          true, //always true
      };
      if (ppdResultParName.Result) {
        if (!logic.UseDataDB) {
          ppdResultParName.Message = "(Loosely-Checked) Valid Procedure Parameter Name";
        } else if (expectedPar.Equals(desc)) {
          ppdResultParName.Message = "Valid Procedure Parameter Name";
        } else {
          ppdResultParName.Result = false;
          ppdResultParName.Message = "Unexpected Procedure Parameter Name [" +
            desc + "] As Argument {" + index + "}. Expected: [" + expectedPar + "]";
        }
      } else
        ppdResultParName.Message = "Procedure Parameter Name Not Found In Procedure [" +
          ProcedureName + "] In [" + (IsUser ? "User" : "Data") + "] Database";
      return ppdResultParName;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}
