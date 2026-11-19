using System.Net;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using DocumentSubmission.Domain.Submissions;
using DocumentSubmission.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

/// <summary>
/// Two DbContexts load the same submission, as two simultaneous requests would. The first save wins;
/// the second fails and leaves nothing behind (N1, R22). The exception handler turns that failure into 409.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task ConcurrentActions_SecondIsRejectedAndSavesNothing_ApproveVsReturn()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;

        var exception = await ApproveThenReturnConcurrentlyAsync(id);

        Assert.IsType<DbUpdateConcurrencyException>(exception);
        var details = await editor.GetSubmissionAsync(id);
        Assert.Equal(SubmissionStatus.Approved, details.Status);
        Assert.Equal(1, details.ApprovedVersionNumber);
        Assert.Equal([AuditAction.Submitted, AuditAction.Approved], (await editor.GetAuditAsync(id)).Select(a => a.Action));
    }

    [Fact]
    public async Task ConcurrentActions_SecondIsRejectedAndSavesNothing_DoubleResubmit()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;
        await reviewer.PostReturnAsync(id, 1).EnsureOkAsync();

        await using var first = new Session(factory);
        await using var second = new Session(factory);
        var firstSubmission = await first.LoadAsync(id);
        var secondSubmission = await second.LoadAsync(id);

        firstSubmission.Resubmit(await first.UserAsync(AliceEditorNl), File("first.pdf"), null, DateTime.UtcNow);
        secondSubmission.Resubmit(await second.UserAsync(AliceEditorNl), File("second.pdf"), null, DateTime.UtcNow);
        await first.Db.SaveChangesAsync();

        // Either the duplicate version 2 (primary key) or the stale concurrency token stops the second save.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => second.Db.SaveChangesAsync());

        var details = await editor.GetSubmissionAsync(id);
        Assert.Equal(SubmissionStatus.Submitted, details.Status);
        Assert.Equal([1, 2], details.Versions.Select(v => v.Number));
        Assert.Equal("first.pdf", details.Versions.Last().FileName);
        Assert.Equal(
            [AuditAction.Submitted, AuditAction.Returned, AuditAction.Resubmitted],
            (await editor.GetAuditAsync(id)).Select(a => a.Action));
    }

    [Fact]
    public async Task ExceptionHandler_MapsConcurrencyErrorsTo409()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);

        // A real DbUpdateConcurrencyException: stale ConcurrencyToken.
        var concurrencyError = await ApproveThenReturnConcurrentlyAsync((await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id);
        Assert.IsType<DbUpdateConcurrencyException>(concurrencyError);
        await AssertMappedTo409Async(concurrencyError);

        // A real primary key violation on DocumentVersions: loading without the versions makes the
        // resubmit try to insert version 1 again, while the concurrency token is still current.
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;
        await reviewer.PostReturnAsync(id, 1).EnsureOkAsync();
        await using var session = new Session(factory);
        var submission = await session.Db.Submissions.SingleAsync(s => s.Id == id);
        submission.Resubmit(await session.UserAsync(AliceEditorNl), File("duplicate.pdf"), null, DateTime.UtcNow);
        var duplicateError = await Assert.ThrowsAnyAsync<DbUpdateException>(() => session.Db.SaveChangesAsync());
        Assert.IsNotType<DbUpdateConcurrencyException>(duplicateError);
        await AssertMappedTo409Async(duplicateError);
    }

    private async Task<Exception> ApproveThenReturnConcurrentlyAsync(Guid id)
    {
        await using var first = new Session(factory);
        await using var second = new Session(factory);
        var firstSubmission = await first.LoadAsync(id);
        var secondSubmission = await second.LoadAsync(id);

        firstSubmission.Approve(await first.UserAsync(ClaraReviewerNl), 1, DateTime.UtcNow);
        secondSubmission.Return(await second.UserAsync(ClaraReviewerNl), 1, "Too late", DateTime.UtcNow);
        await first.Db.SaveChangesAsync();

        return await Assert.ThrowsAnyAsync<DbUpdateException>(() => second.Db.SaveChangesAsync());
    }

    private async Task AssertMappedTo409Async(Exception exception)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetServices<IExceptionHandler>().Single();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        ProblemAssert.IsProblem(StatusCodes.Status409Conflict, httpContext.Response.ContentType?.Split(';')[0], body);
    }

    private static DocumentFile File(string name) => new(name, TestFiles.PdfContentType, TestFiles.Pdf(name));

    /// <summary>One DI scope with its own DbContext, like one HTTP request.</summary>
    private sealed class Session(ApiFactory factory) : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope = factory.Services.CreateAsyncScope();

        public AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        public Task<Submission> LoadAsync(Guid id) => Db.Submissions.Include(s => s.Versions).SingleAsync(s => s.Id == id);

        public async Task<Domain.Users.User> UserAsync(Guid id) => (await Db.Users.FindAsync(id))!;

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
