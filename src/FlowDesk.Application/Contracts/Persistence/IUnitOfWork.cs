using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    void VerifyMembershipOnSave(ProjectMember membership);

    Task<int> SaveChangesAsync();
}