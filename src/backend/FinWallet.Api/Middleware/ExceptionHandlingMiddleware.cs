using System.Diagnostics;
using System.Text.Json;
using FinWallet.Application.Common.Exceptions;
using FluentValidation;

namespace FinWallet.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Record the exception on the current distributed trace span using BCL Activity API
            var activity = Activity.Current;
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                var tags = new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.ToString() }
                };
                activity.AddEvent(new ActivityEvent("exception", tags: tags));
            }

            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = StatusCodes.Status500InternalServerError;
        var success = false;
        var message = "An unexpected error occurred.";
        var errors = new List<ApiError>();

        switch (exception)
        {
            case FinWalletException walletEx:
                statusCode = walletEx.Code switch
                {
                    "USER_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "WALLET_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "TRANSACTION_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "RECEIPT_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
                    "FORBIDDEN" => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status400BadRequest
                };
                message = walletEx.Message;
                errors.Add(new ApiError(walletEx.Code, walletEx.Message));
                break;

            case ValidationException valEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "Validation failed.";
                foreach (var err in valEx.Errors)
                {
                    errors.Add(new ApiError("VALIDATION_ERROR", err.ErrorMessage));
                }
                break;

            default:
                message = "Internal server error.";
                errors.Add(new ApiError("INTERNAL_SERVER_ERROR", exception.Message));
                break;
        }

        context.Response.StatusCode = statusCode;

        var response = new ApiResponse(success, message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private record ApiResponse(bool Success, string Message, List<ApiError> Errors);
    private record ApiError(string Code, string Message);
}
