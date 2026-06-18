using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Infrastructure.Persistence;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditAI.IntegrationTests.Controls;

[Collection(IntegrationTestCollection.Name)]
public sealed class ControlsApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public ControlsApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_CreatingControl_WithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            DepartmentId = TestData.DepartmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_ListingControls_WithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/controls?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_CreatingControl_WithValidToken()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            OrganizationId = TestData.OtherOrganizationId,
            DepartmentId = TestData.DepartmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ControlResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestData.OrganizationId, created.OrganizationId);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_DepartmentDoesNotBelongToAuthenticatedOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            DepartmentId = TestData.OtherDepartmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("DepartmentId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ListOnlyControls_FromAuthenticatedOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/controls?pageNumber=1&pageSize=10&organizationId={TestData.OtherOrganizationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<AuditAI.Application.Common.Pagination.PagedResult<ControlListItemResponse>>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == TestData.ControlId);
        Assert.DoesNotContain(page.Items, item => item.Id == TestData.OtherControlId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingControlFromAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/controls/{TestData.OtherControlId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingControl()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/controls/{Guid.NewGuid()}",
            new UpdateControlRequest
            {
                DepartmentId = TestData.DepartmentId,
                Code = "CTRL-005",
                Category = "Access Management",
                Title = "Updated control",
                Description = "Updated quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_UpdateIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var createResponse = await CreateValidControlAsync(client, "CTRL-006");
        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/controls/{created!.Id}",
            new UpdateControlRequest
            {
                DepartmentId = TestData.DepartmentId,
                Code = "CTRL-006",
                Category = "Access Management",
                Title = "Updated control",
                Description = "Updated quarterly access review.",
                Frequency = ControlFrequency.Yearly
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Should_CreateAuditLogWithAuthenticatedActor_When_DeactivatingControl()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var createResponse = await CreateValidControlAsync(client, "CTRL-007");
        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();

        var deactivateResponse = await client.PatchAsync($"/api/controls/{created!.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditAIDbContext>();
        var auditLog = await dbContext.AuditLogs
            .AsNoTracking()
            .SingleAsync(log =>
                log.EntityName == nameof(Control) &&
                log.EntityId == created.Id &&
                log.Action == AuditAI.Domain.Enums.AuditLogAction.ControlDeactivated);

        Assert.Equal(TestData.UserId, auditLog.UserId);
        Assert.Equal(TestData.OrganizationId, auditLog.OrganizationId);
    }

    [Fact]
    public async Task Should_KeepNonControlEndpointAnonymous_ForNow()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/audit-logs?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> CreateValidControlAsync(HttpClient client, string code)
    {
        return client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            DepartmentId = TestData.DepartmentId,
            Code = code,
            Category = "Access Management",
            Title = $"Control {code}",
            Description = $"Detailed quarterly access review for {code}.",
            Frequency = ControlFrequency.Quarterly
        });
    }
}
