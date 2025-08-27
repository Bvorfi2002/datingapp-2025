using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<bool> IsEmailValidAsync(string email);
    }
}