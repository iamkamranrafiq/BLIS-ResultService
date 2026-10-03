using AutoMapper;

namespace Bioreference.ResultService.WebAPI.Model.Lookup.Mapper
{
    public class LookupMapper : Profile
    {
        public LookupMapper()
        {
            CreateMap<Application.Model.TestCodeGroupsModel, TestCodeGroupsModel>();


            CreateMap<Application.Model.TestCodeGroupModel, TestCodeGroupModel>()
            .ForMember(dest => dest.TestCodeList, opt => opt.MapFrom(src => src.TestCodeList.List));

            CreateMap<Application.Model.TestCodeGroupListModel, TestCodeGroupListModel>();

            CreateMap<Application.Model.TestCodeGroupItemListModel, TestCodeGroupItemListModel>();

            CreateMap<Application.Model.TestCodeGroupItemModel, TestCodeGroupItemModel>();

            CreateMap<Application.Model.CommentModel, CommentModel>()
                .ForMember(dest => dest.InternalNotes, opt => opt.MapFrom(src => src.InternalNotes));

            CreateMap<Application.Model.Lookup.InternalNote.InternalNoteModel, InternalNoteModel>();

        }

    }
}
