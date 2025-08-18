using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IUnitOfWork
    {
        IMemberRepository MemberRepository { get; }
        IMessageRepository MessageRepository { get; }
        ILikesRepository likesRepository { get; }
        Task<bool> Complete();
        bool HasChanges();
    }
}