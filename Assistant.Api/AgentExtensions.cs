using Assistant.Api.Plugin;
using Assistant.Api.Resources;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Assistant.Api;

public static class AgentExtensions
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        PromptTemplateConfig templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(
            EmbeddedResource.Read("AgentDefinition1.yaml"));

        services.AddTransient<ChatCompletionAgent>((sp) =>
        {
            Kernel kernel = sp.GetRequiredService<Kernel>();
            kernel.Plugins.AddFromObject(sp.GetRequiredService<EmailPlugin>());

            var agent = new ChatCompletionAgent(templateConfig, new HandlebarsPromptTemplateFactory())
            {
                Kernel = kernel,
            };

            agent.Arguments.ExecutionSettings![PromptExecutionSettings.DefaultServiceId]
                    .FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            return agent;
        });

        return services;
    }
}