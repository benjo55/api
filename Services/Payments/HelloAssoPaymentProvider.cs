using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using api.Configuration;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.Extensions.Options;

namespace api.Services.Payments
{
    public sealed class HelloAssoPaymentProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHelloAssoTokenProvider _tokenProvider;
        private readonly HelloAssoOptions _options;
        private readonly ILogger<HelloAssoPaymentProvider> _logger;

        public HelloAssoPaymentProvider(
            IHttpClientFactory httpClientFactory,
            IHelloAssoTokenProvider tokenProvider,
            IOptions<HelloAssoOptions> options,
            ILogger<HelloAssoPaymentProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutCommand command, CancellationToken cancellationToken)
        {
            var payload = new
            {
                totalAmount = command.AmountInCents,
                initialAmount = command.AmountInCents,
                itemName = command.ItemName,
                backUrl = command.BackUrl,
                errorUrl = command.ErrorUrl,
                returnUrl = command.ReturnUrl,
                containsDonation = true,
                payer = new
                {
                    firstName = command.FirstName,
                    lastName = command.LastName,
                    email = command.Email,
                    address = command.Address,
                    zipCode = command.ZipCode,
                    city = command.City,
                    country = NormalizeCountry(command.Country),
                    dateOfBirth = FormatDateOfBirth(command.DateOfBirth),
                },
                metadata = command.Metadata,
            };

            var path = $"/v5/organizations/{Uri.EscapeDataString(command.OrganizationSlug)}/checkout-intents";
            var response = await SendWithRetryAndAuthAsync(HttpMethod.Post, path, payload, cancellationToken, command.CredentialKey);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "HelloAsso checkout failed with HTTP {StatusCode} for organization {OrganizationSlug}. Response: {ResponseBody}",
                    (int)response.StatusCode,
                    command.OrganizationSlug,
                    body);

                return new CreateCheckoutResult(
                    false,
                    null,
                    null,
                    ((int)response.StatusCode).ToString(),
                    "Impossible de creer le checkout HelloAsso.",
                    body);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            var checkoutIntentId = ReadJsonValueAsString(node?["id"]);
            var redirectUrl = node?["redirectUrl"]?.GetValue<string>();

            return new CreateCheckoutResult(
                true,
                checkoutIntentId,
                redirectUrl,
                null,
                null,
                json);
        }

        private static string NormalizeCountry(string country)
        {
            var value = country.Trim().ToUpperInvariant();
            if (value.Length != 2)
            {
                return value;
            }

            try
            {
                return new RegionInfo(value).ThreeLetterISORegionName;
            }
            catch (ArgumentException)
            {
                return value;
            }
        }

        private static string? FormatDateOfBirth(DateTime? dateOfBirth) =>
            dateOfBirth?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public async Task<PaymentReconciliationResult> ReconcilePaymentAsync(PaymentReconciliationCommand command, CancellationToken cancellationToken)
        {
            var path = $"/v5/organizations/{Uri.EscapeDataString(command.OrganizationSlug)}/checkout-intents/{Uri.EscapeDataString(command.CheckoutIntentId)}";
            var response = await SendWithRetryAndAuthAsync(HttpMethod.Get, path, null, cancellationToken, command.CredentialKey);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PaymentReconciliationResult(
                    false,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    new Dictionary<string, string>(),
                    ((int)response.StatusCode).ToString(),
                    "Echec de reconciliation HelloAsso.",
                    payload);
            }

            var root = JsonNode.Parse(payload);
            var order = root?["order"];
            var payment = order?["payments"]?.AsArray().FirstOrDefault();
            var amountInCents = ReadCheckoutAmountInCents(root, order);
            var currency = ReadCheckoutCurrency(root, order, payment);

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var metadataNode = root?["metadata"]?.AsObject();
            if (metadataNode is not null)
            {
                foreach (var kvp in metadataNode)
                {
                    metadata[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                }
            }

            var externalOrderId = ReadJsonValueAsString(order?["id"]);
            var externalPaymentId = ReadJsonValueAsString(payment?["id"]);
            var providerState = payment?["state"]?.GetValue<string>();
            var isAuthorized = HelloAssoPaymentStatusMapper.Map(providerState) == PaymentStatus.Authorized;

            return new PaymentReconciliationResult(
                true,
                isAuthorized,
                externalOrderId,
                externalPaymentId,
                providerState,
                amountInCents,
                currency,
                metadata,
                null,
                null,
                payload);
        }

        public Task<WebhookReceptionResult> ReceiveWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, string? remoteIpAddress, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_options.WebhookSignatureKey))
            {
                if (!headers.TryGetValue("x-ha-signature", out var signature) || string.IsNullOrWhiteSpace(signature))
                {
                    return Task.FromResult(new WebhookReceptionResult(false, null, null, "MISSING_SIGNATURE", "Signature webhook absente."));
                }

                var expected = HelloAssoSecurity.ComputeHmacSha256Base64(rawBody, _options.WebhookSignatureKey);
                if (!HelloAssoSecurity.ConstantTimeEquals(expected, signature))
                {
                    return Task.FromResult(new WebhookReceptionResult(false, null, null, "INVALID_SIGNATURE", "Signature webhook invalide."));
                }
            }
            else if (_options.AllowedWebhookIpAddresses.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(remoteIpAddress) || !_options.AllowedWebhookIpAddresses.Contains(remoteIpAddress))
                {
                    return Task.FromResult(new WebhookReceptionResult(false, null, null, "IP_NOT_ALLOWED", "IP source non autorisee."));
                }
            }

            var node = JsonNode.Parse(rawBody);
            var eventType = node?["eventType"]?.GetValue<string>() ?? node?["eventTypeName"]?.GetValue<string>();
            var externalObjectId = ReadJsonValueAsString(node?["data"]?["id"])
                ?? ReadJsonValueAsString(node?["id"]);

            return Task.FromResult(new WebhookReceptionResult(true, eventType, externalObjectId, null, null));
        }

        private async Task<HttpResponseMessage> SendWithRetryAndAuthAsync(HttpMethod method, string path, object? payload, CancellationToken cancellationToken, string? credentialKey)
        {
            var maxRetries = Math.Max(1, _options.RetryCount);

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken, credentialKey);
                var request = BuildRequest(method, path, payload, accessToken);
                var client = _httpClientFactory.CreateClient("helloasso-api");
                var response = await client.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 1)
                {
                    response.Dispose();
                    await _tokenProvider.RefreshAccessTokenAsync(cancellationToken, credentialKey);
                    continue;
                }

                if (!HttpRetryHelper.IsTransient(response.StatusCode) || attempt == maxRetries)
                {
                    return response;
                }

                await HttpRetryHelper.DelayWithBackoffAsync(attempt, response.Headers.RetryAfter, cancellationToken);
                response.Dispose();
            }

            throw new InvalidOperationException("Appel HelloAsso impossible.");
        }

        private static HttpRequestMessage BuildRequest(HttpMethod method, string path, object? payload, string accessToken)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (payload is not null)
            {
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private static string? ReadJsonValueAsString(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            try
            {
                return node.GetValue<string>();
            }
            catch (InvalidOperationException)
            {
                return node.ToJsonString();
            }
        }

        private static int? ReadCheckoutAmountInCents(JsonNode? root, JsonNode? order)
        {
            return ReadJsonInt(root?["initialAmount"])
                ?? ReadJsonInt(root?["amount"]?["initialAmount"])
                ?? ReadJsonInt(order?["items"]?.AsArray().FirstOrDefault()?["amount"])
                ?? ReadJsonInt(root?["totalAmount"]);
        }

        private static string? ReadCheckoutCurrency(JsonNode? root, JsonNode? order, JsonNode? payment)
        {
            return ReadJsonValueAsString(root?["currency"])
                ?? ReadJsonValueAsString(root?["amount"]?["currency"])
                ?? ReadJsonValueAsString(order?["currency"])
                ?? ReadJsonValueAsString(order?["amount"]?["currency"])
                ?? ReadJsonValueAsString(order?["items"]?.AsArray().FirstOrDefault()?["currency"])
                ?? ReadJsonValueAsString(payment?["currency"]);
        }

        private static int? ReadJsonInt(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            try
            {
                return node.GetValue<int>();
            }
            catch (InvalidOperationException)
            {
                return int.TryParse(node.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : null;
            }
        }
    }
}
