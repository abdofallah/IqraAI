using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using IqraCore.Interfaces.AI;
using System.Diagnostics;
using System.Text;
using Twilio.Rest.Api.V2010.Account.Usage.Record;
using static System.Collections.Specialized.BitVector32;

namespace IqraInfrastructure.Services
{
    public class ClaudeStreamingLLMService : IAIService
    {
        private readonly AnthropicClient _client;
        private readonly string _apiKey;

        private int _maxTokens;
        private string _model;
        private decimal _temperature;

        private List<Message> _initialMessages;
        private List<Message> _messagesMemory;

        private string _systemPrompt;

        public event EventHandler<string> MessageStreamed;

        public ClaudeStreamingLLMService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new AnthropicClient(apiKey);

            _maxTokens = 128;
            _model = AnthropicModels.Claude3Haiku;
            _temperature = 1m;

            _initialMessages = new List<Message>();
            _messagesMemory = new List<Message>();

            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task<string> ProcessInputAsync(string input, CancellationToken cancellationToken)
        {
            _messagesMemory.Add(
                new Message {
                    Role = RoleType.User,
                    Content = input
                }
            );

            var finalMessages = _initialMessages.Concat(_messagesMemory).ToList();

            var parameters = new MessageParameters
            {
                SystemMessage = _systemPrompt,
                Messages = finalMessages,
                MaxTokens = _maxTokens,
                Model = _model,
                Stream = true,
                Temperature = _temperature,
            };

            var fullOutputBuilder = new StringBuilder();
            var sectionedOutputBuilder = new StringBuilder();

            int charactersConverted = 0;
            
            try
            {
                await foreach (var res in _client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken))
                {
                    if (res.Delta != null)
                    {
                        if (res.Delta.StopReason == "end_turn")
                        {
                            string remainingString = sectionedOutputBuilder.ToString();
                            if (remainingString.Length > 0)
                            {
                                MessageStreamed?.Invoke(this, remainingString);
                            }

                            break;
                        }

                        if (string.IsNullOrEmpty(res.Delta.Text))
                        {
                            continue;
                        }

                        fullOutputBuilder.Append(res.Delta.Text);
                        sectionedOutputBuilder.Append(res.Delta.Text);

                        var currentText = sectionedOutputBuilder.ToString();

                        var (sections, remaining) = SeparateTextIntoSections2(currentText, ref charactersConverted);
                        sectionedOutputBuilder = remaining;
                        foreach (var section in sections)
                        {
                            if (cancellationToken.IsCancellationRequested) { break; }

                            charactersConverted += section.Length;

                            MessageStreamed?.Invoke(this, section);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    Console.WriteLine(ex);
                }
                else
                {
                    fullOutputBuilder.Append(".... It seems you have spoken over while i was speaking so I will let you speak.");
                    var spokenoverResult = fullOutputBuilder.ToString();

                    _messagesMemory.Add(
                        new Message
                        {
                            Role = RoleType.Assistant,
                            Content = spokenoverResult
                        }
                    );

                    return spokenoverResult;
                }
            }

            var aiAssitantMessage = fullOutputBuilder.ToString();

            _messagesMemory.Add(
                new Message
                {
                    Role = RoleType.Assistant,
                    Content = aiAssitantMessage
                }
            );

            return aiAssitantMessage;
        }

        private (List<string>, StringBuilder?) SeparateTextIntoSections2(string text, ref int charactersConverted)
        {
            var sections = new List<string>();
            var remainingSection = new StringBuilder();

            List<int> seperatorIndexes = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
            
                if (IsSectionSeparator(character))
                {
                    seperatorIndexes.Add(i);
                }
            }

            if (seperatorIndexes.Count == 0)
            {
                remainingSection.Append(text);
                return (sections, remainingSection);
            }

            int lastSeperatorIndex = -1;
            bool useLastSeperator = charactersConverted > 27; // make this dynamic

            if (useLastSeperator)
            {
                if (seperatorIndexes.Count >= 2) // make this dynamic
                {
                    lastSeperatorIndex = seperatorIndexes[seperatorIndexes.Count - 1];
                }
                else
                {
                    remainingSection.Append(text);
                    return (sections, remainingSection);
                }
            }
            else
            {
                lastSeperatorIndex = seperatorIndexes[0];
            }

            string textTillLastSeperator = text.Substring(0, lastSeperatorIndex + 1);

            if (textTillLastSeperator.EndsWith("Muscat,")) // make this dynamic
            {
                if (useLastSeperator)
                {
                    lastSeperatorIndex = seperatorIndexes[seperatorIndexes.Count - 2];
                }
                else
                {
                    remainingSection.Append(text);
                    return (sections, remainingSection);
                }
            }

            string remainingTextAfterSeperator = text.Substring(lastSeperatorIndex + 1);

            sections.Add(textTillLastSeperator);
            remainingSection.Append(remainingTextAfterSeperator);

            if (useLastSeperator)
            {
                charactersConverted = 0;
            }

            return (sections, remainingSection);
        }

        private (List<string>, StringBuilder?) SeparateTextIntoSections(string text, ref int charactersConverted)
        {
            var sections = new List<string>();
            var remainingSection = new StringBuilder();

            bool useLastSeperator = charactersConverted > 27; // make this dynamic

            int seperatorCount = 0;
            int lastSeperatorIndex = -1;

            int characterIndex = useLastSeperator ? (text.Length - 1) : 0;
            while (true)
            {
                char character = text[characterIndex];

                if (useLastSeperator)
                {
                    characterIndex -= 1;

                    if (characterIndex < 0)
                    {
                        remainingSection.Append(text);
                        break;
                    }

                    if (IsSectionSeparator(character))
                    {
                        seperatorCount++;

                        if (lastSeperatorIndex == -1)
                        {
                            lastSeperatorIndex = characterIndex;
                        }

                        if (seperatorCount >= 2)
                        {
                            string textTillLastSeperator = text.Substring(0, lastSeperatorIndex + 2);
                            string remainingTextAfterSeperator = text.Substring(lastSeperatorIndex + 2);

                            sections.Add(textTillLastSeperator);
                            remainingSection.Append(remainingTextAfterSeperator);

                            charactersConverted = 0;
                            break;
                        }
                    }   
                }
                else
                {
                    remainingSection.Append(character);
                    if (IsSectionSeparator(character))
                    {
                        var section = remainingSection.ToString().Trim();
                        if (!string.IsNullOrEmpty(section))
                        {
                            sections.Add(section);
                        }
                        remainingSection.Clear();
                    }

                    characterIndex += 1;

                    if (characterIndex >= text.Length)
                    {
                        break;
                    }
                }
            }

            return (sections, remainingSection);
        }

        private bool IsSectionSeparator(char character)
        {
            return character == '.' || character == '!' || character == '?' || character == ',';
        }

        public void SetModel(string model)
        {
            _model = model;
        }

        public void SetTemperature(decimal temperature)
        {
            _temperature = temperature;
        }

        public void SetMaxTokens(int maxTokens)
        {
            _maxTokens = maxTokens;
        }

        public void SetSystemPrompt(string systemPrompt)
        {
            _systemPrompt = systemPrompt;
        }

        public void SetInitialMessage(string initialMessage)
        {
            _initialMessages = new List<Message>()
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = "Hello"
                },
                new Message
                {
                    Role = RoleType.Assistant,
                    Content = initialMessage
                }
            };
        }
    }
}