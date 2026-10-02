using System.Text.Json;
using Shiftr.Interface;

namespace Shiftr.Middleware
{
    public sealed class InviteRegistrationValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public InviteRegistrationValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPropertyService propertyService)
        {
            var requestPath = context.Request.Path.Value?.TrimEnd('/');
            if (!HttpMethods.IsPost(context.Request.Method) ||
                !string.Equals(requestPath, "/register", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string? inviteCode;
            context.Request.EnableBuffering();
            try
            {
                using var payload = await JsonDocument.ParseAsync(
                    context.Request.Body,
                    cancellationToken: context.RequestAborted);
                inviteCode = payload.RootElement.ValueKind == JsonValueKind.Object &&
                    payload.RootElement.TryGetProperty("inviteCode", out var inviteCodeElement) &&
                    inviteCodeElement.ValueKind == JsonValueKind.String
                        ? inviteCodeElement.GetString()?.Trim()
                        : null;
            }
            catch (JsonException)
            {
                await WriteInvalidInviteResponseAsync(context);
                return;
            }
            finally
            {
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }
            }

            if (string.IsNullOrWhiteSpace(inviteCode) ||
                !await propertyService.IsInviteCodeValidAsync(inviteCode, context.RequestAborted))
            {
                await WriteInvalidInviteResponseAsync(context);
                return;
            }

            await _next(context);
        }

        private static Task WriteInvalidInviteResponseAsync(HttpContext context) =>
            Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid invite code",
                detail: "Enter an active property invite code.")
                .ExecuteAsync(context);
    }
}