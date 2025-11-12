using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// AI Matchmaking service using vector embeddings for attendee similarity
/// </summary>
public class MatchmakingService : IMatchmakingService
{
    private readonly IOpenAIService _openAIService;
    private readonly IAnthropicService _anthropicService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MatchmakingService> _logger;

    public MatchmakingService(
        IOpenAIService openAIService,
        IAnthropicService anthropicService,
        ApplicationDbContext dbContext,
        ILogger<MatchmakingService> logger)
    {
        _openAIService = openAIService;
        _anthropicService = anthropicService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<double> CalculateAttendeeSimilarityAsync(
        Guid attendeeId1,
        Guid attendeeId2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, we would:
            // 1. Fetch stored embeddings for both attendees from the database
            // 2. Calculate cosine similarity between the embeddings
            // For now, we'll use a simplified approach

            var attendee1 = await GetAttendeeProfileDataAsync(attendeeId1, cancellationToken);
            var attendee2 = await GetAttendeeProfileDataAsync(attendeeId2, cancellationToken);

            if (string.IsNullOrEmpty(attendee1) || string.IsNullOrEmpty(attendee2))
            {
                return 0.0;
            }

            // Generate embeddings
            var embedding1 = await _openAIService.GenerateEmbeddingsAsync(attendee1);
            var embedding2 = await _openAIService.GenerateEmbeddingsAsync(attendee2);

            // Calculate cosine similarity
            var similarity = CalculateCosineSimilarity(embedding1, embedding2);

            _logger.LogInformation(
                "Calculated similarity {Similarity} between attendees {Id1} and {Id2}",
                similarity, attendeeId1, attendeeId2);

            return similarity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating attendee similarity");
            return 0.0;
        }
    }

    public async Task<List<NetworkingSuggestion>> GetNetworkingSuggestionsAsync(
        Guid attendeeId,
        Guid eventId,
        int maxSuggestions = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all attendees for the event (excluding the current attendee)
            var eventAttendees = await _dbContext.Registrations
                .Where(r => r.EventId == eventId && r.UserId != attendeeId)
                .Select(r => new
                {
                    AttendeeId = r.UserId,
                    r.User.FullName,
                    r.User.Email
                })
                .Take(50) // Limit to prevent excessive processing
                .ToListAsync(cancellationToken);

            var suggestions = new List<NetworkingSuggestion>();

            // Calculate similarity for each attendee
            foreach (var attendee in eventAttendees)
            {
                var similarity = await CalculateAttendeeSimilarityAsync(
                    attendeeId,
                    attendee.AttendeeId,
                    cancellationToken);

                if (similarity > 0.5) // Only suggest if similarity is above threshold
                {
                    suggestions.Add(new NetworkingSuggestion
                    {
                        SuggestedAttendeeId = attendee.AttendeeId,
                        Name = attendee.FullName ?? "Unknown",
                        SimilarityScore = similarity,
                        MatchReason = $"High compatibility score of {similarity:P0}"
                    });
                }
            }

            // Sort by similarity and take top suggestions
            var topSuggestions = suggestions
                .OrderByDescending(s => s.SimilarityScore)
                .Take(maxSuggestions)
                .ToList();

            // Enhance suggestions with AI-generated insights
            await EnhanceNetworkingSuggestionsAsync(topSuggestions, attendeeId, cancellationToken);

            _logger.LogInformation(
                "Generated {Count} networking suggestions for attendee {AttendeeId}",
                topSuggestions.Count, attendeeId);

            return topSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating networking suggestions");
            return new List<NetworkingSuggestion>();
        }
    }

    public async Task<List<string>> GenerateConversationStartersAsync(
        Guid attendeeId1,
        Guid attendeeId2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile1 = await GetAttendeeProfileDataAsync(attendeeId1, cancellationToken);
            var profile2 = await GetAttendeeProfileDataAsync(attendeeId2, cancellationToken);

            var systemPrompt = @"You are a networking expert. Generate natural, engaging conversation starters for two people meeting at a professional event.

The conversation starters should:
- Be relevant to their backgrounds and interests
- Feel natural and not forced
- Encourage meaningful discussion
- Be specific rather than generic

Return a JSON array of 5 conversation starter strings.";

            var userPrompt = $@"Generate conversation starters for these two attendees:

Person 1:
{profile1}

Person 2:
{profile2}";

            var response = await _anthropicService.SendPromptJsonAsync<ConversationStartersResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.9,
                maxTokens: 1000);

            if (response.Success && response.Data?.ConversationStarters != null)
            {
                return response.Data.ConversationStarters;
            }

            // Fallback generic starters
            return new List<string>
            {
                "What brings you to this event?",
                "What are you most looking forward to learning today?",
                "Have you attended events like this before?",
                "What's your background in this field?",
                "Are there any sessions you're particularly excited about?"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation starters");
            return new List<string>();
        }
    }

    public async Task<List<FollowUpRecommendation>> GetFollowUpRecommendationsAsync(
        Guid attendeeId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, we would track:
            // - Who the attendee talked to
            // - Which sessions they attended together
            // - Interactions during the event
            // For now, we'll use the networking suggestions as a basis

            var networkingSuggestions = await GetNetworkingSuggestionsAsync(
                attendeeId,
                eventId,
                maxSuggestions: 5,
                cancellationToken);

            var recommendations = new List<FollowUpRecommendation>();

            foreach (var suggestion in networkingSuggestions)
            {
                recommendations.Add(new FollowUpRecommendation
                {
                    AttendeeId = suggestion.SuggestedAttendeeId,
                    Name = suggestion.Name,
                    RecommendationType = "connect",
                    Reason = $"You both have aligned interests. {suggestion.MatchReason}",
                    Priority = suggestion.SimilarityScore > 0.8 ? 5 : suggestion.SimilarityScore > 0.7 ? 4 : 3,
                    SuggestedTopics = suggestion.CommonInterests,
                    SuggestedMessage = $"Hi {suggestion.Name}, it was great meeting you at the event. I'd love to stay connected and continue our conversation about {string.Join(" and ", suggestion.CommonInterests.Take(2))}."
                });
            }

            _logger.LogInformation(
                "Generated {Count} follow-up recommendations for attendee {AttendeeId}",
                recommendations.Count, attendeeId);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating follow-up recommendations");
            return new List<FollowUpRecommendation>();
        }
    }

    public async Task UpdateAttendeeEmbeddingsAsync(
        Guid attendeeId,
        AttendeeProfile profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a comprehensive text representation of the attendee profile
            var profileText = BuildProfileText(profile);

            // Generate embeddings
            var embeddings = await _openAIService.GenerateEmbeddingsAsync(profileText);

            // In a real implementation, we would store these embeddings in the database
            // using pgvector for efficient similarity search
            // For now, we'll log the operation

            _logger.LogInformation(
                "Updated embeddings for attendee {AttendeeId} (dimension: {Dimension})",
                attendeeId, embeddings.Length);

            // TODO: Store in database
            // await _dbContext.AttendeeEmbeddings.AddAsync(new AttendeeEmbedding
            // {
            //     AttendeeId = attendeeId,
            //     Embedding = embeddings,
            //     ProfileVersion = DateTime.UtcNow
            // }, cancellationToken);
            // await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendee embeddings");
            throw;
        }
    }

    public async Task<List<SimilarAttendee>> FindSimilarAttendeesAsync(
        Guid attendeeId,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation with pgvector:
            // 1. Get the attendee's embedding from database
            // 2. Use pgvector's similarity search to find closest vectors
            // 3. Return results ordered by similarity

            // For now, we'll use a simpler approach
            var allAttendees = await _dbContext.Users
                .Where(u => u.Id != attendeeId)
                .Select(u => u.Id)
                .Take(100) // Limit for performance
                .ToListAsync(cancellationToken);

            var similarAttendees = new List<SimilarAttendee>();

            foreach (var otherAttendeeId in allAttendees.Take(20)) // Process subset
            {
                var similarity = await CalculateAttendeeSimilarityAsync(
                    attendeeId,
                    otherAttendeeId,
                    cancellationToken);

                if (similarity > 0.5)
                {
                    var attendeeInfo = await _dbContext.Users
                        .Where(u => u.Id == otherAttendeeId)
                        .Select(u => new
                        {
                            u.FullName,
                            u.Email
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (attendeeInfo != null)
                    {
                        similarAttendees.Add(new SimilarAttendee
                        {
                            AttendeeId = otherAttendeeId,
                            Name = attendeeInfo.FullName ?? "Unknown",
                            SimilarityScore = similarity,
                            MatchingAttributes = new List<string> { "Professional interests", "Industry background" }
                        });
                    }
                }
            }

            return similarAttendees
                .OrderByDescending(a => a.SimilarityScore)
                .Take(maxResults)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar attendees");
            return new List<SimilarAttendee>();
        }
    }

    #region Helper Methods

    private async Task<string> GetAttendeeProfileDataAsync(Guid attendeeId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == attendeeId)
            .Select(u => new
            {
                u.FullName,
                u.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return string.Empty;
        }

        // In a real implementation, we'd have much richer profile data
        return $"Name: {user.FullName}, Email: {user.Email}";
    }

    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length || vector1.Length == 0)
        {
            return 0.0;
        }

        double dotProduct = 0.0;
        double magnitude1 = 0.0;
        double magnitude2 = 0.0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0.0 || magnitude2 == 0.0)
        {
            return 0.0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }

    private string BuildProfileText(AttendeeProfile profile)
    {
        var parts = new List<string>
        {
            $"Name: {profile.Name}",
            $"Title: {profile.Title}",
            $"Company: {profile.Company}",
            $"Industry: {profile.Industry}",
            $"Bio: {profile.Bio}"
        };

        if (profile.Interests.Any())
        {
            parts.Add($"Interests: {string.Join(", ", profile.Interests)}");
        }

        if (profile.Skills.Any())
        {
            parts.Add($"Skills: {string.Join(", ", profile.Skills)}");
        }

        if (profile.Goals.Any())
        {
            parts.Add($"Goals: {string.Join(", ", profile.Goals)}");
        }

        if (!string.IsNullOrEmpty(profile.Location))
        {
            parts.Add($"Location: {profile.Location}");
        }

        return string.Join("\n", parts);
    }

    private async Task EnhanceNetworkingSuggestionsAsync(
        List<NetworkingSuggestion> suggestions,
        Guid attendeeId,
        CancellationToken cancellationToken)
    {
        // This would use AI to add more context to each suggestion
        // For now, we'll add placeholder data
        foreach (var suggestion in suggestions)
        {
            suggestion.CommonInterests = new List<string>
            {
                "Technology",
                "Innovation",
                "Professional Growth"
            };

            suggestion.ComplementarySkills = new List<string>
            {
                "Leadership",
                "Strategy"
            };
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Helper Classes

    private class ConversationStartersResponse
    {
        public List<string> ConversationStarters { get; set; } = new();
    }

    #endregion
}
