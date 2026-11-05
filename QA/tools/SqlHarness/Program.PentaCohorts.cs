using AcademyDesk.Api.Tests.Infrastructure;

internal static partial class SqlHarnessEntryPoint
{
    // Each cohort runs against the existing runner's freshly owned SQL/Identity
    // fixture. These names do not enable an interpreter, model or live provider.
    private static async Task<bool> TryVerifyPentaHostCohortAsync(
        QaApiFactory factory, QaRunManifest manifest, string? mode)
    {
        if (mode is not ("--audit-penta-foundation" or "--audit-penta-onboarding" or "--audit-penta-teacher"))
            return false;
        if (Environment.GetEnvironmentVariable("QA_PENTA_MINI") == "1" ||
            Environment.GetEnvironmentVariable("QA_PENTA_BROWSER") == "1")
            throw new InvalidOperationException("Protected host cohorts require model and interactive browser modes to remain disabled.");

        if (mode == "--audit-penta-foundation")
        {
            if (Environment.GetEnvironmentVariable("QA_PENTA_HOST_SECURITY") != "1")
                throw new InvalidOperationException("The foundation cohort requires all retained host security and read privacy checks.");
            await VerifyPentaFoundationAsync(manifest);
        }
        else if (mode == "--audit-penta-onboarding")
        {
            await VerifyPentaOnboardingDraftsAsync(factory, manifest);
            await VerifyPentaOnboardingExecutionAsync(factory, manifest);
            await VerifyPentaOnboardingHttpAsync(factory, manifest);
            await VerifyPentaOnboardingCoreContextAsync(factory, manifest);
            await VerifyPentaOnboardingPreparationAsync(factory, manifest);
            await VerifyPentaOnboardingLifecycleAsync(factory, manifest);
            await VerifyPentaOnboardingApprovalAsync(factory, manifest);
            await VerifyPentaOnboardingPartialAsync(factory, manifest);
            await VerifyPentaOnboardingConversationAsync(factory, manifest);
            await VerifyPentaOnboardingTurnAsync(factory, manifest);
            await VerifyPentaOnboardingGatewayAsync(factory, manifest);
            await VerifyPentaOnboardingStartAsync(manifest);
        }
        else
        {
            await VerifyPentaTeacherCreationAsync(factory, manifest);
            await VerifyPentaTeacherCoreAsync(manifest);
            await VerifyPentaTeacherConversationAsync(manifest);
            await VerifyPentaTeacherStartAsync(manifest);
        }
        Console.WriteLine($"PASS: protected PENTA host cohort {mode}; retained synthetic/private host checks only, no model qualification.");
        return true;
    }
}
