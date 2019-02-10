using Extension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aibe.Models.Docs {
  public class AibeParagraphState {
    public List<AibeSection> Sections { get; set; } = new List<AibeSection>();
    public int CurrentOffset { get; set; }
  }
}
