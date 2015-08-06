using Microsoft.TeamFoundation.Build.Client;

namespace TfsBuildDefinitionsCommon
{
    public static class RetentionPoliciesHelper
    {

        public static void SetRetentionPolicies(IBuildDefinition definition, int succeeded, int partiallySucceeded, int failed, int stopped)
        {
            definition.RetentionPolicyList.Clear();
            definition.AddRetentionPolicy(BuildReason.ValidateShelveset, BuildStatus.Succeeded, succeeded, DeleteOptions.Details | DeleteOptions.DropLocation | DeleteOptions.Label | DeleteOptions.TestResults);
            definition.AddRetentionPolicy(BuildReason.ValidateShelveset, BuildStatus.PartiallySucceeded, partiallySucceeded, DeleteOptions.All);
            definition.AddRetentionPolicy(BuildReason.ValidateShelveset, BuildStatus.Failed, failed, DeleteOptions.All);
            definition.AddRetentionPolicy(BuildReason.ValidateShelveset, BuildStatus.Stopped, stopped, DeleteOptions.All);
            definition.AddRetentionPolicy(BuildReason.Triggered, BuildStatus.Succeeded, succeeded, DeleteOptions.Details | DeleteOptions.DropLocation | DeleteOptions.Label | DeleteOptions.TestResults);
            definition.AddRetentionPolicy(BuildReason.Triggered, BuildStatus.PartiallySucceeded, partiallySucceeded, DeleteOptions.All);
            definition.AddRetentionPolicy(BuildReason.Triggered, BuildStatus.Failed, failed, DeleteOptions.All);
            definition.AddRetentionPolicy(BuildReason.Triggered, BuildStatus.Stopped, stopped, DeleteOptions.All);
        }
    }
}
