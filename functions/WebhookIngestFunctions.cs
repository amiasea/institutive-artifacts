using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amiasea.Functions;

public class WebhookIngestFunctions
{
  private readonly IConfiguration _configuration;
  private readonly ILogger<WebhookIngestFunctions> _logger;
  private readonly ServiceBusSender _sender;

  public WebhookIngestFunctions(
      IConfiguration configuration,
      ILogger<WebhookIngestFunctions> logger,
      ServiceBusSender sender)
  {
    _configuration = configuration;
    _logger = logger;
    _sender = sender;
  }

  [Function("IngestTerraformWebhook")]
  public async Task<IActionResult> RunTerraform(
      [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "webhooks/terraform")] HttpRequest req)
  {
    var token = _configuration["HcpTerraformWebhookToken"];

    if (string.IsNullOrWhiteSpace(token))
    {
      _logger.LogError(
          "HcpTerraformNotificationToken is not configured.");

      return new StatusCodeResult(
          StatusCodes.Status500InternalServerError);
    }

    if (!req.Headers.TryGetValue(
            "X-TFE-Notification-Signature",
            out var signatureHeader))
    {
      _logger.LogWarning(
          "HCP Terraform request rejected: missing signature.");

      return new UnauthorizedResult();
    }

    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    if (string.IsNullOrEmpty(body))
    {
      return new BadRequestObjectResult(
          "Request body is empty.");
    }

    if (!WebhookValidator.ValidateTerraformSignature(
            body,
            signatureHeader.ToString(),
            token))
    {
      _logger.LogWarning(
          "HCP Terraform request rejected: signature mismatch.");

      return new UnauthorizedResult();
    }

    await _sender.SendMessageAsync(
        new ServiceBusMessage(body)
        {
          ContentType = "application/json",
          Subject = "terraform",
          ApplicationProperties =
            {
              ["webhookSource"] = "terraform"
            }
        });

    _logger.LogInformation(
        "Validated HCP Terraform webhook received ({Bytes} bytes) and enqueued.",
        body.Length);

    return new AcceptedResult();
  }

  [Function("IngestGitHubWebhook")]
  public async Task<IActionResult> RunGitHub(
      [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "webhooks/github")] HttpRequest req)
  {
    var secret = _configuration["GitHubWebhookToken"];

    if (string.IsNullOrWhiteSpace(secret))
    {
      _logger.LogError(
          "GitHubWebhookSecret is not configured.");

      return new StatusCodeResult(
          StatusCodes.Status500InternalServerError);
    }

    if (!req.Headers.TryGetValue(
            "X-Hub-Signature-256",
            out var signatureHeader))
    {
      _logger.LogWarning(
          "GitHub request rejected: missing signature.");

      return new UnauthorizedResult();
    }

    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    if (string.IsNullOrEmpty(body))
    {
      return new BadRequestObjectResult(
          "Request body is empty.");
    }

    if (!WebhookValidator.ValidateGitHubSignature(
            body,
            signatureHeader.ToString(),
            secret))
    {
      _logger.LogWarning(
          "GitHub request rejected: signature mismatch.");

      return new UnauthorizedResult();
    }

    req.Headers.TryGetValue(
        "X-GitHub-Event",
        out var eventType);

    req.Headers.TryGetValue(
        "X-GitHub-Delivery",
        out var deliveryId);

    if (string.Equals(
            eventType,
            "ping",
            StringComparison.OrdinalIgnoreCase))
    {
      _logger.LogInformation(
          "GitHub ping handshake verified.");

      return new OkObjectResult(
          new { message = "Pong" });
    }

    await _sender.SendMessageAsync(
        new ServiceBusMessage(body)
        {
          ContentType = "application/json",
          Subject = "github",
          ApplicationProperties =
            {
              ["webhookSource"] = "github",
              ["eventType"] = eventType.ToString(),
              ["deliveryId"] = deliveryId.ToString()
            }
        });

    _logger.LogInformation(
        "Validated GitHub '{Event}' webhook received ({Bytes} bytes) and enqueued.",
        eventType.ToString(),
        body.Length);

    return new AcceptedResult();
  }
}