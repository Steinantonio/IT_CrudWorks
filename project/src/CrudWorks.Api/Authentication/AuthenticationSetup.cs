using CrudWorks.Api.ErrorHandling;
using CrudWorks.Application.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CrudWorks.Api.Authentication;

internal static class AuthenticationSetup
{
    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>((bearerOptions, jwtOptions) =>
            {
                // Keep the raw "sub" claim instead of the legacy soap mapping.
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(jwtOptions.SigningKey!)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                bearerOptions.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        const int status = StatusCodes.Status401Unauthorized;
                        context.Response.StatusCode = status;
                        await context.Response.WriteAsJsonAsync(
                            ApiProblemDetails.Create(context.HttpContext, status, "Authentication required"),
                            options: null,
                            contentType: ApiProblemDetails.ContentType,
                            cancellationToken: context.HttpContext.RequestAborted);
                    },
                };
            });

        return services;
    }
}
