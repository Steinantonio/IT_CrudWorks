using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CrudWorks.Application.Auth;
using CrudWorks.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CrudWorks.Api.Tests.Tasks;

public sealed class TasksApiTests(CrudWorksApiFactory factory) : IClassFixture<CrudWorksApiFactory>
{
    [Fact]
    public async Task Create_WithValidPayload_Returns201WithLocationAndTaskResource()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Write the proposal",
            description = "Draft and review the proposal.",
            dueDate = "2026-11-14",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(task);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Write the proposal", task.Title);
        Assert.Equal("Draft and review the proposal.", task.Description);
        Assert.Equal("Todo", task.Status);
        Assert.Equal(new DateOnly(2026, 11, 14), task.DueDate);
        Assert.True(task.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(task.CreatedAtUtc, task.UpdatedAtUtc);
    }

    [Fact]
    public async Task Create_SerializesStatusAndDueDateExactlyAsTheContractPins()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Write the proposal",
            status = "InProgress",
            dueDate = "2026-11-14",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"InProgress\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"dueDate\":\"2026-11-14\"", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status\":1", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-11-14T00:00:00", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallerTasksOrderedByCreatedAtUtcDescending()
    {
        var client = await CreateAuthenticatedClientAsync();
        var otherClient = await CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync("/api/tasks", new { title = "Older task" });
        await Task.Delay(20);
        await client.PostAsJsonAsync("/api/tasks", new { title = "Newer task" });
        await otherClient.PostAsJsonAsync("/api/tasks", new { title = "Foreign task" });

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<TaskPageResource>();
        Assert.NotNull(page);
        Assert.Equal(["Newer task", "Older task"], page.Items.Select(task => task.Title));
        Assert.All(page.Items, task => Assert.NotEqual("Foreign task", task.Title));
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task List_ReturnsEmptyEnvelopeForANewUser()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<TaskPageResource>();
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task List_WithTwelveTasks_ReturnsTenAndTheNextPage()
    {
        var client = await CreateAuthenticatedClientAsync();
        for (var index = 1; index <= 12; index++)
        {
            await CreateTaskAsync(client, $"Task {index}");
            await Task.Delay(20);
        }

        var first = await client.GetFromJsonAsync<TaskPageResource>("/api/tasks");

        Assert.NotNull(first);
        Assert.Equal(10, first.Items.Count);
        Assert.Equal(
            ["Task 12", "Task 11", "Task 10", "Task 9", "Task 8",
             "Task 7", "Task 6", "Task 5", "Task 4", "Task 3"],
            first.Items.Select(task => task.Title));
        Assert.NotNull(first.NextCursor);

        var second = await client.GetFromJsonAsync<TaskPageResource>(
            $"/api/tasks?cursor={Uri.EscapeDataString(first.NextCursor!)}");

        Assert.NotNull(second);
        Assert.Equal(["Task 2", "Task 1"], second.Items.Select(task => task.Title));
        Assert.Null(second.NextCursor);
        Assert.Empty(first.Items.Select(task => task.Id)
            .Intersect(second.Items.Select(task => task.Id)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    public async Task List_WithAnOutOfRangeTake_Returns400WithTheTakeKey(string take)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tasks?take={take}");

        var problem = await ReadErrorsAsync(response);
        Assert.True(problem.Errors.ContainsKey("take"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task List_WithAnInRangeTake_Returns200(int take)
    {
        var client = await CreateAuthenticatedClientAsync();
        await CreateTaskAsync(client, "One task");

        var page = await client.GetFromJsonAsync<TaskPageResource>($"/api/tasks?take={take}");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task List_WithANonIntegerTake_Returns400WithTheTakeKey()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tasks?take=abc");

        var problem = await ReadErrorsAsync(response);
        Assert.True(problem.Errors.ContainsKey("take"));
    }

    [Fact]
    public async Task List_WithAnInvalidCursor_Returns400WithTheCursorKey()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tasks?cursor=not-a-token");

        var problem = await ReadErrorsAsync(response);
        Assert.True(problem.Errors.ContainsKey("cursor"));
    }

    [Fact]
    public async Task List_WithAnUnsupportedCursorVersion_Returns400WithTheCursorKey()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            v = 2,
            c = DateTime.UtcNow,
            i = Guid.NewGuid(),
        });
        var cursor = WebEncoders.Base64UrlEncode(payload);

        var response = await client.GetAsync($"/api/tasks?cursor={Uri.EscapeDataString(cursor)}");

        var problem = await ReadErrorsAsync(response);
        Assert.True(problem.Errors.ContainsKey("cursor"));
    }

    [Fact]
    public async Task List_WithMalformedCursorPayloads_Returns400WithTheCursorKey()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payloads = new object[]
        {
            new { v = 1, c = DateTime.UtcNow }, // missing id
            new { v = 1, c = DateTime.UtcNow, i = Guid.Empty }, // empty id
            new { v = 1, c = DateTime.UtcNow, i = "not-a-guid" }, // invalid id
            new { v = 1, i = Guid.NewGuid() }, // missing date
            new { v = 1, c = "not-a-date", i = Guid.NewGuid() }, // invalid date
            new { v = 1, c = "2026-11-11T18:30:00", i = Guid.NewGuid() }, // non-UTC date
        };

        foreach (var payload in payloads)
        {
            var cursor = EncodeCursor(payload);
            var response = await client.GetAsync(
                $"/api/tasks?cursor={Uri.EscapeDataString(cursor)}");

            var problem = await ReadErrorsAsync(response);
            Assert.True(problem.Errors.ContainsKey("cursor"));
        }
    }

    [Fact]
    public async Task List_WithACursorFromAnotherUser_DoesNotLeakTasks()
    {
        var caller = await CreateAuthenticatedClientAsync();
        var other = await CreateAuthenticatedClientAsync();
        for (var index = 1; index <= 12; index++)
        {
            await CreateTaskAsync(other, $"Other {index}");
            await Task.Delay(20);
        }

        await CreateTaskAsync(caller, "Caller task");
        var otherPage = await other.GetFromJsonAsync<TaskPageResource>("/api/tasks");
        Assert.NotNull(otherPage);
        Assert.NotNull(otherPage.NextCursor);

        var callerPage = await caller.GetFromJsonAsync<TaskPageResource>(
            $"/api/tasks?cursor={Uri.EscapeDataString(otherPage.NextCursor!)}");

        Assert.NotNull(callerPage);
        Assert.DoesNotContain(callerPage.Items, task => task.Title.StartsWith("Other ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_WithAnOwnedTask_Returns200WithTheTask()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Fetch me", status: "Done");

        var response = await client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(task);
        Assert.Equal(created.Id, task.Id);
        Assert.Equal("Fetch me", task.Title);
        Assert.Equal("Done", task.Status);
        Assert.Null(task.Description);
        Assert.Null(task.DueDate);
        // ADR-0004 §1: nulls are serialized as null, never omitted.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"description\":null", raw, StringComparison.Ordinal);
        Assert.Contains("\"dueDate\":null", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_ReplacesEditableFieldsAndKeepsOwnerAndCreatedAtUtc()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Write the proposal", description: "First draft.");

        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", new
        {
            title = "Review the proposal",
            description = (string?)null,
            status = "Done",
            dueDate = "2026-11-20",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var task = await response.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(task);
        Assert.Equal(created.Id, task.Id);
        Assert.Equal("Review the proposal", task.Title);
        Assert.Null(task.Description);
        Assert.Equal("Done", task.Status);
        Assert.Equal(new DateOnly(2026, 11, 20), task.DueDate);
        // The owner is server-owned: a userId field never appears in the response.
        Assert.DoesNotContain("userId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(created.CreatedAtUtc, task.CreatedAtUtc);
        Assert.True(task.UpdatedAtUtc >= task.CreatedAtUtc);
        Assert.Equal(task.CreatedAtUtc, created.CreatedAtUtc);
    }

    [Fact]
    public async Task Create_WithNumericStatus_Returns400WithTheStatusKey()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Enum.TryParse-style parsing maps "1" to InProgress; ADR-0004 §4 pins
        // the three names case-sensitively, so a numeric string must be a 400.
        using var content = new StringContent(
            """{"title":"Write the proposal","status":"1"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/tasks", content);

        var problem = await ReadErrorsAsync(response);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], problem.Errors["status"]);
    }

    [Fact]
    public async Task Create_WithLowercaseStatus_Returns400WithTheStatusKey()
    {
        var client = await CreateAuthenticatedClientAsync();

        using var content = new StringContent(
            """{"title":"Write the proposal","status":"todo"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/tasks", content);

        var problem = await ReadErrorsAsync(response);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], problem.Errors["status"]);
    }

    [Fact]
    public async Task Create_WithAUserIdInTheBody_IsIgnoredAndTheCallerRemainsTheOwner()
    {
        var caller = await CreateAuthenticatedClientAsync();
        var other = await CreateAuthenticatedClientAsync();
        var otherUser = await other.GetFromJsonAsync<MeResource>("/api/auth/me");
        Assert.NotNull(otherUser);

        var response = await caller.PostAsJsonAsync("/api/tasks", new
        {
            title = "Caller-owned task",
            userId = otherUser.Id,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var callerPage = await caller.GetFromJsonAsync<TaskPageResource>("/api/tasks");
        Assert.NotNull(callerPage);
        Assert.Contains(callerPage.Items, task => task.Title == "Caller-owned task");

        var otherPage = await other.GetFromJsonAsync<TaskPageResource>("/api/tasks");
        Assert.NotNull(otherPage);
        Assert.DoesNotContain(otherPage.Items, task => task.Title == "Caller-owned task");
    }

    [Fact]
    public async Task Update_WithANullDueDate_ClearsAPreviouslySetDueDate()
    {
        var client = await CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Write the proposal",
            dueDate = "2026-11-14",
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", new
        {
            title = "Write the proposal",
            status = "Todo",
            dueDate = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(task);
        Assert.Null(task.DueDate);
        Assert.Equal("Write the proposal", task.Title);
        Assert.Equal("Todo", task.Status);
        // ADR-0004 §1: nulls are serialized as null, never omitted.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"dueDate\":null", raw, StringComparison.Ordinal);

        var fetched = await client.GetFromJsonAsync<TaskResource>($"/api/tasks/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Null(fetched.DueDate);
        Assert.Equal("Write the proposal", fetched.Title);
        Assert.Equal("Todo", fetched.Status);
    }

    [Fact]
    public async Task Delete_Returns204AndAGetReturns404Afterwards()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Doomed task");

        var delete = await client.DeleteAsync($"/api/tasks/{created.Id}");
        var get = await client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(string.Empty, await delete.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/00000000-0000-0000-0000-000000000000")]
    [InlineData("DELETE", "/api/tasks/00000000-0000-0000-0000-000000000000")]
    public async Task EveryTaskRoute_WithoutAToken_Returns401(string method, string route)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Authentication required", problem.Title);
    }

    [Fact]
    public async Task Get_ForAnotherUsersTask_Returns404AndTheRowStillExistsForTheOwner()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var attacker = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(owner, "Private task");

        var response = await attacker.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Task not found.", problem.Title);

        var ownerPage = await owner.GetFromJsonAsync<TaskPageResource>("/api/tasks");
        Assert.NotNull(ownerPage);
        Assert.Contains(ownerPage.Items, task => task.Id == created.Id);
    }

    [Fact]
    public async Task Update_ForAnotherUsersTask_Returns404AndLeavesTheTaskUnchanged()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var attacker = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(owner, "Private task");

        var response = await attacker.PutAsJsonAsync($"/api/tasks/{created.Id}", new
        {
            title = "Hijacked title",
            status = "Done",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var ownerTask = await owner.GetFromJsonAsync<TaskResource>($"/api/tasks/{created.Id}");
        Assert.NotNull(ownerTask);
        Assert.Equal("Private task", ownerTask.Title);
        Assert.Equal("Todo", ownerTask.Status);
    }

    [Fact]
    public async Task Delete_ForAnotherUsersTask_Returns404AndTheRowStillExistsForTheOwner()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var attacker = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(owner, "Private task");

        var response = await attacker.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var ownerPage = await owner.GetFromJsonAsync<TaskPageResource>("/api/tasks");
        Assert.NotNull(ownerPage);
        Assert.Contains(ownerPage.Items, task => task.Id == created.Id);
    }

    [Fact]
    public async Task Get_WithAMissingTask_Returns404ProblemDetails()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Task not found.", problem.Title);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_Returns400WithTheFieldInTheErrorsMap()
    {
        var client = await CreateAuthenticatedClientAsync();

        var shortTitle = await client.PostAsJsonAsync("/api/tasks", new { title = "ab" });
        var longTitle = await client.PostAsJsonAsync("/api/tasks", new { title = new string('a', 201) });
        var longDescription = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Valid title",
            description = new string('a', 2001),
        });
        var unknownStatus = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Valid title",
            status = "Unknown",
        });
        var invalidDueDate = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Valid title",
            dueDate = "14/11/2026",
        });

        var shortProblem = await ReadErrorsAsync(shortTitle);
        var longProblem = await ReadErrorsAsync(longTitle);
        var descriptionProblem = await ReadErrorsAsync(longDescription);
        var statusProblem = await ReadErrorsAsync(unknownStatus);
        var dueDateProblem = await ReadErrorsAsync(invalidDueDate);

        Assert.Equal(["Title must be between 3 and 200 characters."], shortProblem.Errors["title"]);
        Assert.Equal(["Title must be between 3 and 200 characters."], longProblem.Errors["title"]);
        Assert.Equal(["Description must be at most 2000 characters."], descriptionProblem.Errors["description"]);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], statusProblem.Errors["status"]);
        Assert.Equal(["Due date must be a valid date."], dueDateProblem.Errors["dueDate"]);
    }

    [Fact]
    public async Task Update_WithoutAStatus_Returns400WithTheStatusKey()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Write the proposal");

        // The status is required on update; a missing field must not silently reset it.
        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", new
        {
            title = "Review the proposal",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadErrorsAsync(response);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], problem.Errors["status"]);
    }

    [Fact]
    public async Task SeededDemoUser_HasFiveTasksAcrossAllStatusesNewestFirst()
    {
        var client = await LoginAsync(DatabaseSeeder.DemoEmail, DatabaseSeeder.DemoPassword);

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<TaskPageResource>();
        Assert.NotNull(page);
        Assert.Equal(5, page.Items.Count);
        Assert.Null(page.NextCursor);
        // Story-shaped demo data for a busy professional, newest first; every task
        // carries a description and a due date so the demo shows the full feature.
        Assert.Equal(["Prepare the quarterly report", "Review the vendor contract",
            "Book the dentist appointment", "Renew the passport",
            "Send the tax documents to the accountant"], page.Items.Select(task => task.Title));
        Assert.Equal(["InProgress", "InProgress", "Todo", "Todo", "Done"],
            page.Items.Select(task => task.Status));
        Assert.All(page.Items, task => Assert.False(string.IsNullOrWhiteSpace(task.Description)));
        Assert.All(page.Items, task => Assert.NotNull(task.DueDate));
        Assert.All(page.Items, task => Assert.True(task.CreatedAtUtc.Kind is DateTimeKind.Utc));
    }

    private static string EncodeCursor(object payload) =>
        WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));

    private static async Task<ValidationProblemDetails> ReadErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        return problem;
    }

    private static async Task<TaskResource> CreateTaskAsync(
        HttpClient client, string title, string? status = null, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title,
            description,
            status,
        });
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<TaskResource>();
        Assert.NotNull(task);
        return task;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        using var registration = factory.CreateClient();
        var register = await registration.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Task User", "Secret123"));
        register.EnsureSuccessStatusCode();

        return await LoginAsync(email, "Secret123");
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    private sealed record MeResource(
        Guid Id,
        string Email,
        string DisplayName,
        DateTime CreatedAtUtc);

    private sealed record TaskResource(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        DateOnly? DueDate,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    private sealed record TaskPageResource(
        IReadOnlyList<TaskResource> Items,
        string? NextCursor);
}
