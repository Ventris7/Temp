namespace Lims.Core.UseCases.Process.Orders.Commands.RegistrateOrders.Checks;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lims.Core.Domain.Abstract;
using Lims.Core.Domain.Base.Extensions;
using Lims.Core.Domain.Check;
using Lims.Core.Domain.Check.Abstract;
using Lims.Core.Domain.Extensions;
using Microsoft.EntityFrameworkCore;

public class TargetBranchIdFlowIdObjectIdStageIdUniqueCheck : BaseCheck<RegistrateOrdersCommand>
{
    private readonly ILimsDbContext limsDbContext;

    public TargetBranchIdFlowIdObjectIdStageIdUniqueCheck(ILimsDbContext limsDbContext)
    {
        this.limsDbContext = limsDbContext;
    }

    public override async Task<CheckResult> CheckAsync(RegistrateOrdersCommand request, CancellationToken cancellationToken)
    {
        if (!await this.limsDbContext.PrcsOrders
            .AnyManyAsync(
                request.Models,
                (e, m) =>
                    e.TargetBranchId == m.TargetBranchId
                    && e.FlowId == m.FlowId
                    && e.ObjectId == m.ObjectId
                    && e.StageId == m.StageId,
                cancellationToken))
        {
            return new CheckResult();
        }

        var failures = await this.limsDbContext.PrcsOrders
            .AsNoTracking()
            .WhereMany(
                request.Models,
                (e, m) =>
                    e.TargetBranchId == m.TargetBranchId
                    && e.FlowId == m.FlowId
                    && e.ObjectId == m.ObjectId
                    && e.StageId == m.StageId)
            .Include(e => e.TargetBranch)
            .Include(e => e.Flow)
            .IncludeObject()
            .Include(e => e.Stage)
            .Select(e => new CheckFailure(
                string.Empty,
                string.Concat(
                    $"Невозможно создать заявку для объекта '{e.Object.Kind.GetEnumDisplayName()}'",
                    $": {e.Object.GetCaption(false)}",
                    $" на этап '{e.Stage.Name}'",
                    $" в ветке '{e.TargetBranch.Name}'",
                    $", потоке '{e.Flow.Type} - {e.Flow.Year}'.",
                    " Такая заявка уже существует. "),
                request))
            .ToListAsync(cancellationToken);

        return new CheckResult(failures);
    }
}
