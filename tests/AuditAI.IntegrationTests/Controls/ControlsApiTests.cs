using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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
    public async Task Should_ReturnBadRequest_When_OrganizationDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            OrganizationId = Guid.NewGuid(),
            DepartmentId = TestData.DepartmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("OrganizationId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_DepartmentDoesNotBelongToOrganization()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            OrganizationId = TestData.OrganizationId,
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
    public async Task Should_ReturnCreated_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();

        var createResponse = await CreateValidControlAsync("CTRL-001");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestData.OrganizationId, created.OrganizationId);
        Assert.Equal(TestData.DepartmentId, created.DepartmentId);
        Assert.Equal(ControlStatus.Active, created.Status);
    }

    [Fact]
    public async Task Should_ReturnControlById_When_ControlExists()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidControlAsync("CTRL-002");
        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();

        var getResponse = await _fixture.Client.GetAsync($"/api/controls/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var control = await getResponse.Content.ReadFromJsonAsync<ControlResponse>();
        Assert.NotNull(control);
        Assert.Equal(created.Id, control.Id);
        Assert.Equal("CTRL-002", control.Code);
    }

    [Fact]
    public async Task Should_ReturnPaginatedList_IncludingCreatedControls()
    {
        await _fixture.ResetDatabaseAsync();
        await CreateValidControlAsync("CTRL-003");
        await CreateValidControlAsync("CTRL-004");

        var response = await _fixture.Client.GetAsync("/api/controls?pageNumber=1&pageSize=10&organizationId=11111111-1111-1111-1111-111111111111");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<ControlListItemResponse>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
        Assert.Contains(page.Items, item => item.Code == "CTRL-003");
        Assert.Contains(page.Items, item => item.Code == "CTRL-004");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingControl()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PutAsJsonAsync(
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
        var createResponse = await CreateValidControlAsync("CTRL-006");
        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();

        var updateResponse = await _fixture.Client.PutAsJsonAsync(
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

        var updated = await updateResponse.Content.ReadFromJsonAsync<ControlResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated control", updated.Title);
        Assert.Equal(ControlFrequency.Yearly, updated.Frequency);
    }

    [Fact]
    public async Task Should_DeactivateControl_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidControlAsync("CTRL-007");
        var created = await createResponse.Content.ReadFromJsonAsync<ControlResponse>();

        var deactivateResponse = await _fixture.Client.PatchAsync($"/api/controls/{created!.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<ControlResponse>();
        Assert.NotNull(deactivated);
        Assert.Equal(ControlStatus.Inactive, deactivated.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ControlIsMissing()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/controls/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateValidControlAsync(string code)
    {
        return await _fixture.Client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            OrganizationId = TestData.OrganizationId,
            DepartmentId = TestData.DepartmentId,
            Code = code,
            Category = "Access Management",
            Title = $"Control {code}",
            Description = $"Detailed quarterly access review for {code}.",
            Frequency = ControlFrequency.Quarterly
        });
    }
}
