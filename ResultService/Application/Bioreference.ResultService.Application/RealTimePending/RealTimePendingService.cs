using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.RealTimePending;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.RealTimePending
{
    public class RealTimePendingService : IRealTimePendingService
    {
        private readonly IMapper _mapper;
        public RealTimePendingService(IMapper mapper)
        {
            _mapper = mapper;
        }
        public async Task<Pendings> RealTimePendingFetch(RTPSearchCriteria searchCriteriaDTO)
        {
            if (searchCriteriaDTO == null)
                throw new ArgumentNullException(nameof(searchCriteriaDTO));

            var pendingListsLite = await Task.Run(() => PendingListsLite.Fetch(searchCriteriaDTO.PendingListId));

            var selectedPendingListLite = pendingListsLite?.List
                ?.FirstOrDefault(pendingList => pendingList.PendingListId == searchCriteriaDTO.PendingListId);

            DetermineStartEnd(searchCriteriaDTO);

            return await Task.Run(() => Pendings.Fetch(searchCriteriaDTO.StartDate, searchCriteriaDTO.EndDate, selectedPendingListLite));
        }


        public void DetermineStartEnd(RTPSearchCriteria searchCriteriaDTO)
        {
            if (searchCriteriaDTO.RdoEnableDate)
            {
                searchCriteriaDTO.StartDate = DateTime.Parse($"{searchCriteriaDTO.StartDate.ToShortDateString()} {searchCriteriaDTO.StartDate.ToShortTimeString()}");
                searchCriteriaDTO.EndDate = DateTime.Parse($"{searchCriteriaDTO.EndDate.ToShortDateString()} {searchCriteriaDTO.EndDate.ToShortTimeString()}");
            }
            else
            {
                searchCriteriaDTO.StartDate = DateTime.Now.AddHours(-searchCriteriaDTO.LastHours);
                searchCriteriaDTO.EndDate = DateTime.Now.AddHours(-searchCriteriaDTO.ExcludeHours);
            }
        }
    }
}
