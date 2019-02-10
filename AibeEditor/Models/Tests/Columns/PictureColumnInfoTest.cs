using Extension.Models;
using Extension.String;
using System.Collections.Generic;

namespace Aibe.Models.Tests {
  public class PictureColumnInfoTest : CommonBaseInfo {
    private const string skip = Aibe.DH.Skip;
    public const int DefaultWidth = 100;
    public const int DefaultHeight = 100;
    public bool HeightComesFirst { get; private set; } //by default, false. Width comes first.
    public bool IsStretched { get; private set; } //by default false
    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;
    public bool IndexHeightComesFirst { get; private set; } //by default, false. Width comes first.
    public bool IndexIsStretched { get; private set; } //by default false
    public int IndexWidth { get; private set; } = DefaultWidth;
    public int IndexHeight { get; private set; } = DefaultHeight;

    //ColumnName1=picWidth1,picHeight1|indexPicWidth1,indexPicHeight1;
    public PictureColumnInfoTest(string desc) : base(desc) { //now it looks like ColName=100,50|60,70
      CheckerResult.Description = desc;
      CheckerResult.DisplayText = "Picture Column";
      if (HasRightSide) {
        SyntaxCheckerResult subResult = new SyntaxCheckerResult {
          DisplayText = "Dimension Description",
          Description = RightSide,
        };
        CheckerResult.SubResults.Add(subResult);
        SimpleExpression mainExp = new SimpleExpression(RightSide, "|"); //To separate between the index expression from the rests
        if (!mainExp.IsValid) {
          subResult.Message = "Invalid Dimension Description";
          return;
        }
        subResult.Result = true;

        SyntaxCheckerResult subSubResult = new SyntaxCheckerResult {
          DisplayText = "Dimension Sub-Description",
          Description = mainExp.LeftSide,
        };
        subResult.SubResults.Add(subSubResult);

        int width, height;
        bool widthResult, heightResult;
        SimpleExpression leftExp = new SimpleExpression(mainExp.LeftSide, ",");
        if (leftExp.IsValid) {
          HeightComesFirst = leftExp.LeftSide.EqualsIgnoreCase(skip); //can only happen if width is skipped
          SyntaxCheckerResult leftSubSubResult = new SyntaxCheckerResult {
            Description = leftExp.LeftSide,
            DisplayText = "Width-Dimension",
          };
          subSubResult.SubResults.Add(leftSubSubResult);
          if (HeightComesFirst) {
            leftSubSubResult.Result = true;
            leftSubSubResult.Message = "Valid Keyword [" + skip + "]";
            subResult.Result = true;
          }

          widthResult = int.TryParse(leftExp.LeftSide, out width); //whatever is this, try to parse
          if (widthResult && width > 0) { //if the parsing is successful and the value is positive, only then we can take it
            leftSubSubResult.Message = "Valid Width";
            leftSubSubResult.Result = true;
            subSubResult.Result = true;
            subSubResult.Message = "Valid Dimension Description";
            Width = width;
          } else if (!HeightComesFirst) {
            leftSubSubResult.Message = "Invalid Item";
            subSubResult.Message = "Invalid Dimension Description";
          }

          if (!leftExp.IsSingular) {
            heightResult = int.TryParse(leftExp.RightSide, out height);
            SyntaxCheckerResult rightSubSubResult = new SyntaxCheckerResult {
              Description = leftExp.RightSide,
              DisplayText = "Height-Dimension",
            };
            subSubResult.SubResults.Add(rightSubSubResult);
            if (heightResult && height > 0) {
              Height = height;
              rightSubSubResult.Message = "Valid Height";
              rightSubSubResult.Result = true;
            } else {
              rightSubSubResult.Message = "Invalid Height Dimension Description";
            }
            IsStretched = widthResult && heightResult; //only if both are specified correctly then IsStretched can be true
          }
        } else {
          subSubResult.Message = "Invalid Dimension Sub-Description";
        }
        
        if (mainExp.IsSingular) { //just copy the left exp to the right if singular
          IndexHeightComesFirst = HeightComesFirst;
          IndexIsStretched = IsStretched;
          IndexWidth = Width;
          IndexHeight = Height;
          return;
        } //else, start to make rules for the index page

        subSubResult = new SyntaxCheckerResult {
          DisplayText = "Dimension Sub-Description (Index)",
          Description = mainExp.RightSide,
        };
        subResult.SubResults.Add(subSubResult);

        SimpleExpression rightExp = new SimpleExpression(mainExp.RightSide, ",");
        if (!rightExp.IsValid) {
          subSubResult.Message = "Invalid Dimension Sub-Description (Index)";
          return;
        }

        IndexHeightComesFirst = rightExp.LeftSide.EqualsIgnoreCase(skip);
        bool isSkipped = IndexHeightComesFirst;
        SyntaxCheckerResult leftRightSubResult = new SyntaxCheckerResult {
          Description = rightExp.LeftSide,
          DisplayText = "Width-Dimension (Index)",
        };
        subSubResult.SubResults.Add(leftRightSubResult);
        if (IndexHeightComesFirst) {
          leftRightSubResult.Result = true;
          leftRightSubResult.Message = "Valid Keyword [" + skip + "]";
          subResult.Result = true;
        }

        widthResult = int.TryParse(rightExp.LeftSide, out width); //whatever is this, try to parse
        if (widthResult && width > 0) { //if the parsing is successful and the value is positive, only then we can take it
          leftRightSubResult.Message = "Valid Width";
          leftRightSubResult.Result = true;
          subSubResult.Result = true;
          subSubResult.Message = "Valid Dimension Description";
          IndexWidth = width;
        } else if (!IndexHeightComesFirst) {
          leftRightSubResult.Message = "Invalid Item";
          subSubResult.Message = "Invalid Dimension Description";
        }

        if (rightExp.IsSingular)
          return;

        SyntaxCheckerResult rightRightSubResult = new SyntaxCheckerResult {
          Description = rightExp.RightSide,
          DisplayText = "Height-Dimension (Index)",
        };
        subSubResult.SubResults.Add(rightRightSubResult);

        heightResult = int.TryParse(rightExp.RightSide, out height);
        if (heightResult && height > 0) {
          IndexHeight = height;
          rightRightSubResult.Message = "Valid Height";
          rightRightSubResult.Result = true;
        } else {
          rightRightSubResult.Message = "Invalid Height Dimension Description";
        }
        IndexIsStretched = widthResult && heightResult; //only if both are specified correctly then IsStretched can be true
      }
    }

    public SyntaxCheckerResult CheckerResult { get; private set; } = new SyntaxCheckerResult();
  }
}