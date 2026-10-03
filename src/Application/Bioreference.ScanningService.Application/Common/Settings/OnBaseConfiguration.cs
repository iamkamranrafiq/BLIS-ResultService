using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.Common.Settings;

public class OnBaseConfiguration
{
    public const string DataSource = "OnBase";
    public DateOnly? CutoverDate { get; set; }
}
