namespace Lims.Core.Domain.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Lims.Core.Domain.Entities;
using Lims.Core.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

public static class QueryableExtensions
{
    public static IQueryable<T> IncludeObject<T>(this IQueryable<T> query)
    where T : BaseEntity
    {
        var fields = typeof(ObjMainEntity)
            .GetProperties()
            .Where(p => p.PropertyType.IsSubclassOf(typeof(BaseObjectEntity)))
            .ToList();

        fields.ForEach(p =>
        {
            query = query.Include($"Object.{p.Name}");
        });

        return query;
    }

    public static Task<bool> AnyManyAsync<TEntity, TModel>(
        this IQueryable<TEntity> entities,
        List<TModel> models,
        Expression<Func<TEntity, TModel, bool>> predicate,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
        where TModel : class
    {
        var buildedPredicate = Expression.Lambda<Func<TEntity, bool>>(
            BuildQueryExpression<TEntity, TModel>(models, predicate),
            predicate.Parameters[0]);

        return entities.AnyAsync(buildedPredicate, cancellationToken);
    }

    public static IQueryable<TEntity> WhereMany<TEntity, TModel>(
        this IQueryable<TEntity> entities,
        List<TModel> models,
        Expression<Func<TEntity, TModel, bool>> predicate)
        where TEntity : BaseEntity
        where TModel : class
    {
        var buildedPredicate = Expression.Lambda<Func<TEntity, bool>>(
            BuildQueryExpression<TEntity, TModel>(models, predicate),
            predicate.Parameters[0]);

        return entities.Where(buildedPredicate);
    }

    private static Expression BuildQueryExpression<TExpressionArgument, TReplaceableParameter>(
        List<TReplaceableParameter> request,
        Expression<Func<TExpressionArgument, TReplaceableParameter, bool>> lambdaExpression)
    {
        var expressions = new List<Expression>();
        foreach (var instance in request)
        {
            var expression = new ReplacingExpressionVisitor(
                originals: new[] { lambdaExpression.Parameters[1] },
                replacements: new[] { Expression.Constant(instance, typeof(TReplaceableParameter)) })
                .Visit(lambdaExpression.Body);
            expressions.Add(expression);
        }

        var concatExpression = ConcatExpression(ExpressionType.OrElse, expressions);
        return concatExpression;
    }

    private static Expression ConcatExpression(ExpressionType type, List<Expression> expressions)
    {
        if (expressions.Count == 0)
        {
            return Expression.Equal(Expression.Constant(0), Expression.Constant(1));
        }
        else if (expressions.Count == 1)
        {
            return expressions[0];
        }

        var center = expressions.Count / 2;
        return Expression.MakeBinary(
            type,
            ConcatExpression(type, expressions.Take(center).ToList()),
            ConcatExpression(type, expressions.Skip(center).Take(expressions.Count - center).ToList()));
    }
}
