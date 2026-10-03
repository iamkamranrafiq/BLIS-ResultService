using Bioreference.ScanningService.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IScanningUserService
{
    public Task<GroupUserResponse> GetActiveUsers();
    public Task<UserGroupsRespose> GetUserGroups();

}
