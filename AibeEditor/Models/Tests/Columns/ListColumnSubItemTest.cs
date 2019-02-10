using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Aibe.Models.Tests {
  public class ListColumnSubItemTest : BaseInfo {
    public string Value { get; set; }
    public List<string> Options { get; private set; }
    public bool HasOptionOfItsValue { get { return Options != null && !string.IsNullOrWhiteSpace(Value) && Options.Any(x => x.Equals(Value)); } }
    public bool HasOptions { get { return Options != null && Options.Count > 0; } }
    public char SubItemType { get; private set; } //extra info which will be useful for extraction later!
    public int Width { get; private set; } = ListColumnInfoTest.DefaultWidth; //assuming default width unless specified otherwise
    private TableValueRefInfoTest refInfo { get; set; }
    public string CurrentDesc { get {
        StringBuilder sb = new StringBuilder(Value);
        if (Options == null || Options.Count <= 0) //if it has no option, return immediately
          return sb.ToString();
        sb.Append("|"); //if it has options, then return this
        sb.Append(string.Join(",", Options));
        return sb.ToString();
      }
    }

    public ListColumnSubItemTest(string desc, char subItemType, int width) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "List Column Sub-Item";
      CheckerResult.Result = true;
      CheckerResult.Message = "Valid Sub-Item";
      IsValid = true; //this point onwards is OK
      SubItemType = subItemType; //for now, no need to check this (if it is among the allowed list)... TODO
      if (width > 0)
        Width = width; //valid width is applied
      if (desc == null) { //desc can be null yet valid
        Value = string.Empty;
        return;
      }
      int index = desc.IndexOf('|');
      if (index < 0) { //vbar not found
        Value = desc;
        return;
      }
      Value = desc.Substring(0, index).Trim();
      if (desc.Length <= index + 1) //has vbar, but has nothing afterwards
        return;
      string optionsStr = desc.Substring(index + 1).Trim();
      if (string.IsNullOrWhiteSpace(optionsStr))
        return;
      if (isValidReference(optionsStr)) { //then, the refInfo will also be present here
        Options = refInfo.GetStaticDropDownItems();
        //Now, treat this most specially if there is a description
      } else
        Options = optionsStr.GetTrimmedNonEmptyParts(','); //set the options here
    }

    public bool HasOption(string option) { return Options != null && Options.Any(x => x.Equals(option)); } //must use Equals here, not EqualsIgnoreCase

    private bool isValidReference(string desc) {
      var parts = desc.GetTrimmedNonEmptyParts(':');
      if (parts == null || parts.Count < 3 && parts.Count > 5) //can only consists of 3 to 5 parts. Support additional where clause
        return false;
      if (!parts[0].EqualsIgnoreCase(Aibe.DH.Ref))
        return false;
      int index = desc.IndexOf(':');
      string refString = desc.Substring(index + 1).Trim();
      TableValueRefInfoTest refInfo = new TableValueRefInfoTest(refString);
      if (!refInfo.IsValid) //cannot be invalid
        return false;
      if (!refInfo.IsSelectUnconditional && !refInfo.CrossTableColumnIsStatic) //if the select is conditional, then it cannot be dynamic
        return false;
      this.refInfo = refInfo;
      return true;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}
