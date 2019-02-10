using Extension.Models;
using System.Collections.Generic;
using System.Text;

namespace Aibe.Models.Tests {
  public class ListColumnItemTest : BaseInfo {
    public List<ListColumnSubItemTest> SubItems { get; private set; } = new List<ListColumnSubItemTest>();
    public List<int> Widths { get; private set; }
    public string Type { get; private set; } = "LVO";
    public string CurrentDesc { get {
        if (SubItems == null)
          return string.Empty;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < SubItems.Count; ++i) {
          if (i > 0)
            sb.Append("|");
          sb.Append(SubItems[i].CurrentDesc);
        }
        return sb.ToString();
      }
    }

    //Called for creation, such as when add button is used in the javascript
    //legacy: TipLength=10|mm|mm,cm,m  OR  HasTipLength=True OR Ian=17|He is a good officer OR Name=val|ddc1,ddc2|passed!
    //new one: TipLength|10|mm|mm,cm,m  OR  HasTipLength|True OR Ian|17|He is a good officer OR Name|val|ddc1,ddc2|passed!
    public ListColumnItemTest(string desc, string lcType, List<int> widths) : base(desc) {
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "List Column Item";
      if (string.IsNullOrWhiteSpace(desc)) {
        CheckerResult.Message = "Invalid Item";
        return;
      }
      CheckerResult.Message = "Valid Item";
      CheckerResult.Result = true;
      IsValid = true; //this point onwards is OK
      Widths = widths;
      Type = lcType.ToUpper();
      string newDesc = getNonLegacyDesc(desc);
      var descParts = newDesc.Split('|'); //yes, split, because it can be empty
      int descIndex = 0; //start from 0
      int widthIndex = 0; //width index also starts from 0
      foreach (char c in Type) {
        bool hasDescPart = descIndex < descParts.Length;
        bool hasDescNextPart = descIndex + 1 < descParts.Length;
        bool hasWidthDefined = widths != null && widthIndex < widths.Count;
        int width = hasWidthDefined ? widths[widthIndex] : ListColumnInfoTest.DefaultWidth;
        ++widthIndex; //widthIndex always only increase by one
        if (c == 'L' || c == 'V') { //if c is label or value, then simply creates a sub-item
          ListColumnSubItemTest subItem = new ListColumnSubItemTest(hasDescPart ? descParts[descIndex] : string.Empty, c, width);
          SubItems.Add(subItem);
          ++descIndex;
          continue;
        }
        if (c == 'O' || c == 'C') { //option or check list, next part is thus expected
          StringBuilder sb = new StringBuilder(hasDescPart ? descParts[descIndex] : string.Empty);
          sb.Append("|");
          sb.Append(hasDescNextPart ? descParts[descIndex + 1] : string.Empty);
          ListColumnSubItemTest subItem = new ListColumnSubItemTest(sb.ToString(), c, width);
          SubItems.Add(subItem);
          descIndex += 2;
        }
      }
    }

    private string getNonLegacyDesc(string desc) {
      int index = desc.IndexOf('='); //check for legacy
      int indexVbar = desc.IndexOf('|');
      StringBuilder usedDesc = new StringBuilder();

      //has equal sign, for legacy purpose, AND
      //if index vbar does not exist or if index vbar comes after the equal sign, change the equal sign to vbar
      if (index >= 0 && (indexVbar < 0 || indexVbar > index)) {
        usedDesc.Append(desc.Substring(0, index));
        usedDesc.Append("|");
        if (desc.Length > index + 1) //means, there is something after the original equal sign
          usedDesc.Append(desc.Substring(index + 1));
        return usedDesc.ToString();
      }

      //has no equal sign, OR
      //has equal sign after the vbar, leave it alone
      return desc;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}