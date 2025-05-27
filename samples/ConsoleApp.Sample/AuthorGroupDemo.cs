using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prophy.ApiClient;
using Prophy.ApiClient.Models.Requests;
using Prophy.ApiClient.Exceptions;

namespace ConsoleApp.Sample
{
    /// <summary>
    /// Demonstrates author group management functionality of the Prophy API Client.
    /// Shows various ways to create, manage, and work with author groups and their members.
    /// </summary>
    public class AuthorGroupDemo
    {
        private readonly ProphyApiClient _client;
        private readonly ILogger<AuthorGroupDemo> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthorGroupDemo class.
        /// </summary>
        /// <param name="client">The Prophy API client instance.</param>
        /// <param name="logger">The logger for recording demo operations.</param>
        public AuthorGroupDemo(ProphyApiClient client, ILogger<AuthorGroupDemo> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all author group management demonstrations.
        /// </summary>
        public async Task RunAllDemosAsync()
        {
            _logger.LogInformation("👥 Starting Author Group Management API Demonstrations");
            _logger.LogInformation("======================================================");
            Console.WriteLine();

            try
            {
                // Demo 1: Create author groups
                await DemoCreateAuthorGroups();
                Console.WriteLine();

                // Demo 2: Retrieve and list author groups
                await DemoRetrieveAuthorGroups();
                Console.WriteLine();

                // Demo 3: Update author groups
                await DemoUpdateAuthorGroups();
                Console.WriteLine();

                // Demo 4: Add authors to groups
                await DemoAddAuthorsToGroups();
                Console.WriteLine();

                // Demo 5: Manage authors within groups
                await DemoManageAuthorsInGroups();
                Console.WriteLine();

                // Demo 6: Search author groups
                await DemoSearchAuthorGroups();
                Console.WriteLine();

                // Demo 7: Advanced group operations
                await DemoAdvancedGroupOperations();
                Console.WriteLine();

                // Demo 8: Error handling demonstration
                await DemoErrorHandling();
                Console.WriteLine();

                _logger.LogInformation("✅ All author group management demonstrations completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during author group management demonstrations");
                throw;
            }
        }

        /// <summary>
        /// Demonstrates creating author groups with different configurations.
        /// </summary>
        private async Task DemoCreateAuthorGroups()
        {
            _logger.LogInformation("📋 Demo 1: Creating Author Groups");
            _logger.LogInformation("----------------------------------");

            try
            {
                // Create a basic author group
                var basicGroupRequest = new CreateAuthorGroupRequest
                {
                    GroupName = "Physics Reviewers 2024",
                    Description = "Expert reviewers for physics manuscripts in 2024",
                    OwnerTeam = "Editorial Board",
                    EditorTeams = new List<string> { "Senior Editors", "Associate Editors" },
                    ViewerTeams = new List<string> { "Review Committee", "Quality Assurance" },
                    Metadata = new Dictionary<string, object>
                    {
                        ["subject_area"] = "Physics",
                        ["year"] = 2024,
                        ["expertise_level"] = "Expert"
                    }
                };

                _logger.LogInformation("Creating basic author group: {GroupName}", basicGroupRequest.GroupName);
                Console.WriteLine($"📝 Creating group: {basicGroupRequest.GroupName}");
                Console.WriteLine($"   Description: {basicGroupRequest.Description}");
                Console.WriteLine($"   Owner Team: {basicGroupRequest.OwnerTeam}");
                Console.WriteLine($"   Editor Teams: {string.Join(", ", basicGroupRequest.EditorTeams)}");
                Console.WriteLine($"   Viewer Teams: {string.Join(", ", basicGroupRequest.ViewerTeams)}");

                // Note: In a real scenario, this would make an API call
                // For demo purposes, we'll simulate the response
                try
                {
                    var basicGroupResponse = await _client.AuthorGroups.CreateAsync(basicGroupRequest);
                    _logger.LogInformation("✅ Successfully created author group with ID: {GroupId}", basicGroupResponse.Data?.Id);
                    Console.WriteLine($"✅ Group created successfully with ID: {basicGroupResponse.Data?.Id}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would create group: {basicGroupRequest.GroupName}");
                }

                Console.WriteLine();

                // Create a specialized author group
                var specializedGroupRequest = new CreateAuthorGroupRequest
                {
                    GroupName = "Machine Learning Experts",
                    Description = "Specialists in machine learning and artificial intelligence research",
                    OwnerTeam = "AI Research Division",
                    EditorTeams = new List<string> { "ML Editors" },
                    ViewerTeams = new List<string> { "Research Committee" },
                    Metadata = new Dictionary<string, object>
                    {
                        ["subject_area"] = "Computer Science",
                        ["specialization"] = "Machine Learning",
                        ["min_h_index"] = 15,
                        ["required_publications"] = 20
                    }
                };

                _logger.LogInformation("Creating specialized author group: {GroupName}", specializedGroupRequest.GroupName);
                Console.WriteLine($"📝 Creating specialized group: {specializedGroupRequest.GroupName}");
                Console.WriteLine($"   Specialization: Machine Learning & AI");
                Console.WriteLine($"   Minimum H-Index: 15");
                Console.WriteLine($"   Required Publications: 20+");

                try
                {
                    var specializedGroupResponse = await _client.AuthorGroups.CreateAsync(specializedGroupRequest);
                    _logger.LogInformation("✅ Successfully created specialized group with ID: {GroupId}", specializedGroupResponse.Data?.Id);
                    Console.WriteLine($"✅ Specialized group created with ID: {specializedGroupResponse.Data?.Id}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would create group: {specializedGroupRequest.GroupName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during author group creation demo");
                Console.WriteLine($"❌ Error creating author groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates retrieving and listing author groups.
        /// </summary>
        private async Task DemoRetrieveAuthorGroups()
        {
            _logger.LogInformation("📋 Demo 2: Retrieving Author Groups");
            _logger.LogInformation("------------------------------------");

            try
            {
                // Get all author groups
                _logger.LogInformation("Retrieving all author groups...");
                Console.WriteLine("📋 Retrieving all author groups...");

                try
                {
                    var allGroups = await _client.AuthorGroups.GetAllAsync(page: 1, pageSize: 10, includeInactive: false);
                    _logger.LogInformation("✅ Retrieved {Count} author groups", allGroups.Data?.Count ?? 0);
                    Console.WriteLine($"✅ Found {allGroups.Data?.Count ?? 0} active author groups");
                    
                    if (allGroups.Data?.Any() == true)
                    {
                        Console.WriteLine("   Groups:");
                        foreach (var group in allGroups.Data.Take(3))
                        {
                            Console.WriteLine($"   - {group.GroupName} (ID: {group.Id}, Authors: {group.AuthorCount})");
                            Console.WriteLine($"     Owner: {group.OwnerTeam}");
                            if (group.EditorTeams?.Any() == true)
                                Console.WriteLine($"     Editors: {string.Join(", ", group.EditorTeams)}");
                        }
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would retrieve author groups");
                }

                Console.WriteLine();

                // Get a specific author group by ID
                var groupId = "sample-group-id-123";
                _logger.LogInformation("Retrieving specific author group: {GroupId}", groupId);
                Console.WriteLine($"📋 Retrieving group by ID: {groupId}");

                try
                {
                    var specificGroup = await _client.AuthorGroups.GetByIdAsync(groupId, includeAuthors: true);
                    _logger.LogInformation("✅ Retrieved author group: {GroupName}", specificGroup.Data?.GroupName);
                    Console.WriteLine($"✅ Retrieved group: {specificGroup.Data?.GroupName}");
                    Console.WriteLine($"   Description: {specificGroup.Data?.Description}");
                    Console.WriteLine($"   Author Count: {specificGroup.Data?.AuthorCount}");
                    Console.WriteLine($"   Created: {specificGroup.Data?.CreatedAt:yyyy-MM-dd}");
                    
                    if (specificGroup.Data?.Authors?.Any() == true)
                    {
                        Console.WriteLine("   Authors included in response:");
                        foreach (var author in specificGroup.Data.Authors.Take(3))
                        {
                            Console.WriteLine($"   - {author.Name} ({author.Affiliation})");
                        }
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would retrieve group: {groupId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during author group retrieval demo");
                Console.WriteLine($"❌ Error retrieving author groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates updating author groups.
        /// </summary>
        private async Task DemoUpdateAuthorGroups()
        {
            _logger.LogInformation("📋 Demo 3: Updating Author Groups");
            _logger.LogInformation("----------------------------------");

            try
            {
                var groupId = "sample-group-id-123";
                var updateRequest = new UpdateAuthorGroupRequest
                {
                    Description = "Updated description for the author group",
                    EditorTeams = new List<string> { "Senior Editors", "Associate Editors", "Guest Editors" },
                    ViewerTeams = new List<string> { "Review Committee", "Quality Assurance", "External Reviewers" },
                    Metadata = new Dictionary<string, object>
                    {
                        ["last_updated"] = DateTime.UtcNow,
                        ["update_reason"] = "Added new editor and viewer teams"
                    }
                };

                _logger.LogInformation("Updating author group: {GroupId}", groupId);
                Console.WriteLine($"📝 Updating group: {groupId}");
                Console.WriteLine($"   New Description: {updateRequest.Description}");
                Console.WriteLine($"   Updated Editor Teams: {string.Join(", ", updateRequest.EditorTeams ?? new List<string>())}");
                Console.WriteLine($"   Updated Viewer Teams: {string.Join(", ", updateRequest.ViewerTeams ?? new List<string>())}");

                try
                {
                    var updatedGroup = await _client.AuthorGroups.UpdateAsync(groupId, updateRequest);
                    _logger.LogInformation("✅ Successfully updated author group: {GroupName}", updatedGroup.Data?.GroupName);
                    Console.WriteLine($"✅ Group updated successfully: {updatedGroup.Data?.GroupName}");
                    Console.WriteLine($"   Updated at: {updatedGroup.Data?.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would update group: {groupId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during author group update demo");
                Console.WriteLine($"❌ Error updating author groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates adding authors to groups.
        /// </summary>
        private async Task DemoAddAuthorsToGroups()
        {
            _logger.LogInformation("📋 Demo 4: Adding Authors to Groups");
            _logger.LogInformation("------------------------------------");

            try
            {
                var groupId = "sample-group-id-123";
                var clientId = "client-author-001";

                // Create a comprehensive author request
                var authorRequest = new AuthorFromGroupRequest
                {
                    Name = "Dr. Jane Smith",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Emails = new List<string> { "jane.smith@university.edu", "j.smith@research.org" },
                    Orcids = new List<string> { "0000-0000-0000-0001" },
                    Affiliations = new List<string> { "University of Science", "Research Institute" },
                    PrimaryAffiliation = "University of Science",
                    Country = "United States",
                    ResearchAreas = new List<string> { "Machine Learning", "Artificial Intelligence", "Data Science" },
                    HIndex = 25,
                    CitationCount = 1500,
                    PublicationCount = 45,
                    IsActive = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["expertise_level"] = "Senior",
                        ["years_experience"] = 15,
                        ["preferred_topics"] = new[] { "Deep Learning", "Neural Networks" }
                    }
                };

                _logger.LogInformation("Adding author to group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"👤 Adding author to group: {groupId}");
                Console.WriteLine($"   Client ID: {clientId}");
                Console.WriteLine($"   Author: {authorRequest.Name}");
                Console.WriteLine($"   Emails: {string.Join(", ", authorRequest.Emails)}");
                Console.WriteLine($"   ORCID: {string.Join(", ", authorRequest.Orcids)}");
                Console.WriteLine($"   Primary Affiliation: {authorRequest.PrimaryAffiliation}");
                Console.WriteLine($"   Research Areas: {string.Join(", ", authorRequest.ResearchAreas)}");
                Console.WriteLine($"   H-Index: {authorRequest.HIndex}, Citations: {authorRequest.CitationCount}");

                try
                {
                    var addedAuthor = await _client.AuthorGroups.AddAuthorAsync(groupId, clientId, authorRequest);
                    _logger.LogInformation("✅ Successfully added author: {AuthorName}", addedAuthor.Data?.Name);
                    Console.WriteLine($"✅ Author added successfully: {addedAuthor.Data?.Name}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would add author: {authorRequest.Name}");
                }

                Console.WriteLine();

                // Add another author with different profile
                var clientId2 = "client-author-002";
                var authorRequest2 = new AuthorFromGroupRequest
                {
                    Name = "Prof. Michael Johnson",
                    FirstName = "Michael",
                    LastName = "Johnson",
                    Emails = new List<string> { "m.johnson@tech.edu" },
                    Orcids = new List<string> { "0000-0000-0000-0002" },
                    Affiliations = new List<string> { "Technology Institute" },
                    PrimaryAffiliation = "Technology Institute",
                    Country = "Canada",
                    ResearchAreas = new List<string> { "Computer Vision", "Robotics" },
                    HIndex = 35,
                    CitationCount = 2800,
                    PublicationCount = 78,
                    IsActive = true
                };

                _logger.LogInformation("Adding second author to group: {GroupId}, Client ID: {ClientId}", groupId, clientId2);
                Console.WriteLine($"👤 Adding second author to group: {groupId}");
                Console.WriteLine($"   Author: {authorRequest2.Name}");
                Console.WriteLine($"   Specialization: Computer Vision & Robotics");

                try
                {
                    var addedAuthor2 = await _client.AuthorGroups.AddAuthorAsync(groupId, clientId2, authorRequest2);
                    _logger.LogInformation("✅ Successfully added second author: {AuthorName}", addedAuthor2.Data?.Name);
                    Console.WriteLine($"✅ Second author added successfully: {addedAuthor2.Data?.Name}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would add author: {authorRequest2.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during add authors demo");
                Console.WriteLine($"❌ Error adding authors to groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates managing authors within groups.
        /// </summary>
        private async Task DemoManageAuthorsInGroups()
        {
            _logger.LogInformation("📋 Demo 5: Managing Authors in Groups");
            _logger.LogInformation("-------------------------------------");

            try
            {
                var groupId = "sample-group-id-123";
                var clientId = "client-author-001";

                // Get a specific author from the group
                _logger.LogInformation("Retrieving author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"👤 Retrieving author from group: {groupId}");
                Console.WriteLine($"   Client ID: {clientId}");

                try
                {
                    var retrievedAuthor = await _client.AuthorGroups.GetAuthorAsync(groupId, clientId);
                    _logger.LogInformation("✅ Retrieved author: {AuthorName}", retrievedAuthor.Data?.Name);
                    Console.WriteLine($"✅ Retrieved author: {retrievedAuthor.Data?.Name}");
                    Console.WriteLine($"   Primary Affiliation: {retrievedAuthor.Data?.Affiliation}");
                    Console.WriteLine($"   H-Index: {retrievedAuthor.Data?.HIndex}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would retrieve author: {clientId}");
                }

                Console.WriteLine();

                // Update the author's information
                var updateAuthorRequest = new AuthorFromGroupRequest
                {
                    Name = "Dr. Jane Smith-Wilson",
                    FirstName = "Jane",
                    LastName = "Smith-Wilson",
                    Emails = new List<string> { "jane.smith-wilson@university.edu", "j.wilson@research.org" },
                    Orcids = new List<string> { "0000-0000-0000-0001" },
                    Affiliations = new List<string> { "University of Science", "Advanced Research Institute" },
                    PrimaryAffiliation = "Advanced Research Institute",
                    Country = "United States",
                    ResearchAreas = new List<string> { "Machine Learning", "Artificial Intelligence", "Quantum Computing" },
                    HIndex = 28,
                    CitationCount = 1750,
                    PublicationCount = 52,
                    IsActive = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["expertise_level"] = "Senior",
                        ["years_experience"] = 16,
                        ["recent_promotion"] = "Full Professor"
                    }
                };

                _logger.LogInformation("Updating author in group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"📝 Updating author in group: {groupId}");
                Console.WriteLine($"   Updated Name: {updateAuthorRequest.Name}");
                Console.WriteLine($"   New Primary Affiliation: {updateAuthorRequest.PrimaryAffiliation}");
                Console.WriteLine($"   Updated H-Index: {updateAuthorRequest.HIndex}");
                Console.WriteLine($"   New Research Area: Quantum Computing");

                try
                {
                    var updatedAuthor = await _client.AuthorGroups.UpdateAuthorAsync(groupId, clientId, updateAuthorRequest);
                    _logger.LogInformation("✅ Successfully updated author: {AuthorName}", updatedAuthor.Data?.Name);
                    Console.WriteLine($"✅ Author updated successfully: {updatedAuthor.Data?.Name}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would update author: {updateAuthorRequest.Name}");
                }

                Console.WriteLine();

                // Get all authors in the group
                _logger.LogInformation("Retrieving all authors in group: {GroupId}", groupId);
                Console.WriteLine($"👥 Retrieving all authors in group: {groupId}");

                try
                {
                    var allAuthors = await _client.AuthorGroups.GetAuthorsAsync(groupId, page: 1, pageSize: 10, includeInactive: false);
                    _logger.LogInformation("✅ Retrieved {Count} authors from group", allAuthors?.Count ?? 0);
                    Console.WriteLine($"✅ Found {allAuthors?.Count ?? 0} authors in group");
                    
                    if (allAuthors?.Any() == true)
                    {
                        Console.WriteLine("   Authors:");
                        foreach (var author in allAuthors.Take(5))
                        {
                            Console.WriteLine($"   - {author.Name} (H-Index: {author.HIndex}, Citations: {author.CitationCount})");
                            Console.WriteLine($"     Affiliation: {author.Affiliation}");
                        }
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would retrieve all authors from group: {groupId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manage authors demo");
                Console.WriteLine($"❌ Error managing authors in groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates searching for author groups.
        /// </summary>
        private async Task DemoSearchAuthorGroups()
        {
            _logger.LogInformation("📋 Demo 6: Searching Author Groups");
            _logger.LogInformation("-----------------------------------");

            try
            {
                var searchTerms = new[] { "Physics", "Machine Learning", "Biology" };

                foreach (var searchTerm in searchTerms)
                {
                    _logger.LogInformation("Searching for author groups with term: {SearchTerm}", searchTerm);
                    Console.WriteLine($"🔍 Searching for groups containing: '{searchTerm}'");

                    try
                    {
                        var searchResults = await _client.AuthorGroups.SearchAsync(searchTerm, page: 1, pageSize: 5);
                        _logger.LogInformation("✅ Found {Count} groups matching '{SearchTerm}'", searchResults.Data?.Count ?? 0, searchTerm);
                        Console.WriteLine($"✅ Found {searchResults.Data?.Count ?? 0} groups matching '{searchTerm}'");
                        
                        if (searchResults.Data?.Any() == true)
                        {
                            foreach (var group in searchResults.Data)
                            {
                                Console.WriteLine($"   - {group.GroupName} (ID: {group.Id})");
                                Console.WriteLine($"     Description: {group.Description}");
                                Console.WriteLine($"     Authors: {group.AuthorCount}, Owner: {group.OwnerTeam}");
                            }
                        }

                        // Display pagination info if available
                        if (searchResults.Pagination != null)
                        {
                            Console.WriteLine($"   Page {searchResults.Pagination.Page} of {searchResults.Pagination.TotalPages}");
                            Console.WriteLine($"   Total results: {searchResults.Pagination.TotalCount}");
                        }
                    }
                    catch (ProphyApiException ex)
                    {
                        _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                        Console.WriteLine($"⚠️ API call simulation - would search for: '{searchTerm}'");
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search author groups demo");
                Console.WriteLine($"❌ Error searching author groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates advanced group operations.
        /// </summary>
        private async Task DemoAdvancedGroupOperations()
        {
            _logger.LogInformation("📋 Demo 7: Advanced Group Operations");
            _logger.LogInformation("-------------------------------------");

            try
            {
                // Create a temporary group for deletion demo
                var tempGroupRequest = new CreateAuthorGroupRequest
                {
                    GroupName = "Temporary Test Group",
                    Description = "This group will be deleted as part of the demo",
                    OwnerTeam = "Demo Team",
                    EditorTeams = new List<string> { "Test Editors" },
                    Metadata = new Dictionary<string, object>
                    {
                        ["temporary"] = true,
                        ["demo_purpose"] = "deletion_test"
                    }
                };

                _logger.LogInformation("Creating temporary group for deletion demo: {GroupName}", tempGroupRequest.GroupName);
                Console.WriteLine($"📝 Creating temporary group for deletion demo: {tempGroupRequest.GroupName}");

                string? tempGroupId = null;
                try
                {
                    var tempGroupResponse = await _client.AuthorGroups.CreateAsync(tempGroupRequest);
                    tempGroupId = tempGroupResponse.Data?.Id;
                    _logger.LogInformation("✅ Created temporary group with ID: {GroupId}", tempGroupId);
                    Console.WriteLine($"✅ Temporary group created with ID: {tempGroupId}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would create temporary group");
                    tempGroupId = "temp-group-demo-123"; // Use a demo ID for the deletion example
                }

                Console.WriteLine();

                // Delete the temporary group
                if (!string.IsNullOrEmpty(tempGroupId))
                {
                    _logger.LogInformation("Deleting temporary group: {GroupId}", tempGroupId);
                    Console.WriteLine($"🗑️ Deleting temporary group: {tempGroupId}");

                    try
                    {
                        await _client.AuthorGroups.DeleteAsync(tempGroupId);
                        _logger.LogInformation("✅ Successfully deleted temporary group: {GroupId}", tempGroupId);
                        Console.WriteLine($"✅ Temporary group deleted successfully: {tempGroupId}");
                    }
                    catch (ProphyApiException ex)
                    {
                        _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                        Console.WriteLine($"⚠️ API call simulation - would delete group: {tempGroupId}");
                    }
                }

                Console.WriteLine();

                // Delete an author from a group
                var groupId = "sample-group-id-123";
                var clientId = "client-author-002";
                _logger.LogInformation("Removing author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"👤 Removing author from group: {groupId}");
                Console.WriteLine($"   Client ID: {clientId}");

                try
                {
                    await _client.AuthorGroups.DeleteAuthorAsync(groupId, clientId);
                    _logger.LogInformation("✅ Successfully removed author from group: {ClientId}", clientId);
                    Console.WriteLine($"✅ Author removed successfully: {clientId}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine($"⚠️ API call simulation - would remove author: {clientId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during advanced group operations demo");
                Console.WriteLine($"❌ Error in advanced group operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates error handling scenarios.
        /// </summary>
        private async Task DemoErrorHandling()
        {
            _logger.LogInformation("📋 Demo 8: Error Handling");
            _logger.LogInformation("--------------------------");

            try
            {
                // Demonstrate validation errors
                Console.WriteLine("🚫 Testing validation errors...");
                
                try
                {
                    var invalidRequest = new CreateAuthorGroupRequest
                    {
                        GroupName = "", // Invalid: empty name
                        OwnerTeam = "Valid Team"
                    };
                    
                    await _client.AuthorGroups.CreateAsync(invalidRequest);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("✅ Caught expected validation error: {Message}", ex.Message);
                    Console.WriteLine($"✅ Validation error caught: {ex.Message}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogInformation("✅ Caught expected API error: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                    Console.WriteLine($"✅ API error caught: {ex.ErrorCode} - {ex.Message}");
                    if (ex.HttpStatusCode.HasValue)
                        Console.WriteLine($"   HTTP Status: {ex.HttpStatusCode}");
                }

                Console.WriteLine();

                // Demonstrate not found errors
                Console.WriteLine("🚫 Testing not found errors...");
                
                try
                {
                    await _client.AuthorGroups.GetByIdAsync("non-existent-group-id");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogInformation("✅ Caught expected not found error: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                    Console.WriteLine($"✅ Not found error caught: {ex.ErrorCode} - {ex.Message}");
                    if (ex.HttpStatusCode.HasValue)
                        Console.WriteLine($"   HTTP Status: {ex.HttpStatusCode}");
                }

                Console.WriteLine();

                // Demonstrate parameter validation
                Console.WriteLine("🚫 Testing parameter validation...");
                
                try
                {
                    await _client.AuthorGroups.GetAllAsync(page: 0, pageSize: 2000); // Invalid parameters
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("✅ Caught expected parameter validation error: {Message}", ex.Message);
                    Console.WriteLine($"✅ Parameter validation error caught: {ex.Message}");
                }

                _logger.LogInformation("✅ Error handling demonstration completed successfully");
                Console.WriteLine("✅ Error handling demonstration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during error handling demo");
                Console.WriteLine($"❌ Unexpected error during error handling demo: {ex.Message}");
            }
        }
    }
} 