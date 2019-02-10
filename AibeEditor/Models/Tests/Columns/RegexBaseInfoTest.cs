using Extension.Models;
namespace Aibe.Models.Tests {
  public abstract class RegexBaseInfoTest : BaseInfo {
    public string Name { get; protected set; }
    public string Content { get; protected set; }
    public RegexBaseInfoTest(string desc) : base(desc) {
      SimpleExpression exp = new SimpleExpression(desc, "=");
      if (!exp.IsValid || exp.IsSingular)
        return;
      Name = exp.LeftSide;
      Content = exp.RightSide;
    }
  }
}