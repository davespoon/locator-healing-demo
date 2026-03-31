using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace LocatorHealing.Agent.Infrastructure;

public sealed class CandidateAgentFactory
{
    private const string ModelId = "gpt-4o-mini";

    public AIAgent Create()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

        IChatClient chatClient = new OpenAIClient(apiKey)
            .GetChatClient(ModelId)
            .AsIChatClient();

        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                               You repair broken Selenium locators.

                               This is already a confirmed locator-failure case.
                               Your only job is to propose replacement locators from the provided evidence.

                               Rules:
                               - Return structured output only.
                               - Return at most 3 candidates.
                               - Decision must be either "candidates" or "insufficient_evidence".
                               - Prefer stable selectors in this order:
                                 1) data-test / test id
                                 2) stable id
                                 3) stable name
                                 4) concise css selector
                                 5) xpath only as a last resort
                               - Prefer CSS over XPath whenever possible.
                               - Never suggest absolute XPath.
                               - Avoid index-based selectors unless there is no better option.
                               - Confidence must be between 0.0 and 1.0.
                               - Keep reasons short and concrete.
                               - Fill semanticChecks when you can infer them; otherwise use null values.
                               - Use only these riskFlags when needed:
                                 uses_xpath
                                 index_based
                                 dynamic_class
                                 text_fragile
                                 non_unique_risk
                                 semantic_mismatch_risk
                               - If the DOM evidence is too weak to safely suggest candidates, set decision to "insufficient_evidence".
                               - Otherwise set decision to "candidates".
                               """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<LocatorCandidateGenerationResult>()
            }
        });
    }
}