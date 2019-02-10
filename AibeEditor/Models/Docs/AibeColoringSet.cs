using Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Aibe.Models.Docs {
  public class AibeColoringSet {
    public Color UsedColor { get; set; }
    public int StartPosition { get; set; }
    public int Length { get; set; }
    public string Text { get; set; }
    public int NextPosition { get { return StartPosition + Length; } }
    public bool CanCombine(AibeColoringSet cInfo) {
      if (cInfo == null)
        return false;
      return NextPosition == cInfo.StartPosition && UsedColor == cInfo.UsedColor;
    }

    public bool TryAbsorb(AibeColoringSet cInfo) {
      bool canCombine = CanCombine(cInfo);
      if (!canCombine)
        return false;
      Length += cInfo.Length;
      return true;
    }
  }
}
