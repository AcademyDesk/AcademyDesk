using System.Reflection;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Tests;

public sealed class PentaMiniRecoveryTests
{
    private static readonly Guid Session = Guid.NewGuid(), Request = Guid.NewGuid(), Learner = Guid.NewGuid();
    private static MiniReceipt Receipt => new(Session, Request, 1, "RESULT", "Verified source read.", "executor",
        new OutstandingResult(1, false, [new OutstandingLearner(Learner, "Synthetic learner", ["Piano"], [], $"/student-management?studentId={Learner:D}")], DateTime.UtcNow),
        new MiniState(null, [Learner.ToString("D")], [], null));
    [Fact]
    public void Exact_stored_receipt_matches_its_original_request_and_version() =>
        Assert.True(PentaMiniRecovery.ValidReceipt(Receipt, Session, Request, 0));
    [Theory]
    [InlineData("session")][InlineData("request")][InlineData("version")][InlineData("provider")]
    [InlineData("protocol")][InlineData("capability")][InlineData("kind")][InlineData("message")]
    [InlineData("state")][InlineData("source")][InlineData("rows")][InlineData("count")][InlineData("truncation")]
    public void Foreign_or_corrupt_receipt_is_never_a_recovery_response(string change)
    {
        var r = Receipt;
        r = change switch {
            "session" => r with { ConversationId = Guid.NewGuid() },
            "request" => r with { RequestId = Guid.NewGuid() },
            "version" => r with { Version = 2 },
            "provider" => r with { Provider = "Unqualified replacement" },
            "protocol" => r with { Protocol = "9" },
            "capability" => r with { Capability = "admin" },
            "kind" => r with { Kind = "MODEL_AUTHORIZED" },
            "message" => r with { Message = "" },
            "state" => r with { Context = MiniState.Empty },
            "source" => r with { Result = r.Result! with { Rows = [r.Result.Rows[0] with { SourcePath = "/platform" }] } },
            "rows" => r with { Result = r.Result! with { Rows = [r.Result.Rows[0], r.Result.Rows[0]] } },
            "count" => r with { Result = r.Result! with { Count = -1 } },
            "truncation" => r with { Result = r.Result! with { HasMore = true } },
            _ => throw new InvalidOperationException()
        };
        Assert.False(PentaMiniRecovery.ValidReceipt(r, Session, Request, 0));
    }
    [Theory]
    [InlineData("ERROR")][InlineData("REFUSAL")][InlineData("UNSUPPORTED")]
    [InlineData("TEXT_RESPONSE")][InlineData("CLARIFICATION_REQUIRED")][InlineData("APPROVAL_REQUIRED")]
    public void Safe_nonresult_receipts_can_be_recovered_without_inventing_rows(string kind) =>
        Assert.True(PentaMiniRecovery.ValidReceipt(Receipt with { Kind = kind, Result = null, Context = MiniState.Empty }, Session, Request, 0));
    [Fact]
    public void Source_backed_clarification_preserves_current_result_binding() =>
        Assert.True(PentaMiniRecovery.ValidReceipt(Receipt with { Kind = "CLARIFICATION_REQUIRED" }, Session, Request, 0));
    [Fact]
    public void Invalid_expected_version_empty_IDs_and_missing_result_are_refused()
    {
        Assert.False(PentaMiniRecovery.ValidReceipt(null, Session, Request, 0));
        Assert.False(PentaMiniRecovery.ValidReceipt(Receipt, Guid.Empty, Request, 0));
        Assert.False(PentaMiniRecovery.ValidReceipt(Receipt, Session, Guid.Empty, 0));
        Assert.False(PentaMiniRecovery.ValidReceipt(Receipt, Session, Request, -1));
        Assert.False(PentaMiniRecovery.ValidReceipt(Receipt, Session, Request, long.MaxValue));
        Assert.False(PentaMiniRecovery.ValidReceipt(Receipt with { Result = null }, Session, Request, 0));
    }
    [Fact]
    public void Recovery_is_a_GET_and_private_conversation_actions_are_not_cacheable()
    {
        var method = typeof(PentaChatController).GetMethod(nameof(PentaChatController.ReadTurn))!;
        Assert.Equal("conversations/{conversationId:guid}/turns/{requestId:guid}", method.GetCustomAttribute<HttpGetAttribute>()!.Template);
        foreach (var action in new[] { "ReadTurn", "Health", "Create", "Turn" })
            Assert.True(typeof(PentaChatController).GetMethod(action)!.GetCustomAttribute<ResponseCacheAttribute>()!.NoStore);
    }
}
