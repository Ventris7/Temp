public override async Task<CheckResult> CheckAsync(
    RegistrateOrdersCommand request,
    CancellationToken cancellationToken)
{
    var requestModels = request.Models.ToList();

    if (!requestModels.Any())
    {
        return new CheckResult(Array.Empty<CheckFailure>());
    }

    var objectIds = requestModels
        .Select(m => m.ObjectId)
        .Distinct()
        .ToArray();

    var objects = await this.limsDbContext.ObjMains
        .AsNoTracking()
        .Where(o => objectIds.Contains(o.Id))
        .Select(o => new
        {
            o.Id,
            o.Kind,
        })
        .ToListAsync(cancellationToken);

    var kindByObjectId = objects.ToDictionary(o => o.Id, o => o.Kind);

    var requestedPairs = requestModels
        .Where(m => kindByObjectId.ContainsKey(m.ObjectId))
        .Select(m => new
        {
            m.StageId,
            Kind = kindByObjectId[m.ObjectId],
        })
        .DistinctBy(m => new { m.StageId, m.Kind })
        .ToList();

    if (!requestedPairs.Any())
    {
        return new CheckResult(Array.Empty<CheckFailure>());
    }

    var stageIds = requestedPairs
        .Select(m => m.StageId)
        .Distinct()
        .ToArray();

    var kinds = requestedPairs
        .Select(m => m.Kind)
        .Distinct()
        .ToArray();

    var existingPermissions = await this.limsDbContext.PrcsRulePermissions
        .AsNoTracking()
        .Where(p =>
            stageIds.Contains(p.StageId) &&
            kinds.Contains(p.Kind))
        .Select(p => new
        {
            p.StageId,
            p.Kind,
        })
        .ToListAsync(cancellationToken);

    var existingPairs = existingPermissions
        .Select(p => (p.StageId, p.Kind))
        .ToHashSet();

    var missingPairs = requestedPairs
        .Where(m => !existingPairs.Contains((m.StageId, m.Kind)))
        .ToList();

    if (!missingPairs.Any())
    {
        return new CheckResult(Array.Empty<CheckFailure>());
    }

    var missingStageIds = missingPairs
        .Select(m => m.StageId)
        .Distinct()
        .ToArray();

    var stageNames = await this.limsDbContext.PrcsStages
        .AsNoTracking()
        .Where(s => missingStageIds.Contains(s.Id))
        .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

    var failures = missingPairs
        .Select(m =>
        {
            var stageName = stageNames.TryGetValue(m.StageId, out var name)
                ? name
                : $"Stage #{m.StageId}";

            var kindName = m.Kind.GetEnumDisplayName();

            return new CheckFailure(
                PropTypes.Body,
                nameof(request.Models),
                $"Для объекта типа '{kindName}' нельзя назначить этап '{stageName}'.");
        })
        .ToList();

    return new CheckResult(failures);
}
