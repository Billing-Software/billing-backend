using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    /// <summary>
    /// HTTP client for Meta Graph API and WhatsApp Cloud API.
    /// Uses IHttpClientFactory for proper connection pooling.
    /// </summary>
    public class MetaApiClient : IMetaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _graphApiVersion;
        private readonly ILogger<MetaApiClient> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MetaApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<MetaApiClient> logger)
        {
            _httpClient = httpClient;
            _appId = configuration["Meta:AppId"] ?? throw new ArgumentNullException("Meta:AppId is missing from configuration");
            _appSecret = configuration["Meta:AppSecret"] ?? throw new ArgumentNullException("Meta:AppSecret is missing from configuration");
            _graphApiVersion = configuration["Meta:GraphApiVersion"] ?? "v21.0";
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<MetaTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                _logger.LogInformation("[MetaApiClient] Exchanging OAuth code with Graph API {Version}... AppId: {AppId}", _graphApiVersion, _appId);

                var url = $"https://graph.facebook.com/{_graphApiVersion}/oauth/access_token" +
                          $"?client_id={_appId}" +
                          $"&client_secret={_appSecret}" +
                          $"&code={Uri.EscapeDataString(code)}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[MetaApiClient] Meta Token Exchange HTTP Error {StatusCode}: {Content}", response.StatusCode, content);
                    return new MetaTokenResponse { Error = $"Meta token exchange failed ({response.StatusCode}): {content}" };
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var token = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
                var expires = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : (long?)null;

                _logger.LogInformation("[MetaApiClient] Meta Token Exchange Successful! AccessToken Length: {Len}, ExpiresIn: {Expires}s", token?.Length ?? 0, expires);

                return new MetaTokenResponse
                {
                    AccessToken = token,
                    TokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() : null,
                    ExpiresIn = expires
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MetaApiClient] Exception while exchanging authorization code for token");
                return new MetaTokenResponse { Error = ex.Message };
            }
        }

        /// <inheritdoc/>
        public async Task<MetaBusinessInfoResponse> GetSharedWabaInfoAsync(string accessToken)
        {
            try
            {
                // Step 1: Get shared WABA IDs using the debug_token or business management API
                var wabaUrl = $"https://graph.facebook.com/{_graphApiVersion}/debug_token?input_token={accessToken}";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{_appId}|{_appSecret}");

                var debugResponse = await _httpClient.GetAsync(wabaUrl);
                var debugContent = await debugResponse.Content.ReadAsStringAsync();

                // Step 2: Use the token to query shared WABAs
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var sharedWabaUrl = $"https://graph.facebook.com/{_graphApiVersion}/me/businesses";
                var bizResponse = await _httpClient.GetAsync(sharedWabaUrl);
                var bizContent = await bizResponse.Content.ReadAsStringAsync();

                // Step 3: Get WABA phone numbers
                // For Embedded Signup, we query the granted assets
                var assetsUrl = $"https://graph.facebook.com/{_graphApiVersion}/me?fields=id,name";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var assetsResponse = await _httpClient.GetAsync(assetsUrl);
                var assetsContent = await assetsResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Shared WABA lookup — Debug: {Debug}, Biz: {Biz}, Assets: {Assets}",
                    debugContent, bizContent, assetsContent);

                // Parse the WABA and phone number from the Embedded Signup response
                // The actual structure depends on Meta's Embedded Signup callback data
                // In practice, the Embedded Signup returns these IDs directly in the JS callback
                // This endpoint is a fallback/verification step

                return new MetaBusinessInfoResponse
                {
                    // These will be populated from the Embedded Signup JS response
                    // and verified server-side. For now, return what we can parse.
                    Error = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get shared WABA info");
                return new MetaBusinessInfoResponse { Error = ex.Message };
            }
            finally
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SubscribeWabaToAppAsync(string wabaId, string accessToken)
        {
            try
            {
                var url = $"https://graph.facebook.com/{_graphApiVersion}/{wabaId}/subscribed_apps";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully subscribed BillCom App to WABA: {WabaId}", wabaId);
                    return true;
                }

                _logger.LogWarning("Failed to subscribe app to WABA {WabaId}: {StatusCode} {Content}",
                    wabaId, response.StatusCode, content);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing app to WABA {WabaId}", wabaId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MetaPhoneNumberInfo>> GetWabaPhoneNumbersAsync(string wabaId, string accessToken)
        {
            var result = new List<MetaPhoneNumberInfo>();
            try
            {
                var url = $"https://graph.facebook.com/{_graphApiVersion}/{wabaId}/phone_numbers";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch phone numbers for WABA {WabaId}: {StatusCode} {Content}",
                        wabaId, response.StatusCode, content);
                    return result;
                }

                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        result.Add(new MetaPhoneNumberInfo
                        {
                            Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                            DisplayPhoneNumber = item.TryGetProperty("display_phone_number", out var dpn) ? dpn.GetString() : null,
                            VerifiedName = item.TryGetProperty("verified_name", out var vn) ? vn.GetString() : null,
                            QualityRating = item.TryGetProperty("quality_rating", out var qr) ? qr.GetString() : null,
                            Status = item.TryGetProperty("status", out var st) ? st.GetString() : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching phone numbers for WABA {WabaId}", wabaId);
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterPhoneNumberAsync(string phoneNumberId, string accessToken, string pin = "654321")
        {
            try
            {
                var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/register";
                var payload = new
                {
                    messaging_product = "whatsapp",
                    pin = pin
                };
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully registered phone number {PhoneNumberId}", phoneNumberId);
                    return true;
                }

                _logger.LogWarning("Failed to register phone number {PhoneNumberId}: {StatusCode} {Content}",
                    phoneNumberId, response.StatusCode, content);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering phone number {PhoneNumberId}", phoneNumberId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<MetaTemplateCreateResponse> CreateWabaTemplateAsync(
            string wabaId, string accessToken, string templateName, string category, string language, string bodyText)
        {
            try
            {
                var url = $"https://graph.facebook.com/{_graphApiVersion}/{wabaId}/message_templates";
                var payload = new
                {
                    name = templateName.ToLowerInvariant(),
                    language = language,
                    category = category.ToUpperInvariant(),
                    components = new object[]
                    {
                        new
                        {
                            type = "BODY",
                            text = bodyText,
                            example = new
                            {
                                body_text_named_params = new object[]
                                {
                                    new { param_name = "1", example = "Ravi Kumar" },
                                    new { param_name = "2", example = "INV-1025" },
                                    new { param_name = "3", example = "₹1,250" },
                                    new { param_name = "4", example = "Sai Laxmi Salon" },
                                    new { param_name = "5", example = "https://billcom.in/i/sample" }
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Meta template creation returned {StatusCode}: {Content}", response.StatusCode, content);
                    
                    // Parse error if already exists
                    if (content.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    {
                        return new MetaTemplateCreateResponse
                        {
                            Status = "APPROVED",
                            Error = null
                        };
                    }

                    return new MetaTemplateCreateResponse { Error = $"Template creation failed: {response.StatusCode} {content}" };
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return new MetaTemplateCreateResponse
                {
                    Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null,
                    Status = root.TryGetProperty("status", out var stProp) ? stProp.GetString() : "PENDING"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create WABA template via Meta API");
                return new MetaTemplateCreateResponse { Error = ex.Message };
            }
        }

        /// <inheritdoc/>
        public async Task<List<MetaTemplateInfo>> GetWabaTemplatesAsync(string wabaId, string accessToken)
        {
            var result = new List<MetaTemplateInfo>();
            try
            {
                var url = $"https://graph.facebook.com/{_graphApiVersion}/{wabaId}/message_templates";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch WABA templates: {StatusCode} {Content}", response.StatusCode, content);
                    return result;
                }

                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        string? bodyText = null;
                        if (item.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var comp in components.EnumerateArray())
                            {
                                if (comp.TryGetProperty("type", out var t) && t.GetString() == "BODY")
                                {
                                    bodyText = comp.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                                    break;
                                }
                            }
                        }

                        result.Add(new MetaTemplateInfo
                        {
                            Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                            Name = item.TryGetProperty("name", out var name) ? name.GetString() : null,
                            Category = item.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                            Language = item.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                            Status = item.TryGetProperty("status", out var st) ? st.GetString() : null,
                            BodyText = bodyText
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching WABA templates for WABA {WabaId}", wabaId);
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task<MetaSendMessageResponse> SendTextMessageAsync(
            string phoneNumberId, string accessToken, string to, string text)
        {
            var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "text",
                text = new { body = text }
            };

            return await SendMessageAsync(url, accessToken, payload);
        }

        /// <inheritdoc/>
        public async Task<MetaSendMessageResponse> SendDocumentMessageAsync(
            string phoneNumberId, string accessToken, string to, string documentUrl, string? caption, string? filename)
        {
            var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "document",
                document = new
                {
                    link = documentUrl,
                    caption = caption ?? "Invoice",
                    filename = filename ?? "invoice.pdf"
                }
            };

            return await SendMessageAsync(url, accessToken, payload);
        }

        /// <inheritdoc/>
        public async Task<MetaSendMessageResponse> SendTemplateMessageAsync(
            string phoneNumberId, string accessToken, string to, string templateName, Dictionary<string, string>? parameters, string languageCode = "en_US")
        {
            var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/messages";

            var components = new List<object>();
            if (parameters != null && parameters.Count > 0)
            {
                var bodyParams = parameters.Select(p => new { type = "text", text = p.Value }).ToArray();
                components.Add(new { type = "body", parameters = bodyParams });
            }

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = string.IsNullOrEmpty(languageCode) ? "en_US" : languageCode },
                    components = components.Count > 0 ? components : null
                }
            };

            return await SendMessageAsync(url, accessToken, payload);
        }

        private async Task<MetaSendMessageResponse> SendMessageAsync(string url, string accessToken, object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Meta send message failed: {StatusCode} {Content}", response.StatusCode, content);

                    // Parse error details from Meta's response
                    string errorMessage = $"Send failed: {response.StatusCode}";
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("error", out var errorObj))
                        {
                            errorMessage = errorObj.TryGetProperty("message", out var msg)
                                ? msg.GetString() ?? errorMessage
                                : errorMessage;
                        }
                    }
                    catch { /* ignore parse errors, use default message */ }

                    return new MetaSendMessageResponse { Error = errorMessage };
                }

                // Parse message ID from success response
                using var successDoc = JsonDocument.Parse(content);
                string? messageId = null;
                if (successDoc.RootElement.TryGetProperty("messages", out var messages) &&
                    messages.GetArrayLength() > 0)
                {
                    var first = messages[0];
                    messageId = first.TryGetProperty("id", out var id) ? id.GetString() : null;
                }

                _logger.LogInformation("Message sent successfully. MessageId: {MessageId}", messageId);
                return new MetaSendMessageResponse { MessageId = messageId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message via Meta API");
                return new MetaSendMessageResponse { Error = ex.Message };
            }
        }
    }
}
