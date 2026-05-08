using EdSkill.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdSkill.Application.Features.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand
    (
        string Email
    ) : IRequest<Result>;
}
