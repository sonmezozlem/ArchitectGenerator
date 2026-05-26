namespace ArchitectGenerator.Templates;

public static class ModuleTemplates
{
	public static string ApplicationConfigureServices(string moduleName) =>
$$"""
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace {{moduleName}}.Application;

public static class ConfigureServices
{
    public static IServiceCollection Add{{moduleName}}Application(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly);
        });

        return services;
    }
}
""";

	public static string InfrastructureConfigureServices(string moduleName) =>
$$"""
using Microsoft.Extensions.DependencyInjection;

namespace {{moduleName}}.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection Add{{moduleName}}Infrastructure(this IServiceCollection services)
    {
        // Modüle özel altyapı servislerini buraya kaydedin
        return services;
    }
}
""";

	public static string PersistenceConfigureServices(string moduleName) =>
$$"""
using Base.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace {{moduleName}}.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection Add{{moduleName}}Persistence(this IServiceCollection services)
    {
        services.AddSingleton<IModelConfigurator, {{moduleName}}ModelConfigurator>();
        return services;
    }
}
""";

	public static string ModelConfigurator(string moduleName) =>
$$"""
using Base.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace {{moduleName}}.Persistence;

public class {{moduleName}}ModelConfigurator : IModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
""";
}
