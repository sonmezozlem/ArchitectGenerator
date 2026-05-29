using ArchitectGenerator.Core;

namespace ArchitectGenerator.Templates;

public static partial class BaseTemplates
{
	// ======================= Common / Exceptions =======================

	public static string InvalidCredentialsException() =>
"""
namespace Base.Application.Common.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Geçersiz kullanıcı adı veya şifre.")
    {
    }
}
""";

	public static string ConflictException() =>
"""
namespace Base.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
""";

	public static string ForbiddenException() =>
"""
namespace Base.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bu işleme erişim izniniz yok.")
        : base(message)
    {
    }
}
""";

	// ======================= Common / Paging =======================

	public static string PagedRequest() =>
"""
namespace Base.Application.Common.Models;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
""";

	public static string PagedResult() =>
"""
namespace Base.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
""";

	public static string ExpressionExtensions() =>
"""
using System.Linq.Expressions;

namespace Base.Application.Common.Extensions;

// Koşullu predicate kurmak için: filter = filter.And(x => ...)
// NOT: Expression.Invoke yerine parametre yeniden bağlama (ExpressionVisitor) kullanılır;
// böylece EF Core sorguyu SQL'e çevirebilir (Invoke tabanlı sürüm çeviri hatası verir).
public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.OrElse);

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        => Expression.Lambda<Func<T, bool>>(Expression.Not(expr.Body), expr.Parameters[0]);

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(merge(leftBody!, rightBody!), parameter);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
""";

}
