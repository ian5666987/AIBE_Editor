using Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aibe.Models.Docs {
  public class AibeSection : BaseInfo {
    public int StartIndex { get; private set; }
    public int ParagraphOffset { get; private set; }
    public int Length { get; private set; }
    public string Content { get; private set; }
    public SectionType SectType { get; private set; } = SectionType.Unassigned;
    public AibeParagraph Parent { get; set; }
    public bool HasError { get; set; } //So that it can be given color
    public AibeSection(string desc, int startIndex, int paragraphOffset, AibeParagraph parent, SectionType sectType) : base(desc) {
      if (desc == null || startIndex < 0)
        return;
      Content = desc;
      StartIndex = startIndex;
      ParagraphOffset = paragraphOffset;
      Parent = parent;
      IsValid = true;
      Length = desc.Length;
      SectType = sectType;
      HasError = !Check();
    }

    public bool Check() {
      //switch (SectType) {
      //  case SectionType.
      //}
      return false;
    }

    public override string ToString() {
      return SectType.ToString() + "-" + StartIndex + "-" + 
        Length + "-" + ParagraphOffset + ": [" + Content?.ToString() + "]";
    }
  }
}
