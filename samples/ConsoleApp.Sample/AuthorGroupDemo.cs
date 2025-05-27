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
                    EditorTeams = new[] { "Senior Editors", "Associate Editors" },
                    ViewerTeams = new[] { "Review Committee", "Quality Assurance" },
                    IsActive = true,
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
                    EditorTeams = new[] { "ML Editors" },
                    ViewerTeams = new[] { "Research Committee" },
                    IsActive = true,
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
                            Console.WriteLine($"   • {group.GroupName} (ID: {group.Id})");
                            Console.WriteLine($"     Authors: {group.AuthorCount}, Active: {group.IsActive}");
                        }
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would retrieve groups list");
                    Console.WriteLine("   Sample groups:");
                    Console.WriteLine("   • Physics Reviewers 2024 (ID: group_001)");
                    Console.WriteLine("     Authors: 25, Active: True");
                    Console.WriteLine("   • Machine Learning Experts (ID: group_002)");
                    Console.WriteLine("     Authors: 18, Active: True");
                }

                Console.WriteLine();

                // Get a specific author group by ID
                var groupId = "group_001";
                _logger.LogInformation("Retrieving specific author group: {GroupId}", groupId);
                Console.WriteLine($"🔍 Retrieving specific group: {groupId}");

                try
                {
                    var specificGroup = await _client.AuthorGroups.GetByIdAsync(groupId, includeAuthors: true);
                    _logger.LogInformation("✅ Retrieved group: {GroupName}", specificGroup.Data?.GroupName);
                    Console.WriteLine($"✅ Retrieved group: {specificGroup.Data?.GroupName}");
                    Console.WriteLine($"   Description: {specificGroup.Data?.Description}");
                    Console.WriteLine($"   Owner: {specificGroup.Data?.OwnerTeam}");
                    Console.WriteLine($"   Authors: {specificGroup.Data?.AuthorCount}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would retrieve specific group");
                    Console.WriteLine("   Group: Physics Reviewers 2024");
                    Console.WriteLine("   Description: Expert reviewers for physics manuscripts in 2024");
                    Console.WriteLine("   Owner: Editorial Board");
                    Console.WriteLine("   Authors: 25");
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
                var groupId = "group_001";
                
                // Update group description and teams
                var updateRequest = new UpdateAuthorGroupRequest
                {
                    Description = "Updated: Expert reviewers for physics manuscripts in 2024 - Now including quantum physics specialists",
                    EditorTeams = new[] { "Senior Editors", "Associate Editors", "Quantum Physics Editors" },
                    ViewerTeams = new[] { "Review Committee", "Quality Assurance", "Research Board" },
                    Metadata = new Dictionary<string, object>
                    {
                        ["subject_area"] = "Physics",
                        ["year"] = 2024,
                        ["expertise_level"] = "Expert",
                        ["specializations"] = new[] { "General Physics", "Quantum Physics", "Theoretical Physics" },
                        ["last_updated"] = DateTime.UtcNow
                    }
                };

                _logger.LogInformation("Updating author group: {GroupId}", groupId);
                Console.WriteLine($"📝 Updating group: {groupId}");
                Console.WriteLine($"   New description: {updateRequest.Description}");
                Console.WriteLine($"   Added editor team: Quantum Physics Editors");
                Console.WriteLine($"   Added viewer team: Research Board");
                Console.WriteLine($"   Added specializations metadata");

                try
                {
                    var updatedGroup = await _client.AuthorGroups.UpdateAsync(groupId, updateRequest);
                    _logger.LogInformation("✅ Successfully updated author group: {GroupId}", groupId);
                    Console.WriteLine($"✅ Group updated successfully");
                    Console.WriteLine($"   Updated at: {updatedGroup.Data?.UpdatedAt}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would update group successfully");
                }

                Console.WriteLine();

                // Partial update example
                var partialUpdateRequest = new UpdateAuthorGroupRequest
                {
                    IsActive = false // Deactivate the group
                };

                _logger.LogInformation("Performing partial update (deactivating group): {GroupId}", groupId);
                Console.WriteLine($"🔄 Performing partial update - deactivating group: {groupId}");

                try
                {
                    var partiallyUpdatedGroup = await _client.AuthorGroups.UpdateAsync(groupId, partialUpdateRequest);
                    _logger.LogInformation("✅ Successfully deactivated author group: {GroupId}", groupId);
                    Console.WriteLine($"✅ Group deactivated successfully");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would deactivate group");
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
                var groupId = "group_001";
                
                // Add a comprehensive author profile
                var authorRequest = new AuthorFromGroupRequest
                {
                    Name = "Dr. Sarah Johnson",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    MiddleName = "Elizabeth",
                    Emails = new[] { "sarah.johnson@university.edu", "s.johnson@research.org" },
                    Orcids = new[] { "0000-0002-1234-5678" },
                    Affiliations = new[] { "MIT Physics Department", "Quantum Research Institute" },
                    Countries = new[] { "United States" },
                    Keywords = new[] { "quantum physics", "theoretical physics", "particle physics", "quantum mechanics" },
                    HIndex = 42,
                    CitationCount = 1250,
                    PublicationCount = 85,
                    IsActive = true,
                    CustomFields = new Dictionary<string, object>
                    {
                        ["expertise_level"] = "Senior",
                        ["years_experience"] = 15,
                        ["preferred_subjects"] = new[] { "Quantum Mechanics", "Particle Physics" },
                        ["languages"] = new[] { "English", "German", "French" },
                        ["availability"] = "High"
                    }
                };

                var clientId = "author_001";
                
                _logger.LogInformation("Adding author to group: {GroupId}, Author: {AuthorName}", groupId, authorRequest.Name);
                Console.WriteLine($"👤 Adding author to group: {groupId}");
                Console.WriteLine($"   Name: {authorRequest.Name}");
                Console.WriteLine($"   Email: {authorRequest.Emails?.FirstOrDefault()}");
                Console.WriteLine($"   ORCID: {authorRequest.Orcids?.FirstOrDefault()}");
                Console.WriteLine($"   Affiliation: {authorRequest.Affiliations?.FirstOrDefault()}");
                Console.WriteLine($"   H-Index: {authorRequest.HIndex}");
                Console.WriteLine($"   Citations: {authorRequest.CitationCount}");
                Console.WriteLine($"   Publications: {authorRequest.PublicationCount}");
                Console.WriteLine($"   Keywords: {string.Join(", ", authorRequest.Keywords ?? new string[0])}");

                try
                {
                    var addedAuthor = await _client.AuthorGroups.AddAuthorAsync(groupId, clientId, authorRequest);
                    _logger.LogInformation("✅ Successfully added author to group: {AuthorName}", authorRequest.Name);
                    Console.WriteLine($"✅ Author added successfully");
                    Console.WriteLine($"   Author ID: {addedAuthor.Data?.Id}");
                    Console.WriteLine($"   Group: {addedAuthor.GroupName}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would add author successfully");
                }

                Console.WriteLine();

                // Add another author with minimal information
                var minimalAuthorRequest = new AuthorFromGroupRequest
                {
                    Name = "Prof. Michael Chen",
                    Emails = new[] { "m.chen@university.edu" },
                    Affiliations = new[] { "Stanford University" },
                    Keywords = new[] { "machine learning", "artificial intelligence" },
                    IsActive = true
                };

                var clientId2 = "author_002";
                
                _logger.LogInformation("Adding minimal author profile: {AuthorName}", minimalAuthorRequest.Name);
                Console.WriteLine($"👤 Adding minimal author profile: {minimalAuthorRequest.Name}");
                Console.WriteLine($"   Email: {minimalAuthorRequest.Emails?.FirstOrDefault()}");
                Console.WriteLine($"   Affiliation: {minimalAuthorRequest.Affiliations?.FirstOrDefault()}");

                try
                {
                    var addedMinimalAuthor = await _client.AuthorGroups.AddAuthorAsync(groupId, clientId2, minimalAuthorRequest);
                    _logger.LogInformation("✅ Successfully added minimal author to group: {AuthorName}", minimalAuthorRequest.Name);
                    Console.WriteLine($"✅ Minimal author added successfully");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would add minimal author successfully");
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
                var groupId = "group_001";
                var clientId = "author_001";

                // Get author from group
                _logger.LogInformation("Retrieving author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"🔍 Retrieving author from group: {groupId}");

                try
                {
                    var retrievedAuthor = await _client.AuthorGroups.GetAuthorAsync(groupId, clientId);
                    _logger.LogInformation("✅ Retrieved author: {AuthorName}", retrievedAuthor.Data?.Name);
                    Console.WriteLine($"✅ Retrieved author: {retrievedAuthor.Data?.Name}");
                    Console.WriteLine($"   Email: {retrievedAuthor.Data?.Emails?.FirstOrDefault()}");
                    Console.WriteLine($"   H-Index: {retrievedAuthor.Data?.HIndex}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would retrieve author: Dr. Sarah Johnson");
                }

                Console.WriteLine();

                // Update author information
                var updateAuthorRequest = new AuthorFromGroupRequest
                {
                    Name = "Dr. Sarah Johnson",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    MiddleName = "Elizabeth",
                    Emails = new[] { "sarah.johnson@university.edu", "s.johnson@research.org", "sarah.j@newaffiliation.edu" },
                    Orcids = new[] { "0000-0002-1234-5678" },
                    Affiliations = new[] { "MIT Physics Department", "Quantum Research Institute", "New Quantum Lab" },
                    Countries = new[] { "United States" },
                    Keywords = new[] { "quantum physics", "theoretical physics", "particle physics", "quantum mechanics", "quantum computing" },
                    HIndex = 45, // Updated H-Index
                    CitationCount = 1350, // Updated citation count
                    PublicationCount = 92, // Updated publication count
                    IsActive = true,
                    CustomFields = new Dictionary<string, object>
                    {
                        ["expertise_level"] = "Senior",
                        ["years_experience"] = 16, // Updated
                        ["preferred_subjects"] = new[] { "Quantum Mechanics", "Particle Physics", "Quantum Computing" },
                        ["languages"] = new[] { "English", "German", "French" },
                        ["availability"] = "High",
                        ["recent_awards"] = new[] { "Excellence in Physics Research 2024" }
                    }
                };

                _logger.LogInformation("Updating author information: {ClientId}", clientId);
                Console.WriteLine($"📝 Updating author information: {clientId}");
                Console.WriteLine($"   Added new affiliation: New Quantum Lab");
                Console.WriteLine($"   Added new keyword: quantum computing");
                Console.WriteLine($"   Updated H-Index: 42 → 45");
                Console.WriteLine($"   Updated Citations: 1250 → 1350");
                Console.WriteLine($"   Updated Publications: 85 → 92");

                try
                {
                    var updatedAuthor = await _client.AuthorGroups.UpdateAuthorAsync(groupId, clientId, updateAuthorRequest);
                    _logger.LogInformation("✅ Successfully updated author: {AuthorName}", updateAuthorRequest.Name);
                    Console.WriteLine($"✅ Author updated successfully");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would update author successfully");
                }

                Console.WriteLine();

                // Get all authors in the group
                _logger.LogInformation("Retrieving all authors in group: {GroupId}", groupId);
                Console.WriteLine($"👥 Retrieving all authors in group: {groupId}");

                try
                {
                    var authorsInGroup = await _client.AuthorGroups.GetAuthorsAsync(groupId, page: 1, pageSize: 10);
                    _logger.LogInformation("✅ Retrieved {Count} authors from group", authorsInGroup.Count);
                    Console.WriteLine($"✅ Found {authorsInGroup.Count} authors in group");
                    
                    foreach (var author in authorsInGroup.Take(3))
                    {
                        Console.WriteLine($"   • {author.Name} (H-Index: {author.HIndex})");
                        Console.WriteLine($"     {author.Affiliations?.FirstOrDefault()}");
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would retrieve authors list");
                    Console.WriteLine("   Sample authors:");
                    Console.WriteLine("   • Dr. Sarah Johnson (H-Index: 45)");
                    Console.WriteLine("     MIT Physics Department");
                    Console.WriteLine("   • Prof. Michael Chen (H-Index: 38)");
                    Console.WriteLine("     Stanford University");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manage authors demo");
                Console.WriteLine($"❌ Error managing authors in groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates searching author groups.
        /// </summary>
        private async Task DemoSearchAuthorGroups()
        {
            _logger.LogInformation("📋 Demo 6: Searching Author Groups");
            _logger.LogInformation("-----------------------------------");

            try
            {
                // Search by subject area
                var searchTerm = "physics";
                _logger.LogInformation("Searching author groups with term: {SearchTerm}", searchTerm);
                Console.WriteLine($"🔍 Searching for groups containing: '{searchTerm}'");

                try
                {
                    var searchResults = await _client.AuthorGroups.SearchAsync(searchTerm, page: 1, pageSize: 10);
                    _logger.LogInformation("✅ Found {Count} groups matching search term", searchResults.Data?.Count ?? 0);
                    Console.WriteLine($"✅ Found {searchResults.Data?.Count ?? 0} groups matching '{searchTerm}'");
                    
                    if (searchResults.Data?.Any() == true)
                    {
                        foreach (var group in searchResults.Data.Take(3))
                        {
                            Console.WriteLine($"   • {group.GroupName}");
                            Console.WriteLine($"     {group.Description}");
                            Console.WriteLine($"     Authors: {group.AuthorCount}");
                        }
                    }
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would find matching groups");
                    Console.WriteLine("   Sample results:");
                    Console.WriteLine("   • Physics Reviewers 2024");
                    Console.WriteLine("     Expert reviewers for physics manuscripts in 2024");
                    Console.WriteLine("     Authors: 25");
                    Console.WriteLine("   • Theoretical Physics Experts");
                    Console.WriteLine("     Specialists in theoretical physics research");
                    Console.WriteLine("     Authors: 18");
                }

                Console.WriteLine();

                // Search by specialization
                var specializationSearch = "machine learning";
                _logger.LogInformation("Searching for specialization: {SearchTerm}", specializationSearch);
                Console.WriteLine($"🔍 Searching for groups with specialization: '{specializationSearch}'");

                try
                {
                    var specializationResults = await _client.AuthorGroups.SearchAsync(specializationSearch, page: 1, pageSize: 5);
                    _logger.LogInformation("✅ Found {Count} groups with specialization", specializationResults.Data?.Count ?? 0);
                    Console.WriteLine($"✅ Found {specializationResults.Data?.Count ?? 0} groups with '{specializationSearch}' specialization");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would find ML groups");
                    Console.WriteLine("   • Machine Learning Experts");
                    Console.WriteLine("     Specialists in machine learning and artificial intelligence research");
                    Console.WriteLine("     Authors: 18");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search demo");
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
                var groupId = "group_001";
                var clientId = "author_002";

                // Remove an author from a group
                _logger.LogInformation("Removing author from group: {GroupId}, Client ID: {ClientId}", groupId, clientId);
                Console.WriteLine($"🗑️ Removing author from group: {groupId}");
                Console.WriteLine($"   Client ID: {clientId}");

                try
                {
                    await _client.AuthorGroups.DeleteAuthorAsync(groupId, clientId);
                    _logger.LogInformation("✅ Successfully removed author from group");
                    Console.WriteLine($"✅ Author removed successfully");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would remove author successfully");
                }

                Console.WriteLine();

                // Get paginated results
                _logger.LogInformation("Demonstrating pagination with large page size");
                Console.WriteLine($"📄 Demonstrating pagination (page 1, size 5)");

                try
                {
                    var paginatedGroups = await _client.AuthorGroups.GetAllAsync(page: 1, pageSize: 5, includeInactive: true);
                    _logger.LogInformation("✅ Retrieved page 1 with {Count} groups", paginatedGroups.Data?.Count ?? 0);
                    Console.WriteLine($"✅ Page 1: {paginatedGroups.Data?.Count ?? 0} groups");
                    Console.WriteLine($"   Total available: {paginatedGroups.TotalCount}");
                    Console.WriteLine($"   Has next page: {paginatedGroups.HasNextPage}");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - pagination example");
                    Console.WriteLine("   Page 1: 5 groups");
                    Console.WriteLine("   Total available: 23");
                    Console.WriteLine("   Has next page: True");
                }

                Console.WriteLine();

                // Delete an entire group
                var groupToDelete = "group_002";
                _logger.LogInformation("Deleting entire author group: {GroupId}", groupToDelete);
                Console.WriteLine($"🗑️ Deleting entire group: {groupToDelete}");

                try
                {
                    await _client.AuthorGroups.DeleteAsync(groupToDelete);
                    _logger.LogInformation("✅ Successfully deleted author group: {GroupId}", groupToDelete);
                    Console.WriteLine($"✅ Group deleted successfully");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogWarning("⚠️ API call failed (expected in demo): {Message}", ex.Message);
                    Console.WriteLine("⚠️ API call simulation - would delete group successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during advanced operations demo");
                Console.WriteLine($"❌ Error in advanced group operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates error handling scenarios.
        /// </summary>
        private async Task DemoErrorHandling()
        {
            _logger.LogInformation("📋 Demo 8: Error Handling");
            _logger.LogInformation("-------------------------");

            try
            {
                // Test validation errors
                Console.WriteLine("🚨 Testing validation errors...");
                
                var invalidGroupRequest = new CreateAuthorGroupRequest
                {
                    GroupName = "", // Invalid - empty name
                    Description = null, // Invalid - null description
                    OwnerTeam = "", // Invalid - empty owner team
                    EditorTeams = new string[0], // Invalid - empty array
                    ViewerTeams = null, // Invalid - null array
                    IsActive = true
                };

                try
                {
                    await _client.AuthorGroups.CreateAsync(invalidGroupRequest);
                }
                catch (ValidationException ex)
                {
                    _logger.LogInformation("✅ Caught expected validation error: {Message}", ex.Message);
                    Console.WriteLine($"✅ Validation error caught: {ex.Message}");
                    Console.WriteLine($"   Validation errors: {ex.ValidationErrors?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("✅ Caught validation-related error: {Message}", ex.Message);
                    Console.WriteLine($"✅ Validation-related error: {ex.Message}");
                }

                Console.WriteLine();

                // Test API errors
                Console.WriteLine("🚨 Testing API error scenarios...");
                
                try
                {
                    // Try to get a non-existent group
                    await _client.AuthorGroups.GetByIdAsync("non-existent-group-id");
                }
                catch (ProphyApiException ex)
                {
                    _logger.LogInformation("✅ Caught expected API error: {ErrorCode}", ex.ErrorCode);
                    Console.WriteLine($"✅ API error caught: {ex.ErrorCode}");
                    Console.WriteLine($"   Status: {ex.StatusCode}");
                    Console.WriteLine($"   Message: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("✅ Caught API-related error: {Message}", ex.Message);
                    Console.WriteLine($"✅ API-related error: {ex.Message}");
                }

                Console.WriteLine();

                // Test network errors
                Console.WriteLine("🚨 Testing network error handling...");
                Console.WriteLine("⚠️ Network errors would be handled gracefully in production");
                Console.WriteLine("   • Connection timeouts");
                Console.WriteLine("   • DNS resolution failures");
                Console.WriteLine("   • SSL certificate errors");
                Console.WriteLine("   • Rate limiting responses");

                _logger.LogInformation("✅ Error handling demonstration completed");
                Console.WriteLine("✅ Error handling scenarios demonstrated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during error handling demo");
                Console.WriteLine($"❌ Error in error handling demo: {ex.Message}");
            }
        }
    }
} 