public static class ExpressionCombiner
{
    public static Expression<Func<T, bool>> CombineModelGroupsBalanced<T, TModel>(
        List<List<TModel>> modelGroups,
        ExpressionType outerOperator,
        ExpressionType innerOperator,
        Expression<Func<T, TModel, bool>> predicate)
    {
        if (modelGroups == null || modelGroups.Count == 0)
            return x => false;

        var param = predicate.Parameters[0];

        var groupExprs = modelGroups
            .Where(g => g?.Count > 0)
            .Select(group =>
            {
                var exprs = group.Select(model =>
                {
                    var replaced = new ReplacingExpressionVisitor(
                        new[] { predicate.Parameters[1] },
                        new[] { Expression.Constant(model, typeof(TModel)) })
                        .Visit(predicate.Body);

                    return replaced!;
                }).ToList();

                return CombineBalanced(innerOperator, exprs);
            })
            .ToList();

        if (groupExprs.Count == 0)
            return x => false;

        var body = CombineBalanced(outerOperator, groupExprs);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private static Expression CombineBalanced(ExpressionType op, List<Expression> expressions)
    {
        if (expressions.Count == 1)
            return expressions[0];

        int mid = expressions.Count / 2;
        return Expression.MakeBinary(
            op,
            CombineBalanced(op, expressions.Take(mid).ToList()),
            CombineBalanced(op, expressions.Skip(mid).ToList()));
    }
}
