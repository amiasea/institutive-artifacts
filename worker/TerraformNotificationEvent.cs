using System;

using System.Text.Json.Serialization;

public sealed record TerraformNotificationEvent(
  [property: JsonPropertyName("message")] string Message,
  [property: JsonPropertyName("trigger")] string Trigger, // e.g., "verification" or "run:created"
  [property: JsonPropertyName("run_status")] string? RunStatus,
  [property: JsonPropertyName("run_updated_at")] DateTime? RunUpdatedAt,
  [property: JsonPropertyName("run_updated_by")] string? RunUpdatedBy
);