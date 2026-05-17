using System.Reflection;
using EdSkill.Application.Common.Behaviors;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EdSkill.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IPolicyConsentService, PolicyConsentService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IPointLedgerService, PointLedgerService>();
        services.AddScoped<ISessionPricingService, SessionPricingService>();
        services.AddScoped<ITokenLedgerService, TokenLedgerService>();
        services.AddScoped<IAchievementAwardService, AchievementAwardService>();
        services.AddScoped<IWalletPaymentProcessingService, WalletPaymentProcessingService>();

        // Add validation behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
