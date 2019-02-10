using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Aibe.Models.Tests {
  public class ScriptColumnInfoTest : CommonBaseInfo {
    public ScriptConstructorColumnInfoTest Constructor { get; private set; }
    public string TableSource { get; private set; }
    public string RefTableName { get { return Constructor.RefTableName; } }
    public string Script { get; private set; }
    public List<DataColumn> DataColumns { get; private set; }
    public List<string> Parameters { get; private set; } = new List<string>();
    private List<string> parsLowerCased = new List<string>();

    public ScriptColumnInfoTest(List<ScriptConstructorColumnInfoTest> infos, string tableSource, string desc) : base(desc) {
      //There must be info from 
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Script Column";
      if (!HasRightSide) {
        IsValid = false;
        CheckerResult.Message = "Incomplete Script";
        return;
      }

      if (infos == null) {
        IsValid = false;
        CheckerResult.Message = "Script Constructor Info Not Found";
        return;
      }

      if (!infos.Any(x => x.Name != null && x.Name.EqualsIgnoreCase(Name))) {
        IsValid = false;
        CheckerResult.Message = "Matching Script Constructor Info Not Found";
        return;
      }

      CheckerResult.Result = true;
      SyntaxCheckerResult subResult = new SyntaxCheckerResult {
        DisplayText = "Column Name",
        Description = Name,
        Result = true,
        Message = "(Loosely-Checked) Valid Column Name",
      };
      CheckerResult.SubResults.Add(subResult);

      //Get the constructor here
      ScriptConstructorColumnInfoTest constructorInfo = infos.FirstOrDefault(x => x.Name.EqualsIgnoreCase(Name));
      Constructor = constructorInfo;
      DataColumns = constructorInfo.DataColumns;

      subResult = new SyntaxCheckerResult {
        DisplayText = "Script",
        Description = RightSide,
        Result = true,
      };
      CheckerResult.SubResults.Add(subResult);

      //Check script validity here
      string scriptChecked = RightSide;
      //Get all script parameters
      var matches = Aibe.DH.ParRegex.Matches(scriptChecked);
      foreach (var match in matches) {
        string matchStr = match.ToString().Trim();
        SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
          DisplayText = "Parameter",
          Description = matchStr,
        };
        subResult.SubResults.Add(subSubResult);
        if (!Parameters.Any(x => x.EqualsIgnoreCase(matchStr))) {//only adds distinct items
          Parameters.Add(matchStr);
          subSubResult.Message = "Valid Parameter";
          subSubResult.Result = true;
        } else {
          subSubResult.Message = "Duplicate Parameter";
        }
      }
      parsLowerCased = Parameters.Select(x => x.ToLower()).ToList();

      //TODO currently just take as it is
      Script = scriptChecked;
      TableSource = tableSource;
    }

    public bool IsPictureLinkColumn(string columnName) {
      return Constructor != null && Constructor.PictureLinks != null && Constructor.PictureLinks.Any(x => x.EqualsIgnoreCase(columnName));
    }

    public int GetPictureWidthFor(string columnName) {
      if (!IsPictureLinkColumn(columnName))
        return 0;
      //https://stackoverflow.com/questions/4075340/finding-first-index-of-element-that-matches-a-condition-using-linq
      //Jon Skeet's idea:
      int index = Constructor.PictureLinks
                  .Select((v, i) => new { v, i = i + 1 })
                  .Where(p => p.v.EqualsIgnoreCase(columnName))
                  .Select(p => p.i)
                  .FirstOrDefault() - 1;
      if (index == -1)
        return 0;
      return Constructor.PictureWidths[index];
    }

    public DataTable GetDataTable(Dictionary<string, string> inputs) {
      if (Parameters == null || !Parameters.Any()) //no parameter, means just return whatever is needed
        return SQLServerHandler.GetDataTable(Aibe.DH.DataDBConnectionString, Script); //use original script
      if ((inputs == null || inputs.Count <= 0) && Parameters != null && Parameters.Any()) //cannot evaluate this for lacking of needed parameters
        return null;
      if (inputs.Count < Parameters.Count) //cannot evaluate if object dictionary is smaller than the parameters
        return null;
      var tests = inputs.Keys.Select(x => Aibe.DH.PP + x.ToLower().Trim());
      if (parsLowerCased.Except(tests).Any()) //if lowerCased pars cannot find its value in the inputs, then skips it
        return null; //unable to evaluate
      string processedScript = Script; //select * from TestTable where a=@@blabla.best and b=@@blabla.mada;
      foreach (var par in Parameters) { //@@blabla.best, @@blabla.mada
        var kvp = inputs.FirstOrDefault(x => (Aibe.DH.PP + x.Key).EqualsIgnoreCase(par)); //get the kvp to replace the actual value 
        processedScript = processedScript.Replace(par, string.IsNullOrWhiteSpace(kvp.Value) ?
          Aibe.DH.NULL : kvp.Value.AsSqlStringValue()); //no need to know that, process everything as string
      } //at the end of the loop, the processed script is ready to get the data from the table
      return SQLServerHandler.GetDataTable(Aibe.DH.DataDBConnectionString, processedScript);
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}