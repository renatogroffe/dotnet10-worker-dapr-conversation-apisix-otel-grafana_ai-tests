using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using System.Text.Json;
using WorkerConsumer.Tracing;

namespace WorkerDaprTestConversation;

public class Worker(ILogger<Worker> logger, IConfiguration configuration,
#pragma warning disable DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    DaprConversationClient conversationClient) : BackgroundService
#pragma warning restore DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    private readonly string[] TECNOLOGIAS = { "C#", ".NET", "Dapr", "OpenTelemetry", "Grafana", "Grafana Tempo", "Grafana Loki", "Kubernetes", "Microsoft Azure", "Azure Kubernetes Service" };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string conversationComponentId = configuration["Dapr:ConversationComponentId"]!;
        int waitingInSeconds = Convert.ToInt32(configuration["WaitingInSeconds"]);
        int contador = 0;
        var jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activityTest = OpenTelemetryExtensions.ActivitySource!
                .StartActivity("TestarConversationDapr")!;
            activityTest.SetTag("conversation.component.id", conversationComponentId);

            var question = $"Explique em poucas palavras o que e {TECNOLOGIAS[contador]}.";
            logger.LogInformation($"Pergunta que sera enviada para a IA: {question}");
            logger.LogInformation($"Enviando mensagem para o componente: {conversationComponentId}");
            var response = await conversationClient.ConverseAsync(
                [
                    new ConversationInput(
                        new List<IConversationMessage>
                        {
                            new UserMessage
                            {
                                Name = "Test User",
                                Content =
                                [
                                    new MessageContent(question)
                                ]
                            }
                        }
                    )
                ],
                new ConversationOptions(conversationComponentId)
            );

            int responseNumber = 0;
            logger.LogInformation("Respostas geradas:");
            foreach (var resp in response.Outputs)
            {
                logger.LogInformation($"* {resp.GetType().FullName}.Outputs[{responseNumber++}]");
                logger.LogInformation($"Model: {resp.Model}");
                logger.LogInformation("Conteudo:");
                logger.LogInformation(JsonSerializer.Serialize(resp, jsonSerializerOptions));
                int choiceNumber = 0;
                foreach (var choice in resp.Choices)
                {
                    logger.LogInformation($"{resp.GetType().FullName}.Choices[{choiceNumber++}].Message:");
                    logger.LogInformation(choice.Message.Content);
                }
            }

            contador++;
            if (contador == TECNOLOGIAS.Length)
                contador = 0;
            activityTest.Stop();

            logger.LogInformation(
                $"Execucao concluida em: {DateTime.UtcNow.AddHours(-3):yyyy-MM-dd HH:mm:ss}");
            await Task.Delay(1000 * waitingInSeconds, stoppingToken);
        }
    }
}