using IqraCore.Entities.Business; // For language config access
using IqraInfrastructure.Managers.Languages; // For LanguagesManager
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer; // For NumberToWords

namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentDTMFHandler
    {
        // Dependencies
        private readonly ILogger<ConversationAIAgentDTMFHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LanguagesManager _languagesManager;

        // References to other modules/orchestrator needed for actions
        private readonly ConversationAIAgentAudioOutput _audioOutput;
        // Need a way to trigger re-initialization (e.g., callback to Orchestrator)
        private readonly Func<string, Task> _onLanguageChangeRequestAsync; // Orchestrator provides this

        // Internal state for specific DTMF handling flows
        private bool _isAwaitingLanguageSelection = false;

        public ConversationAIAgentDTMFHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LanguagesManager languagesManager,
            ConversationAIAgentAudioOutput audioOutput,
            Func<string, Task> onLanguageChangeRequestAsync // Callback to Orchestrator
            )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentDTMFHandler>();
            _agentState = agentState;
            _languagesManager = languagesManager;
            _audioOutput = audioOutput;
            _onLanguageChangeRequestAsync = onLanguageChangeRequestAsync;
        }

        public Task InitializeAsync()
        {
            // Reset state on init
            _isAwaitingLanguageSelection = false;
            _agentState.IsProcessingDTMFAlready = false; // Reset lock in state
            _logger.LogInformation("DTMF Handler initialized for Agent {AgentId}.", _agentState.AgentId);
            return Task.CompletedTask;
        }


        // Called by Orchestrator during NotifyConversationStarted if multi-language is enabled
        public async Task SetupLanguageSelectionAsync(CancellationToken cancellationToken)
        {
            // --- Move multi-language prompt logic from NotifyConversationStarted here ---
            if (_agentState.CurrentSessionRoute?.Language.MultiLanguageEnabled == true &&
                _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages?.Count > 1)
            {
                _logger.LogInformation("Agent {AgentId}: Setting up multi-language selection.", _agentState.AgentId);
                string multiLanguageText = "";
                int langCount = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages.Count;

                for (int i = 0; i < langCount; i++)
                {
                    var languageInfo = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages[i];
                    var languageData = await _languagesManager.GetLanguageByCode(languageInfo.LanguageCode);
                    var languageLocale = languageData.Success ? languageData.Data.Name : languageInfo.LanguageCode; // Fallback to code

                    // Ensure Humanizer handles numbers correctly based on current culture or specify one if needed.
                    string numberWord = (i + 1).ToWords(); // Potentially needs culture info

                    // Replace placeholders case-insensitively?
                    string builtMessage = languageInfo.MessageToPlay
                       .Replace("{number}", numberWord, StringComparison.OrdinalIgnoreCase)
                       .Replace("{name}", languageLocale, StringComparison.OrdinalIgnoreCase);

                    multiLanguageText += $"\n{builtMessage}";
                }

                if (!string.IsNullOrWhiteSpace(multiLanguageText))
                {
                    _logger.LogDebug("Agent {AgentId}: Playing multi-language prompt: {Prompt}", _agentState.AgentId, multiLanguageText);
                    // Use AudioOutput to play the prompt (blocking)
                    await _audioOutput.SynthesizeAndPlayBlockingAsync(multiLanguageText.Trim(), cancellationToken);
                    _isAwaitingLanguageSelection = true; // Set flag to expect DTMF for language
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Multi-language enabled, but prompt text generation failed.", _agentState.AgentId);
                    _isAwaitingLanguageSelection = false;
                }
            }
            else
            {
                _isAwaitingLanguageSelection = false; // Not needed
            }
        }

        // Called by Orchestrator's ProcessDTMFAsync
        public async Task ProcessDigitAsync(string digit, CancellationToken cancellationToken)
        {
            // --- Move DTMF processing logic here ---
            // Check _isAwaitingLanguageSelection flag
            // Use _agentState.IsProcessingDTMFAlready lock
            // Parse digit, validate against available languages
            // If valid language change:
            //    - Call _onLanguageChangeRequestAsync(newLanguageCode)
            //    - Reset _isAwaitingLanguageSelection
            // If invalid:
            //    - Play error prompt via _audioOutput

            if (!_isAwaitingLanguageSelection)
            {
                _logger.LogDebug("Agent {AgentId}: Ignoring DTMF digit '{Digit}' as language selection is not active.", _agentState.AgentId, digit);
                // TODO: Implement handling for other DTMF scenarios if needed later
                return;
            }


            if (_agentState.IsProcessingDTMFAlready)
            {
                _logger.LogWarning("Agent {AgentId}: Ignoring DTMF digit '{Digit}' due to concurrent processing.", _agentState.AgentId, digit);
                return;
            }

            _agentState.IsProcessingDTMFAlready = true;
            try
            {
                _logger.LogInformation("Agent {AgentId}: Processing DTMF digit '{Digit}' for language selection.", _agentState.AgentId, digit);

                if (int.TryParse(digit, out int languageIndex) && _agentState.CurrentSessionRoute?.Language.EnabledMultiLanguages != null)
                {
                    int langCount = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages.Count;
                    if (languageIndex > 0 && languageIndex <= langCount)
                    {
                        var selectedLanguage = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages[languageIndex - 1];
                        _logger.LogInformation("Agent {AgentId}: User selected language '{Code}' via DTMF.", _agentState.AgentId, selectedLanguage.LanguageCode);

                        if (selectedLanguage.LanguageCode != _agentState.CurrentLanguageCode)
                        {
                            // Trigger re-initialization via Orchestrator callback
                            await _onLanguageChangeRequestAsync(selectedLanguage.LanguageCode);
                        }
                        else
                        {
                            _logger.LogInformation("Agent {AgentId}: User selected current language '{Code}'. No change needed.", _agentState.AgentId, selectedLanguage.LanguageCode);
                            // Maybe play a confirmation? "Continuing in [Language]."
                            // await _audioOutput.SynthesizeAndPlayBlockingAsync($"Continuing in {selectedLanguage.LanguageCode}", cancellationToken); // Example
                        }
                        _isAwaitingLanguageSelection = false; // Language selected, stop listening for it
                    }
                    else
                    {
                        _logger.LogWarning("Agent {AgentId}: Invalid language selection index: {Index}", _agentState.AgentId, languageIndex);
                        await PlayInvalidSelectionPromptAsync(cancellationToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Invalid DTMF input for language selection: '{Digit}'", _agentState.AgentId, digit);
                    await PlayInvalidSelectionPromptAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing DTMF digit '{Digit}'.", _agentState.AgentId, digit);
                // TODO: Raise error event?
            }
            finally
            {
                _agentState.IsProcessingDTMFAlready = false;
            }
        }

        private async Task PlayInvalidSelectionPromptAsync(CancellationToken cancellationToken)
        {
            // Re-generate the prompt text (could be cached from SetupLanguageSelectionAsync)
            string multiLanguageText = "Please enter a valid option."; // Basic fallback
            if (_agentState.CurrentSessionRoute?.Language.MultiLanguageEnabled == true &&
                _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages?.Count > 1)
            {
                multiLanguageText = ""; // Regenerate
                int langCount = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages.Count;
                for (int i = 0; i < langCount; i++)
                { /* ... copy logic from SetupLanguageSelectionAsync ... */
                    var languageInfo = _agentState.CurrentSessionRoute.Language.EnabledMultiLanguages[i];
                    var languageData = await _languagesManager.GetLanguageByCode(languageInfo.LanguageCode);
                    var languageLocale = languageData.Success ? languageData.Data.Name : languageInfo.LanguageCode;
                    string numberWord = (i + 1).ToWords();
                    string builtMessage = languageInfo.MessageToPlay
                        .Replace("{number}", numberWord, StringComparison.OrdinalIgnoreCase)
                        .Replace("{name}", languageLocale, StringComparison.OrdinalIgnoreCase);
                    multiLanguageText += $"\n{builtMessage}";
                }
                multiLanguageText = $"Invalid selection. {multiLanguageText.Trim()}";
            }
            await _audioOutput.SynthesizeAndPlayBlockingAsync(multiLanguageText, cancellationToken);
        }

        public void Reset()
        {
            _isAwaitingLanguageSelection = false;
            _agentState.IsProcessingDTMFAlready = false;
            _logger.LogDebug("DTMF Handler state reset for Agent {AgentId}.", _agentState.AgentId);
        }

        // No Dispose needed if it doesn't own resources like timers/services
    }
}