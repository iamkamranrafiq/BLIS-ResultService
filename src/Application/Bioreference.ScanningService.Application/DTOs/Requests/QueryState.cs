using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class QueryState
{
    public int OnbaseOffset { get; set; }
    public int ScanningOffset { get; set; }
}
