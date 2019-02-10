using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aibe.Models.Docs {
  public class AibeStreamProcessResult {
    public List<AibeSection> Sections { get; set; } = new List<AibeSection>();
    public int FinalIndex { get; set; }
    public AibeSection MainSection { get; set; }
  }
}
