using Bl.QueryVisitor.MySql.Extension;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;

namespace Bl.QueryVisitor.MySql.Api.Test.Filter;

public class ODataCountableFilter : EnableQueryAttribute
{
    public ODataCountableFilter() : base()
    {

    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var isCountRequest = request.QueryString.HasValue &&
            request.QueryString.Value.Contains("count=true", StringComparison.OrdinalIgnoreCase);
        if (isCountRequest && context.Result is ObjectResult objectResult && objectResult.Value is IQueryable queryable)
        {
            var total = await queryable.SqlLongCountAsync(context.HttpContext.RequestAborted);
            context.Result = new OkObjectResult(total);
            await next();
            return;
        }

        await base.OnResultExecutionAsync(context, next); // query executed here
    }
}
