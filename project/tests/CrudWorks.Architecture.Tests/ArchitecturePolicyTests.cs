using System.Reflection;
using System.Text;
using NetArchTest.Rules;

namespace CrudWorks.Architecture.Tests;

/// <summary>
/// Executable architecture policy for the four-project Clean Architecture boundary.
/// </summary>
/// <remarks>
/// These tests make the dependency rule from ADR-0001 and specs/constitution.md
/// (Article 2) explicit and locally verifiable. They assert type/namespace
/// policy, not arbitrary folder layout or compile-time project references.
/// </remarks>
public class ArchitecturePolicyTests
{
    private static readonly Assembly DomainAssembly = typeof(CrudWorks.Domain.Tasks.TaskItem).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CrudWorks.Application.Tasks.ITaskService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(CrudWorks.Infrastructure.Persistence.CrudWorksDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(CrudWorks.Api.Controllers.TasksController).Assembly;

    private static readonly string[] ForbiddenFrameworkNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "System.Net.Http",
        "System.Data",
        "Microsoft.Data"
    ];

    private static readonly string[] OtherCrudWorksProjects =
    [
        "CrudWorks.Application",
        "CrudWorks.Infrastructure",
        "CrudWorks.Api"
    ];

    private static readonly string[] ApplicationForbiddenProjects =
    [
        "CrudWorks.Infrastructure",
        "CrudWorks.Api"
    ];

    [Fact]
    public void Domain_Should_Not_Depend_On_Other_CrudWorks_Projects()
    {
        // AC-004-01: Domain is the innermost layer and references no other CrudWorks project.
        AssertNoDependencies(
            DomainAssembly,
            OtherCrudWorksProjects,
            "Domain depends on another CrudWorks project.");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Framework_Namespaces()
    {
        // AC-004-02: Domain must remain framework-independent.
        AssertNoDependencies(
            DomainAssembly,
            ForbiddenFrameworkNamespaces,
            "Domain depends on a forbidden framework namespace.");
    }

    [Fact]
    public void Application_Should_Depend_Only_On_Domain()
    {
        // AC-004-01: Application may depend on Domain but not Infrastructure or Api.
        AssertNoDependencies(
            ApplicationAssembly,
            ApplicationForbiddenProjects,
            "Application depends on Infrastructure or Api.");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Framework_Namespaces()
    {
        // AC-004-02: Application must remain framework-independent.
        AssertNoDependencies(
            ApplicationAssembly,
            ForbiddenFrameworkNamespaces,
            "Application depends on a forbidden framework namespace.");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        // AC-004-01: Infrastructure depends on Application (and transitively Domain);
        // it must not depend on the composition root.
        AssertNoDependencies(
            InfrastructureAssembly,
            ["CrudWorks.Api"],
            "Infrastructure depends on Api.");
    }

    [Fact]
    public void Api_Is_Composition_Root_Depending_On_Application_And_Infrastructure()
    {
        // AC-004-01: Api is the composition root and may compose Application and Infrastructure.
        // We prove the positive dependency by showing the negative rule fails.
        var applicationDependency = Types
            .InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOn("CrudWorks.Application")
            .GetResult();

        var infrastructureDependency = Types
            .InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOn("CrudWorks.Infrastructure")
            .GetResult();

        Assert.False(
            applicationDependency.IsSuccessful,
            "Api is expected to depend on CrudWorks.Application as the composition root.");
        Assert.False(
            infrastructureDependency.IsSuccessful,
            "Api is expected to depend on CrudWorks.Infrastructure as the composition root.");
    }

    private static void AssertNoDependencies(Assembly assembly, IEnumerable<string> dependencies, string reason)
    {
        var failures = new List<string>();

        foreach (var dependency in dependencies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn(dependency)
                .GetResult();

            if (!result.IsSuccessful)
            {
                failures.Add(FormatFailure(result, dependency));
            }
        }

        if (failures.Count > 0)
        {
            var message = new StringBuilder(reason);
            message.AppendLine();
            foreach (var failure in failures)
            {
                message.AppendLine(failure);
            }

            Assert.Fail(message.ToString());
        }
    }

    private static string FormatFailure(TestResult result, string dependency)
    {
        var failingTypes = result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string?>();
        var detail = string.Join(Environment.NewLine, failingTypes.Where(n => !string.IsNullOrWhiteSpace(n)));
        return $"Dependency: {dependency}{Environment.NewLine}Failing types:{Environment.NewLine}{detail}";
    }
}
