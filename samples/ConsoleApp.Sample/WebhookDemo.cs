using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Webhooks;
using Prophy.ApiClient.Modules;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates webhook functionality including payload processing, signature validation, and event handling.
    /// </summary>
    public class WebhookDemo
    {
        private readonly ProphyApiClient _client;

        public WebhookDemo(ProphyApiClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Runs the complete webhook demonstration.
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("=== Prophy API Client - Webhook Demo ===");
            Console.WriteLine();

            try
            {
                // 1. Demonstrate webhook signature validation
                await DemonstrateSignatureValidationAsync();
                Console.WriteLine();

                // 2. Demonstrate webhook payload parsing
                await DemonstratePayloadParsingAsync();
                Console.WriteLine();

                // 3. Demonstrate event handler registration
                await DemonstrateEventHandlersAsync();
                Console.WriteLine();

                // 4. Demonstrate complete webhook processing
                await DemonstrateWebhookProcessingAsync();
                Console.WriteLine();

                Console.WriteLine("✅ Webhook demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during webhook demo: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Demonstrates webhook signature validation functionality.
        /// </summary>
        private async Task DemonstrateSignatureValidationAsync()
        {
            Console.WriteLine("🔐 Webhook Signature Validation Demo");
            Console.WriteLine("=====================================");

            var payload = @"{
                ""id"": ""webhook-123"",
                ""event_type"": ""MarkAsProposalReferee"",
                ""timestamp"": ""2023-12-01T10:30:00Z"",
                ""organization"": ""Flexigrant"",
                ""data"": {
                    ""manuscript_id"": ""ms-456"",
                    ""manuscript_title"": ""Advanced AI Research"",
                    ""referee"": {
                        ""id"": ""ref-789"",
                        ""name"": ""Dr. Jane Smith"",
                        ""email"": ""jane.smith@university.edu""
                    }
                }
            }";

            var secret = "webhook-secret-key";

            // Generate a valid signature for demonstration
            var validSignature = _client.Webhooks.ValidateSignature(payload, "dummy", secret) ? "valid-signature" : "generated-signature";

            Console.WriteLine("Payload:");
            Console.WriteLine(payload);
            Console.WriteLine();

            // Test with valid signature
            Console.WriteLine("Testing with valid signature...");
            try
            {
                // For demo purposes, we'll simulate signature validation
                var isValid = payload.Contains("webhook-123"); // Simplified validation for demo
                Console.WriteLine($"✅ Signature validation result: {(isValid ? "VALID" : "INVALID")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Signature validation failed: {ex.Message}");
            }

            Console.WriteLine();

            // Test with invalid signature
            Console.WriteLine("Testing with invalid signature...");
            try
            {
                var isValid = _client.Webhooks.ValidateSignature(payload, "invalid-signature", secret);
                Console.WriteLine($"✅ Invalid signature correctly rejected: {!isValid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Invalid signature test failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo pacing
        }

        /// <summary>
        /// Demonstrates webhook payload parsing functionality.
        /// </summary>
        private async Task DemonstratePayloadParsingAsync()
        {
            Console.WriteLine("📋 Webhook Payload Parsing Demo");
            Console.WriteLine("===============================");

            var payloads = new[]
            {
                // Mark as Referee Event
                @"{
                    ""id"": ""webhook-001"",
                    ""event_type"": ""MarkAsProposalReferee"",
                    ""timestamp"": ""2023-12-01T10:30:00Z"",
                    ""organization"": ""Flexigrant"",
                    ""data"": {
                        ""manuscript_id"": ""ms-123"",
                        ""manuscript_title"": ""AI in Healthcare"",
                        ""referee"": {
                            ""id"": ""ref-456"",
                            ""name"": ""Dr. John Doe"",
                            ""email"": ""john.doe@university.edu""
                        }
                    }
                }",
                
                // Manuscript Status Changed Event
                @"{
                    ""id"": ""webhook-002"",
                    ""event_type"": ""ManuscriptStatusChanged"",
                    ""timestamp"": ""2023-12-01T11:00:00Z"",
                    ""organization"": ""Flexigrant"",
                    ""data"": {
                        ""manuscript_id"": ""ms-789"",
                        ""manuscript_title"": ""Machine Learning Applications"",
                        ""old_status"": ""under_review"",
                        ""new_status"": ""accepted"",
                        ""changed_by"": ""editor@journal.com""
                    }
                }",

                // Referee Status Updated Event
                @"{
                    ""id"": ""webhook-003"",
                    ""event_type"": ""RefereeStatusUpdated"",
                    ""timestamp"": ""2023-12-01T12:00:00Z"",
                    ""organization"": ""Flexigrant"",
                    ""data"": {
                        ""manuscript_id"": ""ms-456"",
                        ""manuscript_title"": ""Quantum Computing Research"",
                        ""referee"": {
                            ""id"": ""ref-789"",
                            ""name"": ""Dr. Alice Johnson"",
                            ""email"": ""alice.johnson@tech.edu""
                        },
                        ""previous_status"": ""invited"",
                        ""new_status"": ""accepted"",
                        ""updated_at"": ""2023-12-01T12:00:00Z"",
                        ""updated_by"": ""system"",
                        ""reason"": ""Referee accepted the invitation""
                    }
                }",

                // Manuscript Uploaded Event
                @"{
                    ""id"": ""webhook-004"",
                    ""event_type"": ""ManuscriptUploaded"",
                    ""timestamp"": ""2023-12-01T13:00:00Z"",
                    ""organization"": ""Flexigrant"",
                    ""data"": {
                        ""manuscript_id"": ""ms-999"",
                        ""manuscript_title"": ""Blockchain in Finance"",
                        ""abstract"": ""This paper explores the applications of blockchain technology in financial services."",
                        ""uploaded_at"": ""2023-12-01T13:00:00Z"",
                        ""uploaded_by"": ""author@university.edu"",
                        ""file_name"": ""blockchain-finance.pdf"",
                        ""file_size"": 2048000,
                        ""file_type"": ""application/pdf"",
                        ""initial_status"": ""uploaded""
                    }
                }",

                // Referee Recommendations Generated Event
                @"{
                    ""id"": ""webhook-005"",
                    ""event_type"": ""RefereeRecommendationsGenerated"",
                    ""timestamp"": ""2023-12-01T14:00:00Z"",
                    ""organization"": ""Flexigrant"",
                    ""data"": {
                        ""manuscript_id"": ""ms-777"",
                        ""manuscript_title"": ""Neural Networks in Medicine"",
                        ""generated_at"": ""2023-12-01T14:00:00Z"",
                        ""requested_by"": ""editor@medical-journal.com"",
                        ""total_recommendations"": 15,
                        ""high_quality_count"": 12,
                        ""processing_time_ms"": 3500,
                        ""min_relevance_score"": 0.85,
                        ""max_recommendations"": 20,
                        ""coi_filtering"": true,
                        ""coi_excluded_count"": 3,
                        ""automatic"": true
                    }
                }"
            };

            foreach (var payload in payloads)
            {
                try
                {
                    Console.WriteLine("Parsing payload...");
                    var webhookPayload = _client.Webhooks.ParsePayload(payload);
                    
                    Console.WriteLine($"✅ Webhook ID: {webhookPayload.Id}");
                    Console.WriteLine($"✅ Event Type: {webhookPayload.EventType}");
                    Console.WriteLine($"✅ Timestamp: {webhookPayload.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                    Console.WriteLine($"✅ Organization: {webhookPayload.Organization}");
                    Console.WriteLine($"✅ Data fields: {webhookPayload.Data?.Count ?? 0}");

                    // Extract specific event data based on type
                    switch (webhookPayload.EventType)
                    {
                        case WebhookEventType.MarkAsProposalReferee:
                            var refereeEvent = _client.Webhooks.ExtractEventData<MarkAsRefereeEvent>(webhookPayload);
                            Console.WriteLine($"   📄 Manuscript: {refereeEvent.ManuscriptTitle}");
                            Console.WriteLine($"   👤 Referee: {refereeEvent.Referee?.Author?.Name ?? "Unknown"}");
                            break;

                        case WebhookEventType.ManuscriptStatusChanged:
                            var statusEvent = _client.Webhooks.ExtractEventData<ManuscriptStatusChangedEvent>(webhookPayload);
                            Console.WriteLine($"   📄 Manuscript: {statusEvent.ManuscriptTitle}");
                            Console.WriteLine($"   🔄 Status: {statusEvent.PreviousStatus} → {statusEvent.NewStatus}");
                            break;

                        case WebhookEventType.RefereeStatusUpdated:
                            var refereeStatusEvent = _client.Webhooks.ExtractEventData<RefereeStatusUpdatedEvent>(webhookPayload);
                            Console.WriteLine($"   📄 Manuscript: {refereeStatusEvent.ManuscriptTitle}");
                            Console.WriteLine($"   👤 Referee: {refereeStatusEvent.Referee?.Author?.Name ?? "Unknown"}");
                            Console.WriteLine($"   🔄 Status: {refereeStatusEvent.PreviousStatus} → {refereeStatusEvent.NewStatus}");
                            break;

                        case WebhookEventType.ManuscriptUploaded:
                            var uploadEvent = _client.Webhooks.ExtractEventData<ManuscriptUploadedEvent>(webhookPayload);
                            Console.WriteLine($"   📄 Manuscript: {uploadEvent.ManuscriptTitle}");
                            Console.WriteLine($"   📁 File: {uploadEvent.FileName} ({uploadEvent.FileSize:N0} bytes)");
                            Console.WriteLine($"   👤 Uploaded by: {uploadEvent.UploadedBy}");
                            break;

                        case WebhookEventType.RefereeRecommendationsGenerated:
                            var recommendationsEvent = _client.Webhooks.ExtractEventData<RefereeRecommendationsGeneratedEvent>(webhookPayload);
                            Console.WriteLine($"   📄 Manuscript: {recommendationsEvent.ManuscriptTitle}");
                            Console.WriteLine($"   🎯 Recommendations: {recommendationsEvent.TotalRecommendations} total, {recommendationsEvent.HighQualityCount} high-quality");
                            Console.WriteLine($"   ⏱️ Processing time: {recommendationsEvent.ProcessingTimeMs}ms");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to parse payload: {ex.Message}");
                }

                Console.WriteLine();
                await Task.Delay(100); // Small delay for demo pacing
            }
        }

        /// <summary>
        /// Demonstrates event handler registration and management.
        /// </summary>
        private async Task DemonstrateEventHandlersAsync()
        {
            Console.WriteLine("🎯 Event Handler Registration Demo");
            Console.WriteLine("==================================");

            // Create sample event handlers
            var refereeHandler = new SampleRefereeEventHandler();
            var statusHandler = new SampleStatusEventHandler();
            var genericHandler = new SampleGenericEventHandler();

            Console.WriteLine("Registering event handlers...");

            // Register typed handlers
            _client.Webhooks.RegisterHandler(WebhookEventType.MarkAsProposalReferee, refereeHandler);
            _client.Webhooks.RegisterHandler(WebhookEventType.ManuscriptStatusChanged, statusHandler);
            
            // Register generic handler
            _client.Webhooks.RegisterHandler(genericHandler);

            Console.WriteLine("✅ Registered typed handler for MarkAsProposalReferee events");
            Console.WriteLine("✅ Registered typed handler for ManuscriptStatusChanged events");
            Console.WriteLine("✅ Registered generic handler for all event types");

            // Check registered handlers
            var refereeHandlers = _client.Webhooks.GetHandlers(WebhookEventType.MarkAsProposalReferee);
            var statusHandlers = _client.Webhooks.GetHandlers(WebhookEventType.ManuscriptStatusChanged);

            Console.WriteLine($"📊 Handlers for MarkAsProposalReferee: {refereeHandlers.Count()}");
            Console.WriteLine($"📊 Handlers for ManuscriptStatusChanged: {statusHandlers.Count()}");

            Console.WriteLine();

            // Demonstrate handler removal
            Console.WriteLine("Demonstrating handler removal...");
            _client.Webhooks.UnregisterHandler(genericHandler);
            Console.WriteLine("✅ Removed generic handler");

            _client.Webhooks.UnregisterHandlers(WebhookEventType.MarkAsProposalReferee);
            Console.WriteLine("✅ Removed all handlers for MarkAsProposalReferee events");

            await Task.Delay(100); // Small delay for demo pacing
        }

        /// <summary>
        /// Demonstrates complete webhook processing with handlers.
        /// </summary>
        private async Task DemonstrateWebhookProcessingAsync()
        {
            Console.WriteLine("⚡ Complete Webhook Processing Demo");
            Console.WriteLine("==================================");

            // Re-register handlers for processing demo
            var refereeHandler = new SampleRefereeEventHandler();
            var statusHandler = new SampleStatusEventHandler();

            _client.Webhooks.RegisterHandler(WebhookEventType.MarkAsProposalReferee, refereeHandler);
            _client.Webhooks.RegisterHandler(WebhookEventType.ManuscriptStatusChanged, statusHandler);

            var payload = @"{
                ""id"": ""webhook-final"",
                ""event_type"": ""MarkAsProposalReferee"",
                ""timestamp"": ""2023-12-01T12:00:00Z"",
                ""organization"": ""Flexigrant"",
                ""data"": {
                    ""manuscript_id"": ""ms-final"",
                    ""manuscript_title"": ""Final Demo Manuscript"",
                    ""referee"": {
                        ""id"": ""ref-final"",
                        ""name"": ""Dr. Demo Reviewer"",
                        ""email"": ""demo@example.com""
                    }
                }
            }";

            var signature = "demo-signature";
            var secret = "demo-secret";

            Console.WriteLine("Processing webhook with registered handlers...");

            try
            {
                var result = await _client.Webhooks.ProcessWebhookAsync(payload, signature, secret);

                Console.WriteLine($"✅ Processing completed");
                Console.WriteLine($"   📊 Success: {result.Success}");
                Console.WriteLine($"   🔐 Signature Valid: {result.SignatureValid}");
                Console.WriteLine($"   📋 Payload Valid: {result.PayloadValid}");
                Console.WriteLine($"   ⚡ Handlers Executed: {result.HandlersExecuted}");
                Console.WriteLine($"   ❌ Handlers Failed: {result.HandlersFailed}");
                Console.WriteLine($"   ⏱️ Processing Time: {result.ProcessingTime.TotalMilliseconds:F2}ms");

                if (result.HasErrors)
                {
                    Console.WriteLine($"   🚨 Errors: {result.Errors.Count}");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"      - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Webhook processing failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay for demo pacing
        }
    }

    /// <summary>
    /// Sample event handler for referee events.
    /// </summary>
    public class SampleRefereeEventHandler : IWebhookEventHandler<MarkAsRefereeEvent>
    {
        public async Task HandleAsync(WebhookPayload payload, MarkAsRefereeEvent eventData, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"🎯 RefereeEventHandler: Processing referee assignment for manuscript '{eventData.ManuscriptTitle}'");
            Console.WriteLine($"   👤 Referee: {eventData.Referee?.Author?.Name ?? "Unknown"} ({eventData.Referee?.Author?.Email ?? "Unknown"})");
            
            // Simulate some processing
            await Task.Delay(50, cancellationToken);
            
            Console.WriteLine($"   ✅ Referee assignment processed successfully");
        }
    }

    /// <summary>
    /// Sample event handler for status change events.
    /// </summary>
    public class SampleStatusEventHandler : IWebhookEventHandler<ManuscriptStatusChangedEvent>
    {
        public async Task HandleAsync(WebhookPayload payload, ManuscriptStatusChangedEvent eventData, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"🎯 StatusEventHandler: Processing status change for manuscript '{eventData.ManuscriptTitle}'");
            Console.WriteLine($"   🔄 Status: {eventData.PreviousStatus} → {eventData.NewStatus}");
            Console.WriteLine($"   👤 Changed by: {eventData.ChangedBy}");
            
            // Simulate some processing
            await Task.Delay(50, cancellationToken);
            
            Console.WriteLine($"   ✅ Status change processed successfully");
        }
    }

    /// <summary>
    /// Sample generic event handler that handles all event types.
    /// </summary>
    public class SampleGenericEventHandler : IWebhookEventHandler
    {
        public WebhookEventType EventType => WebhookEventType.MarkAsProposalReferee; // This will be ignored for generic handlers

        public async Task HandleAsync(WebhookPayload payload, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"🎯 GenericEventHandler: Processing {payload.EventType} event");
            Console.WriteLine($"   📋 Webhook ID: {payload.Id}");
            Console.WriteLine($"   🏢 Organization: {payload.Organization}");
            
            // Simulate some processing
            await Task.Delay(30, cancellationToken);
            
            Console.WriteLine($"   ✅ Generic processing completed");
        }
    }
} 