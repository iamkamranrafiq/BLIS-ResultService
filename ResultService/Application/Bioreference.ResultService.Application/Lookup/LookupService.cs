using Bioreference.Common.TestMaster;
using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.UI;
using Comment = Bioreference.Common.TestMaster.Comment;
using Bioreference.ResultService.Abstractions.Application.Brad;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Lookup.InternalNote;
using Bioreference.ResultService.Application.Model.Enum;

namespace Bioreference.ResultService.Application.Lookup
{
 
    public class LookupService : ILookupService
    {
        private readonly IMapper _mapper;
        private readonly IBradService _bradService;

        public LookupService(IMapper mapper, IBradService bradService = null)
        {
            _mapper = mapper;
            _bradService = bradService;
        }
        public async Task<TestCodeGroupsModel> GetTestCodeGroups(bool forSecurity = false, bool forGrouping = false)
        {
            TestCodeGroups testCodeGroups = await Task.Run(() => TestCodeGroups.Fetch(forSecurity, forGrouping));
            TestCodeGroupsModel mappedTestCodeGroups = _mapper.Map<TestCodeGroupsModel>(testCodeGroups);
            return mappedTestCodeGroups;
           
        }


        //public async Task<List<CommentModel>> GetComments(CommentType commentType)
        //{
        //    Bioreference.Common.TestMaster.Comments commentList = await Task.Run(() => Bioreference.Common.TestMaster.Comments.Fetch(commentType));
        //    var internalNoteList = new List<InternalNoteModel>();

        //    if (commentList.List != null && commentList.List.Count > 0)
        //    {
        //        foreach (Comment comment in commentList.List)
        //        {
        //            foreach (var c in comment.InternalNotes.List)
        //            {
        //                internalNoteList.Add(new InternalNoteModel
        //                {
        //                    Note = c.InternalNote,
        //                    Id = c.Id
        //                });
        //            }
        //        }
        //    }
        //    var comments = _mapper.Map<List<CommentModel>>(commentList.List);
        //    return comments;
        //}
        public async Task<List<CommentModel>> GetComments(CommentTypeModel commentType)
        {
            Bioreference.Common.TestMaster.Comments commentList = await Task.Run(() =>
                Bioreference.Common.TestMaster.Comments.Fetch((CommentType)commentType));

            var comments = new List<CommentModel>();

            if (commentList.List != null && commentList.List.Count > 0)
            {
                foreach (Comment comment in commentList.List)
                {
                    var commentModel = new CommentModel
                    {
                        Id = comment.AssignedID,
                        Text = comment.Text,
                        InternalNotes = new List<InternalNoteModel>()
                    };

                    if (comment.InternalNotes != null && comment.InternalNotes.List != null && comment.InternalNotes.List.Count() > 0)
                    {
                        foreach (var internalNote in comment.InternalNotes.List)
                        {
                            var noteModel = new InternalNoteModel
                            {
                                Id = internalNote.Id,
                                Note = internalNote.InternalNote
                            };

                            commentModel.InternalNotes.Add(noteModel);
                        }
                    }

                    comments.Add(commentModel);
                }
            }

            return comments;
        }

        public async Task<Dictionary<int,string>> GetDivisions()
        {
            Dictionary<int, string> response = await Task.Run(() => OrderManager.Divisions);
            return response;
        }

        public async Task<List<Model.Lookup.PendingList>> GetPendingList()
        {
            List<Model.Lookup.PendingList> pendingLists = new List<Model.Lookup.PendingList>();
            PendingListsLite response = await Task.Run(() => PendingListsLite.Fetch());

            if (response != null)
            {
                foreach (var pending in response.List)
                {
                    Model.Lookup.PendingList pendingItem = new Model.Lookup.PendingList();
                    pendingItem.Id = pending.PendingListId;
                    pendingItem.Name = pending.PendingListName;
                    pendingLists.Add(pendingItem);
                }
            
            }

            return pendingLists;
        }

        public async Task<List<PendingListItemLite>> GetPendingListItems(int id)
        {
            var response =  await Task.Run(() => PendingListsLite.Fetch(id));
            return response.List.Select(x=>x.PendingListItemsLite).First().ToList();
        }
    }
}
