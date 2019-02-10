using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class DropDownInfoTest : CommonBaseInfo {
    private static List<string> validOrderByDirectives = new List<string>() { Aibe.DH.AscOrderWord, Aibe.DH.DescOrderWord };
    public string OrderByDirective { get; private set; } //by default this is null, unless this is specified as asc or desc
    public List<DropDownItemInfoTest> Items { get; private set; } = new List<DropDownItemInfoTest>();
    
    public DropDownInfoTest(string desc) : base(desc) { //Each Info should be like Info1=1,2,3,[RInfo1],[RInfo2],...
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Dropdown Info";
      if (!IsValid) {
        CheckerResult.Message = "Invalid Syntax";
        return;
      }

      if (!HasRightSide) { //must have right side
        IsValid = false;
        CheckerResult.Message = "Dropdown Item(s) Not Found";
        return;
      }

      //Here it does not allow empty item too, try to allow! The result is good! So just allow it!
      var parts = RightSide.ParseComponentsWithEnclosurePairs(',', true, new List<KeyValuePair<char, char>> {
        new KeyValuePair<char, char>('[', ']')
      }).Select(x => x.Trim()).ToList();

      SyntaxCheckerResult orderByResult = null;
      string possibleDirectiveContent = parts.Select(x => x.GetNonEmptyTrimmedInBetween("{", "}"))
        .Where(x => !string.IsNullOrWhiteSpace(x)).FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(possibleDirectiveContent)) {//has something and valid
        orderByResult = new SyntaxCheckerResult {
          DisplayText = "Order By Directive",
          Description = possibleDirectiveContent,
        };
        if (validOrderByDirectives.Any(x => x.Equals(possibleDirectiveContent.ToUpper()))) {
          OrderByDirective = possibleDirectiveContent.ToUpper();
          orderByResult.Result = true;
        } else
          orderByResult.Result = false;
        orderByResult.Message = orderByResult.Result ? "Valid Order By Directive" : 
          "Invalid Order By Directive. Valid Order By Directives: " + string.Join(", ", validOrderByDirectives);
      }

      Items = parts.Where(x => !x.StartsWith("{") && !x.EndsWith("}")) //select whatever is not order by directive
        .Select(x => new DropDownItemInfoTest(x.Trim())).ToList();

      foreach (var item in Items)
        CheckerResult.SubResults.Add(item.CheckerResult);
      if (orderByResult != null)
        CheckerResult.SubResults.Add(orderByResult); //added after the dropdown items

      IsValid = Items != null && Items.Any();
      CheckerResult.Result = IsValid;
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}