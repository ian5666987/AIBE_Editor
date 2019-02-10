using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Aibe.Models.Tests {
  public class ScriptConstructorColumnInfoTest : CommonBaseInfo {
    public string TableSource { get; private set; } //Table source of origin, where this Column is located
    public string RefTableName { get; private set; } //Reference table name, where this Column gets its data from
    public string ScriptConstructor { get; private set; }
    public List<DataColumn> DataColumns { get; private set; }
    public List<string> PictureLinks { get; private set; } = new List<string>();
    public List<int> PictureWidths { get; private set; } = new List<int>(); //TODO as of now, the way to do this is not so good, but leave if for now... it should be just use the PictureColumnInfo
    //ColumnName1|AttrName1A:AttrVal1A|AttrName1B:AttrVal1B|…|AttrName1Z:AttrVal1Z=Script1;ColumnName2=Script2;…;ColumnNameN=ScriptN
    public ScriptConstructorColumnInfoTest(string tableSource, string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Script Constructor Column";
      if (!HasRightSide || string.IsNullOrWhiteSpace(tableSource)) {
        IsValid = false;
        CheckerResult.Message = "Invalid Description Or No Table Source";
        return;
      }

      //The "Name" here is in the form of Name|attrName:item1,item2,...,itemN, thus use further divider
      var parts = Name.GetTrimmedNonEmptyParts('|');
      Name = parts[0]; //the actual "Name" is only the first part
      if (string.IsNullOrWhiteSpace(Name)) {
        CheckerResult.Message = "Empty Column Name";
        IsValid = false;
        return;
      }

      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Column Name",
        Description = parts[0],
        Result = true,
        Message = "(Loosely-Checked) Valid Column Name"
      };
      CheckerResult.SubResults.Add(subResult);

      if (parts.Count > 1) //has some other parts
        for (int i = 1; i < parts.Count; ++i) { //for 
          subResult = new SyntaxCheckerResult {
            DisplayText = "Attribute",
            Description = parts[i],
          };
          CheckerResult.SubResults.Add(subResult);
          SimpleExpression exp = new SimpleExpression(parts[i], ":");
          if (!exp.IsValid) {//do not process singular or invalid expression
            subResult.Message = "Invalid Attribute Description";
            continue;
          }
          if (exp.IsSingular) {
            subResult.Message = "Incomplete Attribute Description";
            continue;
          }
          if (!Aibe.DH.ScAttributes.Any(x => exp.LeftSide.EqualsIgnoreCase(x))) {//not among the listed attribute, not allowed
            subResult.Message = "Unknown Attribute [" + exp.LeftSide + "]. Acceptable Attributes: " + string.Join(", ", Aibe.DH.ScAttributes);
            continue;
          } else {
            SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
              DisplayText = "Attribute Name",
              Description = exp.LeftSide,
              Result = true,
              Message = "Valid Attribute Name"
            };
            subResult.SubResults.Add(subSubResult);
          }
          subResult.Result = true;

          if (exp.LeftSide.EqualsIgnoreCase(Aibe.DH.ScPictureLinksAttribute)) { //handles correct attributes
            var subparts = exp.RightSide.GetTrimmedNonEmptyParts(','); //ColumnName1#Width1, ColumnName2#Width2, ..., ColumnNameN#WidthN
            SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
              DisplayText = "Picture Link Attribute Values",
              Description = exp.RightSide,
              Result = true,
            };
            subResult.SubResults.Add(subSubResult);
            foreach (var subpart in subparts) { //each subpart is in the form of ColumnName#Width like Photo#100
              SyntaxCheckerResult partSubSubResult = new SyntaxCheckerResult {
                DisplayText = "Picture Link Attribute Value",
                Description = subpart,
              };
              subSubResult.SubResults.Add(partSubSubResult);
              SimpleExpression subExp = new SimpleExpression(subpart, "#");
              if (!subExp.IsValid) {
                partSubSubResult.Message = "Invalid Value";
                continue;
              }
              partSubSubResult.Result = true;
              SyntaxCheckerResult leftPartSubSubResult = new SyntaxCheckerResult {
                DisplayText = "Column Name",
                Description = subExp.LeftSide,
                Result = true,
                Message = "(Loosely-Checked) Valid Column Name"
              };
              partSubSubResult.SubResults.Add(leftPartSubSubResult);
              PictureLinks.Add(subExp.LeftSide);
              int width = PictureColumnInfoTest.DefaultWidth;
              bool result = false;
              if (!subExp.IsSingular) {
                result = int.TryParse(subExp.RightSide, out width);
                SyntaxCheckerResult rightPartSubSubResult = new SyntaxCheckerResult {
                  DisplayText = "Width",
                  Description = subExp.RightSide,
                  Result = result && width > 0,
                };
                rightPartSubSubResult.Message = rightPartSubSubResult.Result ? "Valid Width" : 
                  "Invalid Width. Default Width [" + PictureColumnInfoTest.DefaultWidth + "] Shall Be Applied";
                partSubSubResult.SubResults.Add(rightPartSubSubResult);
              }
              PictureWidths.Add(result && width > 0 ? width : PictureColumnInfoTest.DefaultWidth);
            }
          } else if (exp.LeftSide.EqualsIgnoreCase(Aibe.DH.ScRefTableNameAttribute)) {
            RefTableName = exp.RightSide; //RightSide is like MyRefTableName
            SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
              DisplayText = "Reference Table Attribute Value",
              Description = exp.RightSide,
              Result = true,
              Message = "Valid Attribute Value"
            };
            subResult.SubResults.Add(subSubResult);
          } else { //add as many "else if" as necessary
            subResult.Message = "Unlisted Attribute [" + exp.LeftSide + "]";
          }
        }      

      try {
        DataColumns = SQLServerHandler.GetColumns(Aibe.DH.DataDBConnectionString, tableSource); //get the DataColumns of the table first
      } catch {
        IsValid = false;
        CheckerResult.Message = "Fail To Populate Data Columns From Database";
        return;
      }

      //For now, if the DataColumn does not contain Cid, it would not be allowed to proceed, this is because picture image needs this Cid
      if (DataColumns == null || DataColumns.Count <= 0 || !DataColumns.Any(x => x.ColumnName.EqualsIgnoreCase(Aibe.DH.Cid))) {
        IsValid = false;
        CheckerResult.Message = "Data Columns Not Found Or Invalid";
        return;
      }

      //TODO check script constructor validity here
      ScriptConstructor = RightSide;
      TableSource = tableSource;
      CheckerResult.Result = true;
      SyntaxCheckerResult constResult = new SyntaxCheckerResult {
        DisplayText = "Constructor Script",
        Description = RightSide,
        Result = true,
        Message = "Non-Checked Constructor Script",
      };
      CheckerResult.SubResults.Add(constResult);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}