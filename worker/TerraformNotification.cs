using System;

using System.Text.Json.Serialization;

public sealed record TerraformNotification(
  [property: JsonPropertyName("notification_configuration_id")] string NotificationConfigurationId,
  [property: JsonPropertyName("payload_version")] int PayloadVersion,
  [property: JsonPropertyName("run_id")] string? RunId, // Can be null in tests
  [property: JsonPropertyName("workspace_id")] string? WorkspaceId, // Can be null in tests
  [property: JsonPropertyName("workspace_name")] string? WorkspaceName, // Can be null in tests
  [property: JsonPropertyName("run_url")] string? RunUrl,
  [property: JsonPropertyName("run_message")] string? RunMessage,
  [property: JsonPropertyName("run_created_at")] DateTime? RunCreatedAt,
  [property: JsonPropertyName("run_created_by")] string? RunCreatedBy,
  [property: JsonPropertyName("organization_name")] string? OrganizationName,
  [property: JsonPropertyName("notifications")] List<TerraformNotificationEvent> Notifications
);