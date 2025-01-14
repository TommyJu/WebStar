using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using System.Text;
using System.ClientModel;

namespace WebStar.Pages
{
    public class ChatBotModel : PageModel
    {
        private readonly IConfiguration _configuration;

        // Properties to hold user input and AI response
        [BindProperty]
        public string UserInput { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;

        public ChatBotModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            // Do nothing on GET
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(UserInput))
            {
                ModelState.AddModelError(string.Empty, "Input cannot be empty.");
                return Page();
            }

            try
            {
                // Load AI settings from configuration
                var modelId = _configuration["AI:ModelId"];
                var uri = _configuration["AI:Endpoint"];
                var githubPAT = _configuration["AI:PAT"];

                var client = new OpenAIClient(new ApiKeyCredential(githubPAT), new OpenAIClientOptions { Endpoint = new Uri(uri) });

                var builder = Kernel.CreateBuilder();
                builder.AddOpenAIChatCompletion(modelId, client);
                var kernel = builder.Build();

                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

                // Create a new chat history with a prompt
                ChatHistory chat = new ChatHistory("You are an AI assistant that helps people find information. The response must be brief and to the point.");
                chat.AddUserMessage(UserInput);

                StringBuilder sb = new StringBuilder();

                // Get streaming response from AI
                await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(chat, kernel: kernel))
                {
                    sb.Append(message.Content);
                }

                chat.AddAssistantMessage(sb.ToString());

                // Store the AI response in the property
                AiResponse = sb.ToString();

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return Page();
            }
        }
    }
}
