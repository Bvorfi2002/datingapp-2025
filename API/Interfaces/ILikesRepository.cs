using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface ILikesRepository
    {
        Task<MemberLike?> GetMemberLike(string sourceMemberId, string targetMemberId);
        Task<PaginatedResult<Member>> GetMembersLikes(LikesParams likesParams);
        Task<IReadOnlyList<string>> GetCurrentMemberLikesIds(string memberId);
        void DeleteLike(MemberLike like);
        void AddLike(MemberLike like);
        Task<bool> SaveAllChanges();
    }
}