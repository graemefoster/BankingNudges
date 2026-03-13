using System.Collections.Concurrent;
using Azure.AI.OpenAI;
using Azure.Identity;
using BankOfGraeme.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

namespace BankOfGraeme.Api.Endpoints;

public static class ChatEndpoints
{
    // In-memory session store — maps sessionId to a pre-configured agent + session.
    // In production you'd persist this; for now ephemeral sessions are fine.
    private static readonly ConcurrentDictionary<string, ChatSessionState> Sessions = new();

    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat");

        // POST /api/chat/nudge/start — create a chat session for a nudge
        group.MapPost("/nudge/start", async (
            NudgeChatStartRequest req,
            NudgeChatAgent chatAgent,
            NudgeChatTools chatTools,
            IConfiguration config) =>
        {
            var setup = await chatAgent.BuildChatSetupAsync(req.NudgeId, req.CustomerId);
            if (setup is null)
                return Results.NotFound(new { error = "Nudge not found or context unavailable" });

            var endpoint = config["AZURE_OPENAI_ENDPOINT"]
                ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required");
            var deployment = config["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4o";

            var agent = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new DefaultAzureCredential())
                .GetChatClient(deployment)
                .AsIChatClient()
                .AsAIAgent(
                    instructions: setup.SystemPrompt,
                    name: "NudgeChatAgent",
                    tools: [AIFunctionFactory.Create(chatTools.GetNudgeHistory)]);

            var session = await agent.CreateSessionAsync();
            var sessionId = Guid.NewGuid().ToString("N");

            Sessions[sessionId] = new ChatSessionState(agent, session);

            return Results.Ok(new NudgeChatStartResponse(sessionId));
        });

        // POST /api/chat/nudge/message — send a message and get a streaming response
        group.MapPost("/nudge/message", async (NudgeChatMessageRequest req, HttpContext httpContext) =>
        {
            if (!Sessions.TryGetValue(req.SessionId, out var state))
                return Results.NotFound(new { error = "Chat session not found or expired" });

            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            await foreach (var update in state.Agent.RunStreamingAsync(req.Message, state.Session))
            {
                var text = update.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    await httpContext.Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { content = text })}\n\n");
                    await httpContext.Response.Body.FlushAsync();
                }
            }

            // Signal end of stream
            await httpContext.Response.WriteAsync("data: [DONE]\n\n");
            await httpContext.Response.Body.FlushAsync();
            return Results.Empty;
        });

        // DELETE /api/chat/nudge/:sessionId — clean up a session
        group.MapDelete("/nudge/{sessionId}", (string sessionId) =>
        {
            Sessions.TryRemove(sessionId, out _);
            return Results.Ok();
        });
    }
}

public record NudgeChatStartRequest(int CustomerId, int NudgeId);
public record NudgeChatStartResponse(string SessionId);
public record NudgeChatMessageRequest(string SessionId, string Message);

internal record ChatSessionState(AIAgent Agent, AgentSession Session);
