using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Aibe.Models.Tests {
  //ColumnName1|HeaderName11,HeaderName12,…,HeaderName1N=ListType|RefTableName:RefColumnName:RefAnotherColumnName=ThisOtherColumnName:AddWhereAndClause;
  public class ListColumnInfoTest : CommonBaseInfo {
    #region legacy
    private static List<string> defaultListTypes = new List<string> { "default", "check", "list", "remarks", "dropdown" };
    private static Dictionary<string, string> legacyToListTypeDictionary = new Dictionary<string, string> {
      { "default", "LVO" }, { "check", "LC" }, { "list", "L" }, { "remarks", "LVV" }, { "dropdown", "LOV" }
    };
    #endregion

    private static List<char> listTypeLetters = new List<char> { 'L', 'V', 'O', 'C' };
    public string DefaultHeaderPrefix;
    public new string Name { get; private set; } //this is hiding the original name, but make use of it
    public List<string> HeaderNames { get; private set; }
    public string ListType { get; private set; } = "LVO";
    public static int DefaultWidth { get; private set; } = 20;
    public List<int> Widths { get; private set; }
    public TableValueRefInfoTest TemplateRef { get; private set; }
    public ListColumnInfoTest(string desc) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "List Column Info";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      DefaultHeaderPrefix = LCZ.W_Header; //initialized everytime now due to the localization
      HeaderNames = new List<string> { LCZ.W_Name, LCZ.W_Value, LCZ.W_Ending };
      Widths = new List<int> { DefaultWidth, DefaultWidth, DefaultWidth }; //same number as HeaderNames

      List<string> baseNameParts = base.Name.GetTrimmedNonEmptyParts('|');
      if(baseNameParts.Count <= 0) { //it cannot be without name
        CheckerResult.Message = "Column Name Not Found";
        IsValid = false;
        return;
      }
      Name = baseNameParts[0]; //The name exists
      if (baseNameParts.Count >= 2) { //have header names and width
        var headerWidthParts = baseNameParts[1].GetTrimmedNonEmptyParts(',');
        HeaderNames = new List<string>();
        Widths = new List<int>();

        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Header Name-Width List",
          Description = baseNameParts[1],
        };
        CheckerResult.SubResults.Add(subResult);

        foreach(var headerWidthPart in headerWidthParts) {
          var parts = headerWidthPart.GetTrimmedNonEmptyParts('#');
          SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
            DisplayText = "Name-Width",
            Description = headerWidthPart,
          };
          subResult.SubResults.Add(subSubResult);
          if (parts == null || parts.Count <= 0) {
            subSubResult.Message = "Component(s) Not Found";
            continue;
          }
          HeaderNames.Add(parts[0]);
          SyntaxCheckerResult subSubHeaderResult = new SyntaxCheckerResult {
            DisplayText = "Name",
            Description = parts[0],
            Result = true,
            Message = "Header Name: " + parts[0],
          };
          subSubResult.SubResults.Add(subSubHeaderResult);
          if (parts.Count > 1) {
            int width;
            bool result = int.TryParse(parts[1], out width);
            SyntaxCheckerResult subSubWidthResult = new SyntaxCheckerResult {
              DisplayText = "Width",
              Description = parts[1],
              Result = result,
            };
            subSubWidthResult.Message = result && width > 0 ? "Header Width: " + parts[1] : 
              "Invalid Header Width, Default Width [" + DefaultWidth + "] Shall Be Applied";
            subSubResult.SubResults.Add(subSubWidthResult);
            Widths.Add(result && width > 0 ? width : DefaultWidth);
          } else
            Widths.Add(DefaultWidth);
          subSubResult.Result = subSubResult.SubResults.Any() ? subSubResult.SubResults.All(x => x.Result) : true;
        }
        subResult.Result = subResult.SubResults.All(x => x.Result);
        subResult.Message = (subResult.Result ? "Valid" : "Invalid") + " Header Name-Width List";
      }

      if (!HasRightSide) //it is ok not to have the right side for ListColumnInfo
        return;

      //Actually, where clause also cannot have "|"
      var rightParts = RightSide.GetTrimmedNonEmptyParts('|');

      if (rightParts.Count < 1) {
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Right Part",
          Description = RightSide,
          Result = false,
          Message = "Incomplete Right Part Description"
        };
        CheckerResult.SubResults.Add(subResult);
        return;
      }

      SyntaxCheckerResult listTypeResult = new SyntaxCheckerResult {
        DisplayText = "List Type",
        Description = rightParts[0],
      };
      CheckerResult.SubResults.Add(listTypeResult);
      if (defaultListTypes.Any(x => x.EqualsIgnoreCaseTrim(rightParts[0]))) { //If it is a valid legacy type        
        string legacyListType = defaultListTypes.FirstOrDefault(x => x.EqualsIgnoreCaseTrim(rightParts[0]));
        if (legacyToListTypeDictionary.ContainsKey(legacyListType)) {
          ListType = legacyToListTypeDictionary[legacyListType]; //convert the legacy type to the new type
          listTypeResult.Message = "Valid List Type (Legacy). Equivalent To: " + ListType;
          listTypeResult.Result = true;
        } else {
          listTypeResult.Message = "Invalid List Type (Legacy). Valid List Types (Legacy): " + 
            string.Join(", ", legacyToListTypeDictionary.Keys.ToArray());
        }
      } else if (rightParts[0].ToUpper().All(x => listTypeLetters.Contains(x))) { //check if it is a valid list type, all letters must be checked (they must all be allowed letters)
        ListType = rightParts[0].ToUpper(); //apply the list type here
        listTypeResult.Message = "Valid List Type";
        listTypeResult.Result = true;
      } else {
        listTypeResult.Message = "Invalid List Type. List Type May Only Contain The Following Letters: " + 
          string.Join(", ", listTypeLetters);
      }

      if (rightParts.Count < 2)
        return;      

      TemplateRef = new TableValueRefInfoTest(rightParts[1]);
      TemplateRef.CheckerResult.DisplayText = "List Column Template Reference";
      CheckerResult.SubResults.Add(TemplateRef.CheckerResult);
      if (!TemplateRef.IsValid) { //if it has reference, it must be valid. Otherwise revoke the validity
        IsValid = false;
        CheckerResult.Message = "Invalid List Column Template Reference";
        return;
      }
    }

    private int headerIndex = -1;

    public void ResetHeaderCount() {
      headerIndex = -1;
    }

    public string GetNextHeader() {
      ++headerIndex;
      return HeaderNames != null && HeaderNames.Count > headerIndex ?
        HeaderNames[headerIndex] : DefaultHeaderPrefix + " " + (headerIndex + 1).ToString();
    }

    public bool GetRefDataValue(string changedColumnName, string changedColumnValue, out string dataValue) {
      dataValue = string.Empty;
      if (TemplateRef == null ||
        string.IsNullOrWhiteSpace(TemplateRef.RefTableName) ||
        string.IsNullOrWhiteSpace(TemplateRef.Column) ||
        string.IsNullOrWhiteSpace(TemplateRef.CondColumn) ||
        (string.IsNullOrWhiteSpace(TemplateRef.StaticCrossTableCondColumn) &&  //if static cross-cond-column is empty
           (string.IsNullOrWhiteSpace(TemplateRef.CrossTableCondColumn) || //the dynamic cannot be empty
            string.IsNullOrWhiteSpace(changedColumnValue) || //the changed column value cannot be empty 
            string.IsNullOrWhiteSpace(changedColumnName) || //the changed column name cannot be empty
            !TemplateRef.CrossTableCondColumn.EqualsIgnoreCase(changedColumnName)))) //or if the changedColumnName is not equal to CrossTableCondColumn
        return false;

      try {
        //Script making.
        StringBuilder selectScript = new StringBuilder(string.Concat(
          "SELECT DISTINCT [", TemplateRef.Column, "] FROM [", TemplateRef.RefTableName, "] WHERE [",
          TemplateRef.Column, "] IS NOT ", Aibe.DH.NULL, " AND [", TemplateRef.CondColumn, "] = @par")
         );

        if (!string.IsNullOrWhiteSpace(TemplateRef.AdditionalWhereClause)) {
          selectScript.Append(" AND (");
          selectScript.Append(TemplateRef.AdditionalWhereClause);
          selectScript.Append(")");
        }

        string appliedValue = string.IsNullOrWhiteSpace(TemplateRef.CrossTableCondColumn) &&
          !string.IsNullOrWhiteSpace(TemplateRef.StaticCrossTableCondColumn) ?
          TemplateRef.StaticCrossTableCondColumn : changedColumnValue;
        SqlParameter par = new SqlParameter("@par", appliedValue);
        DataTable dataTable = SQLServerHandler.GetDataTable(Aibe.DH.DataDBConnectionString, selectScript.ToString(), par);

        if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count <= 0 ||
          dataTable.Rows[0].ItemArray == null || dataTable.Rows[0].ItemArray.Length <= 0 ||
          dataTable.Rows[0].ItemArray[0] == null)
          return false;

        dataValue = dataTable.Rows[0].ItemArray[0].ToString();
        return true;
      } catch {
        return false;
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}
