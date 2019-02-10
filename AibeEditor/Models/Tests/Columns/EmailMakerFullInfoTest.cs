using Extension.Database.SqlServer;
using Extension.Models;
using Extension.String;
using System;
using System.Collections.Generic;
using System.Data;

namespace Aibe.Models.Tests {
  public class EmailMakerFullInfoTest {
    public EmailMakerTriggerInfoTest Trigger { get; private set; }
    public EmailMakerInfoTest Maker { get; private set; }
    public DataTable LastTriggeredTable { get; private set; }
    public bool IsValid { get; private set; }
    public EmailMakerFullInfoTest (EmailMakerTriggerInfoTest triggerInfo, EmailMakerInfoTest makerInfo) {
      if (triggerInfo == null || makerInfo == null || string.IsNullOrWhiteSpace(makerInfo.Name) || string.IsNullOrWhiteSpace(triggerInfo.Name) ||
        !makerInfo.Name.EqualsIgnoreCase(triggerInfo.Name))
        return;
      IsValid = true;
      Trigger = triggerInfo;
      Maker = makerInfo;
    }

    public List<SyntaxCheckerResult> CheckerResults { get; private set; } = new List<SyntaxCheckerResult>();
  }
}