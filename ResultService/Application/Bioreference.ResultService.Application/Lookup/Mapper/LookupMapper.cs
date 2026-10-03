using AutoMapper;
using Bioreference.LIS;
using Bioreference.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.Common.TestMaster;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.Lookup.Mapper
{
    public class LookupMapper : Profile
    {
        public LookupMapper()
        {
            CreateMap<TestCodeGroups, TestCodeGroupsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<TestCodeGroupList, TestCodeGroupListModel>()
                .ForMember(dest => dest.TestCodeGroup, opt => opt.MapFrom(src => src.Cast<TestCodeGroup>().ToList()));

            CreateMap<TestCodeGroup, TestCodeGroupModel>()
                .ForMember(dest => dest.TestCodeList, opt => opt.MapFrom(src => src.TestCodeList));

            CreateMap<TestCodeGroupItemList, TestCodeGroupItemListModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.Cast<TestCodeGroupItem>().ToList()));

            CreateMap<TestCodeGroupItem, TestCodeGroupItemModel>();

            CreateMap<Comment, CommentModel>()
               .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AssignedID)); 

        }

    }
}
