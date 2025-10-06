import { ChatViewModel } from './viewmodels/ChatViewModel.js';
import { ChatView } from './views/ChatView.js';

/**
 * Main Application Bootstrap
 * Uses MVVM pattern with proper separation of concerns
 */
document.addEventListener('DOMContentLoaded', () => {
    // Initialize MVVM components
    const viewModel = new ChatViewModel();
    const view = new ChatView();

    // Wire up ViewModel events to View updates
    viewModel.subscribe(handleViewModelEvents);

    // Wire up View events to ViewModel actions
    setupViewEventHandlers();

    // Initialize the application
    initializeApplication();

    /**
     * Handle events from the ViewModel and update the View accordingly
     */
    function handleViewModelEvents(event) {
        switch (event.type) {
            case 'providersLoaded':
                view.renderProviders(event.providers);
                // Restore saved selection if available
                if (viewModel.selectedProvider) {
                    view.selectProvider(viewModel.selectedProvider);
                    // Also load models for the saved provider
                    viewModel.loadModelsForProvider(viewModel.selectedProvider).catch(error => {
                        console.warn('Failed to load models for saved provider:', error);
                    });
                }
                break;

            case 'modelsLoaded':
                view.renderModels(event.models);
                // Restore saved selection if available and it matches the current provider
                if (viewModel.selectedModel && viewModel.selectedProvider === event.providerId) {
                    view.selectModel(viewModel.selectedModel);
                }
                break;

            case 'messageSent':
                view.renderMessage(event.message);
                view.showTypingIndicator();
                view.clearMessageInput();
                view.setLoading(true);
                break;

            case 'streamingStarted':
                view.hideTypingIndicator();
                view.renderMessage(event.message);
                break;

            case 'streamingUpdate':
                view.renderStreamingUpdate(event.message, event.content);
                break;

            case 'streamingComplete':
                view.renderMessage(event.message, true);
                break;

            case 'errorMessage':
                view.showError(event.error);
                view.setLoading(false);
                break;

            case 'conversationCleared':
                view.clearMessages();
                break;

            case 'loadingComplete':
                view.setLoading(false);
                view.focusMessageInput();
                break;

            case 'requestAborted':
                view.hideTypingIndicator();
                view.setLoading(false);
                break;

            case 'error':
                view.showError(event.error);
                break;

            case 'modelUpdated':
                // Handle general model state updates
                view.setLoading(viewModel.isLoading);
                break;
        }
    }

    /**
     * Set up event handlers from View to ViewModel
     */
    function setupViewEventHandlers() {
        view.bindProviderChange((providerId) => {
            console.log('Provider changed to:', providerId);
            viewModel.selectProvider(providerId);
            if (providerId) {
                viewModel.loadModelsForProvider(providerId).then(models => {
                    console.log('Models loaded for provider:', models.length);
                }).catch(error => {
                    console.error('Failed to load models:', error);
                });
            }
        });

        view.bindModelChange((modelId) => {
            viewModel.selectModel(modelId);
        });

        view.bindSendMessage(async (messageContent) => {
            try {
                await viewModel.sendMessage(messageContent);
            } catch (error) {
                view.showError(error.message);
            }
        });

        view.bindClearConversation(() => {
            viewModel.clearConversation();
        });

        view.bindToolsToggle((useTools) => {
            viewModel.useTools = useTools;
        });
    }

    /**
     * Initialize the application
     */
    async function initializeApplication() {
        try {
            // Load initial providers
            const providers = await viewModel.loadProviders();
            console.log('Providers loaded successfully:', providers.length);

            // Focus on message input
            view.focusMessageInput();

        } catch (error) {
            console.error('Failed to initialize application:', error);
            view.showError(`Failed to initialize the chat application: ${error.message}. Please refresh the page.`);
        }
    }
});