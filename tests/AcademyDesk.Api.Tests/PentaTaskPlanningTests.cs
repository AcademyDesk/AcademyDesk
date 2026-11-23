using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTaskPlanningTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly PentaOperationalScope Scope = new("academy-a", "actor-a", "connection-a", "conversation-a", 1, 1);
    private static readonly IReadOnlySet<string> Allowed = PentaTaskCatalog.Keys.ToHashSet(StringComparer.Ordinal);
    private static readonly Guid Course = Guid.Parse("854e2d35-5815-43b5-ad1b-62b8cc9f11ce");
    private static JsonObject Value(string kind, object value) => JsonSerializer.SerializeToNode(new { kind, value })!.AsObject();
    private static JsonObject Step(string id, string tool, JsonObject fields, JsonObject? bindings = null, string[]? dependencies = null) => new() {
        ["id"] = id, ["tool_key"] = tool, ["tool_revision"] = 1, ["fields"] = fields,
        ["bindings"] = bindings ?? new(), ["depends_on"] = JsonSerializer.SerializeToNode(dependencies ?? []) };
    private static JsonObject Bind(string step, string output) => new() { ["step_id"] = step, ["output"] = output };
    private static JsonObject Student(string birth = "2015-04-10") => new() {
        ["StudentFirstName"] = Value("text", "Anika"), ["StudentLastName"] = Value("text", "Rao"), ["DateOfBirth"] = Value("date", birth) };
    private static JsonObject Batch() => new() {
        ["Name"] = Value("text", "Tuesday Explorers"), ["CourseId"] = Value("reference", Course.ToString("D")),
        ["Capacity"] = Value("integer", 8), ["DeliveryMode"] = Value("text", "Online") };
    private static string Proposal(params JsonObject[] steps) => new JsonObject {
        ["contract"] = PentaTaskPlanning.Contract, ["base_revision"] = 0,
        ["steps"] = new JsonArray(steps.Select(x => (JsonNode)x).ToArray()) }.ToJsonString();
    private static string Compound(string birth = "2015-04-10") => Proposal(
        // Intentionally out of dependency order: IDs, not array position, bind results.
        Step("enroll", PentaEnrollmentFields.CreateTool, new(), new() {
            ["StudentId"] = Bind("student", "StudentId"), ["BatchId"] = Bind("batch", "BatchId") }),
        Step("student", PentaOnboardingFields.Tool, Student(birth)), Step("batch", PentaTaskCatalog.BatchCreate, Batch()));
    private static PentaTaskPlan Create(string json) => PentaTaskPlanning.Create(Scope, json, Allowed, Today);
    private static PentaTaskPlan ReadyCompound() => PentaTaskPlanning.Edit(Create(Compound("2000-01-01")), "batch", 1,
        Edit(new() { ["MeetingLink"] = Value("text", "https://meet.example.invalid/class") }), Allowed, Today);
    private static string Edit(JsonObject fields, params string[] remove) => new JsonObject {
        ["set"] = fields, ["remove"] = JsonSerializer.SerializeToNode(remove) }.ToJsonString();
    private static PentaTaskHostResult Result(PentaTaskPlan plan, string id, string output, Guid value)
    {
        var task = plan.Steps.Single(x => x.Id == id);
        return new(plan.Scope, id, task.ToolKey, task.Revision, "receipt-" + id, new Dictionary<string, Guid> { [output] = value });
    }

    [Fact]
    public void Compound_goal_retains_all_tasks_and_asks_only_missing_conditional_details()
    {
        var plan = Create(Compound());
        Assert.Equal(["student", "batch", "enroll"], plan.Steps.Select(x => x.Id));
        var student = plan.Steps[0];
        Assert.Equal(["ParentFirstName", "ParentLastName", "ParentEmail"], student.Issues.Select(x => x.Field));
        Assert.Equal("Anika", student.Fields["StudentFirstName"].GetProperty("value").GetString());
        Assert.Equal("MeetingLink", Assert.Single(plan.Steps[1].Issues).Field);
        Assert.Empty(plan.Steps[2].Issues);
        Assert.Equal("waiting-for-dependencies", plan.Steps[2].Status);
        Assert.DoesNotContain(student.Issues, x => x.Field is "StudentPhone" or "StudentEmail" or "BranchId");
    }

    [Fact]
    public void Multi_turn_details_are_retained_and_corrections_invalidate_dependent_draft_revisions()
    {
        var initial = Create(Compound());
        var supplied = PentaTaskPlanning.Edit(initial, "student", 1, Edit(new() {
            ["ParentFirstName"] = Value("text", "Sana"), ["ParentLastName"] = Value("text", "Rao"),
            ["ParentEmail"] = Value("text", "sana@example.invalid") }), Allowed, Today);
        Assert.Empty(supplied.Steps.Single(x => x.Id == "student").Issues);
        Assert.Equal(2, supplied.Steps.Single(x => x.Id == "enroll").Revision);
        Assert.Equal(1, supplied.Steps.Single(x => x.Id == "batch").Revision);
        var corrected = PentaTaskPlanning.Edit(supplied, "student", 2,
            Edit(new() { ["StudentFirstName"] = Value("text", "Anika corrected") }), Allowed, Today);
        Assert.Equal("Sana", corrected.Steps[0].Fields["ParentFirstName"].GetProperty("value").GetString());
        Assert.Equal("Anika", initial.Steps[0].Fields["StudentFirstName"].GetProperty("value").GetString());
        Assert.Equal(3, corrected.Revision);
        Assert.Equal(3, corrected.Steps.Single(x => x.Id == "enroll").Revision);
    }

    [Fact]
    public void Adult_rule_and_explicit_false_optional_values_use_existing_domain_rules()
    {
        var fields = Student("2000-01-01"); fields["AllowFinance"] = Value("boolean", false);
        var plan = Create(Proposal(Step("student", PentaOnboardingFields.Tool, fields)));
        Assert.Empty(plan.Steps[0].Issues);
        var payload = PentaTaskPlanning.PreparePayload(plan, "student", [], Allowed, Today);
        Assert.True(PentaOnboardingFields.TryRead(payload, out var request));
        Assert.False(request!.AllowFinance); Assert.Null(request.StudentEmail); Assert.Null(request.ParentEmail);
        Assert.Null(request.StudentTemporaryPassword);
    }

    [Fact]
    public void Omitted_birth_date_is_collected_instead_of_inventing_adult_or_minor_status()
    {
        var fields = Student(); fields.Remove("DateOfBirth");
        var plan = Create(Proposal(Step("student", PentaOnboardingFields.Tool, fields)));
        Assert.Equal("DateOfBirth", Assert.Single(plan.Steps[0].Issues).Field);
        Assert.False(plan.Steps[0].Fields.ContainsKey("DateOfBirth"));
    }

    [Fact]
    public void Domain_invalid_birth_date_and_email_are_not_ready_for_preparation()
    {
        var fields = Student("2099-01-01");
        var plan = Create(Proposal(Step("student", PentaOnboardingFields.Tool, fields)));
        Assert.Equal("business_rule", Assert.Single(plan.Steps[0].Issues).Code);
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "student", [], Allowed, Today));
        fields = Student("2000-01-01"); fields["StudentEmail"] = Value("text", "invalid-email");
        Assert.NotEmpty(Create(Proposal(Step("student", PentaOnboardingFields.Tool, fields))).Steps[0].Issues);
    }

    [Fact]
    public void Dependent_payload_uses_exact_host_result_ids_and_existing_enrollment_parser()
    {
        var plan = ReadyCompound();
        var student = Guid.NewGuid(); var batch = Guid.NewGuid();
        var payload = PentaTaskPlanning.PreparePayload(plan, "enroll",
            [Result(plan, "student", "StudentId", student), Result(plan, "batch", "BatchId", batch)], Allowed, Today);
        Assert.True(PentaEnrollmentFields.TryCreate(payload, out var request));
        Assert.Equal(student, request!.StudentId); Assert.Equal(batch, request.BatchId);
        Assert.Equal("Active", request.Status);
    }

    [Fact]
    public void Missing_or_partial_dependency_results_never_produce_an_enrollment_payload()
    {
        var plan = Create(Compound("2000-01-01"));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "enroll", [], Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "enroll",
            [Result(plan, "student", "StudentId", Guid.NewGuid())], Allowed, Today));
    }

    [Theory]
    [InlineData("tenant")][InlineData("actor")][InlineData("conversation")][InlineData("connection")]
    [InlineData("authority")][InlineData("context")][InlineData("revision")][InlineData("tool")]
    [InlineData("receipt")][InlineData("zero-id")][InlineData("wrong-output")]
    public void Foreign_stale_or_malformed_host_observations_are_rejected(string change)
    {
        var plan = ReadyCompound();
        var result = Result(plan, "student", "StudentId", Guid.NewGuid());
        result = change switch {
            "tenant" => result with { Scope = Scope with { TenantId = "academy-b" } },
            "actor" => result with { Scope = Scope with { ActorId = "actor-b" } },
            "conversation" => result with { Scope = Scope with { ConversationId = "conversation-b" } },
            "connection" => result with { Scope = Scope with { ConnectionId = "connection-b" } },
            "authority" => result with { Scope = Scope with { AuthorityVersion = 2 } },
            "context" => result with { Scope = Scope with { ContextVersion = 2 } },
            "revision" => result with { DraftRevision = 2 }, "tool" => result with { ToolKey = PentaTeacherCreationFields.Tool },
            "receipt" => result with { ReceiptRef = "" }, "zero-id" => result with { Identities = new Dictionary<string, Guid> { ["StudentId"] = Guid.Empty } },
            _ => result with { Identities = new Dictionary<string, Guid> { ["TeacherId"] = Guid.NewGuid() } } };
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "enroll",
            [result, Result(plan, "batch", "BatchId", Guid.NewGuid())], Allowed, Today));
    }

    [Fact]
    public void A_receipt_is_not_reexecuted_and_duplicate_receipt_observations_are_rejected()
    {
        var plan = Create(Compound("2000-01-01")); var result = Result(plan, "student", "StudentId", Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "student", [result], Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "enroll", [result, result], Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "student", 1,
            Edit(new() { ["StudentFirstName"] = Value("text", "Changed") }), Allowed, Today, [result]));
    }

    [Fact]
    public void Revoked_capability_or_stale_draft_version_blocks_edit_and_preparation()
    {
        var plan = Create(Compound("2000-01-01")); var none = new HashSet<string>();
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Create(Scope, Compound(), none, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "student", 0, Edit(new()), Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "student", 1, Edit(new()), none, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "student", [], none, Today));
    }

    [Fact]
    public void Wrong_entity_binding_cannot_turn_a_teacher_id_into_a_student_id()
    {
        var teacher = Step("teacher", PentaTeacherCreationFields.Tool, new() {
            ["FirstName"] = Value("text", "Synthetic"), ["LastName"] = Value("text", "Teacher") });
        var enrollment = Step("enroll", PentaEnrollmentFields.CreateTool, new() {
            ["BatchId"] = Value("reference", Guid.NewGuid().ToString("D")) }, new() { ["StudentId"] = Bind("teacher", "TeacherId") });
        Assert.Throws<ArgumentException>(() => Create(Proposal(teacher, enrollment)));
    }

    [Theory]
    [InlineData("cycle")][InlineData("missing")][InlineData("self")][InlineData("duplicate")][InlineData("unknown-output")]
    public void Invalid_dependency_graphs_are_rejected(string mode)
    {
        var a = Step("a", PentaTeacherCreationFields.Tool, new(), dependencies: mode switch {
            "cycle" => ["b"], "missing" => ["absent"], "self" => ["a"], _ => [] });
        var b = Step(mode == "duplicate" ? "a" : "b", PentaTeacherCreationFields.Tool, new(), dependencies: mode == "cycle" ? ["a"] : []);
        if (mode == "unknown-output") a["bindings"] = new JsonObject { ["BranchId"] = Bind("b", "UnknownId") };
        Assert.Throws<ArgumentException>(() => Create(Proposal(a, b)));
    }

    [Theory]
    [InlineData("Approved")][InlineData("ActorId")][InlineData("AcademyId")][InlineData("Role")]
    [InlineData("StudentTemporaryPassword")][InlineData("Sql")][InlineData("CallbackUrl")]
    public void Model_cannot_supply_authority_credentials_or_arbitrary_execution_fields(string field)
    {
        var values = Student(); values[field] = Value("text", "untrusted-private-value");
        var error = Assert.Throws<ArgumentException>(() => Create(Proposal(Step("student", PentaOnboardingFields.Tool, values))));
        Assert.DoesNotContain("untrusted-private-value", error.Message);
    }

    [Theory]
    [InlineData("{\"contract\":\"private-value\"")]
    [InlineData("[]")]
    [InlineData("{\"contract\":\"academy-task-plan.v1\",\"base_revision\":0,\"steps\":[],\"approved\":true}")]
    [InlineData("{\"contract\":\"academy-task-plan.v1\",\"base_revision\":0,\"base_revision\":0,\"steps\":[]}")]
    public void Closed_contract_rejects_malformed_duplicate_and_extra_properties_without_echo(string json)
    {
        var error = Assert.Throws<ArgumentException>(() => Create(json)); Assert.DoesNotContain("private-value", error.Message);
    }

    [Fact]
    public void Contradictory_edits_unknown_fields_and_binding_overrides_are_rejected()
    {
        var plan = Create(Compound());
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "student", 1,
            Edit(new() { ["StudentFirstName"] = Value("text", "Correction") }, "StudentFirstName"), Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "enroll", 1,
            Edit(new() { ["StudentId"] = Value("reference", Guid.NewGuid().ToString("D")) }), Allowed, Today));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.Edit(plan, "student", 1,
            Edit(new() { ["UnknownField"] = Value("text", "Unknown") }), Allowed, Today));
    }

    [Fact]
    public void Registry_distinguishes_draft_only_batch_from_existing_protected_adapters()
    {
        Assert.Equal("draft-only", PentaTaskCatalog.Describe(PentaTaskCatalog.BatchCreate, new Dictionary<string, JsonElement>(), Today).Binding);
        Assert.Equal("protected-adapter", PentaTaskCatalog.Describe(PentaTeacherCreationFields.Tool, new Dictionary<string, JsonElement>(), Today).Binding);
        Assert.Throws<ArgumentException>(() => Create(Proposal(Step("x", "ExecuteSql", new()))));
    }

    [Fact]
    public void Unresolved_source_draft_cannot_be_claimed_complete_to_unblock_enrollment()
    {
        var plan = Create(Compound("2000-01-01"));
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "enroll",
            [Result(plan, "student", "StudentId", Guid.NewGuid()), Result(plan, "batch", "BatchId", Guid.NewGuid())], Allowed, Today));
    }

    [Fact]
    public void Batch_draft_covers_actual_request_properties_without_making_it_executable()
    {
        var definition = PentaTaskCatalog.Describe(PentaTaskCatalog.BatchCreate, new Dictionary<string, JsonElement>(), Today);
        Assert.Equal(typeof(CreateBatchRequest).GetProperties().Select(x => x.Name).Order(StringComparer.Ordinal),
            definition.Fields.Select(x => x.Key).Order(StringComparer.Ordinal));
        Assert.Equal("draft-only", definition.Binding);
    }

    [Theory]
    [InlineData("Capacity", 0)][InlineData("Capacity", 1001)]
    [InlineData("WaitlistCapacity", -1)][InlineData("WaitlistCapacity", 1001)]
    public void Batch_numeric_bounds_require_correction_before_host_preparation(string field, int value)
    {
        var fields = Batch(); fields["MeetingLink"] = Value("text", "https://meet.example.invalid/class");
        fields[field] = Value("integer", value);
        var plan = Create(Proposal(Step("batch", PentaTaskCatalog.BatchCreate, fields)));
        Assert.Contains(plan.Steps[0].Issues, x => x.Field == field && x.Code == "invalid");
        Assert.Throws<ArgumentException>(() => PentaTaskPlanning.PreparePayload(plan, "batch", [], Allowed, Today));
    }
}
