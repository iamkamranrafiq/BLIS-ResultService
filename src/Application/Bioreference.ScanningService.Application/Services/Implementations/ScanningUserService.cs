using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class ScanningUserService : IScanningUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScanningUserService> _logger;
    public ScanningUserService(IUnitOfWork unitOfWork, ILogger<ScanningUserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupUserResponse> GetActiveUsers()
    {
        _logger.LogInformation("GetActiveUsers called");
        try
        {

            var allUsers = await _unitOfWork.ScanningUsers.GetAllAsync(query => 
                        query
                            .Include(u => u.UserGroups)
                            .ThenInclude(ug => ug.UserGroup));

            var resp = allUsers.Select(u => new ScanningGroupUserResult
            {
                UserId = u.Id,
                UserName = u.DisplayName,

                Groups = u.UserGroups?
                .Select(ugm => new UserGroup
                {
                    GroupId = ugm.UserGroupId,
                    GroupName = ugm.UserGroup.GroupName
                })
                .ToList() ?? new List<UserGroup>()
            }).ToList();

            return new GroupUserResponse()
            {
                Success = true,
                Message = "Successfully fetch users data",
                Data = resp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users data");
            return new GroupUserResponse()
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };
        }
    }

    public async Task<UserGroupsRespose> GetUserGroups()
    {
        _logger.LogInformation("GetUserGroups called");
        try
        {
            var allGroups =  await _unitOfWork.ScanningUserGroups.GetAllAsync();

            var resp = allGroups.Select(ug => new UserGroup
            {
                GroupId = ug.Id, 
                GroupName = ug.GroupName
            }).ToList();

            return new UserGroupsRespose()
            {
                Success = true,
                Message = "Successfully fetch users data",
                Data = resp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user groups data");
            return new UserGroupsRespose()
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };
        }
    }
}
