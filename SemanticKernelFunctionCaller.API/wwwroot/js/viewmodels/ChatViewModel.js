import { ChatModel } from '../models/ChatModel.js';
import { ChatApiService } from '../services/ChatApiService.js';

/**
 * ChatViewModel - Coordinates between Model and API service
 * Handles business logic and orchestrates data flow
 */
export class ChatViewModel {
    constructor() {
        this.model = new ChatModel();
        this.apiService = new ChatApiService();
        this._listeners = [];
    }

    // Model state access
    get conversationHistory() {
        return this.model.conversationHistory;
    }

    get selectedProvider() {
        return this.model.selectedProvider;
    }

    get selectedModel() {
        return this.model.selectedModel;
    }

    get isLoading() {
        return this.model.isLoading;
    }

    get canSendMessage() {
        return this.model.canSendMessage();
    }

    // Provider and Model management
    async loadProviders() {
        try {
            const providers = await this.apiService.fetchProviders();
            this._notifyListeners({ type: 'providersLoaded', providers });
            return providers;
        } catch (error) {
            this._notifyListeners({ type: 'error', error: error.message });
            throw error;
        }
    }

    async loadModelsForProvider(providerId) {
        try {
            const models = await this.apiService.fetchModels(providerId);
            this.model.setSelectedProvider(providerId);
            this._notifyListeners({ type: 'modelsLoaded', models, providerId });
            return models;
        } catch (error) {
            this._notifyListeners({ type: 'error', error: error.message });
            throw error;
        }
    }

    selectProvider(providerId) {
        this.model.setSelectedProvider(providerId);
        this._notifyListeners({ type: 'providerSelected', providerId });
    }

    selectModel(modelId) {
        this.model.setSelectedModel(modelId);
        this._notifyListeners({ type: 'modelSelected', modelId });
    }

    // Message handling
    async sendMessage(messageContent) {
        if (!messageContent.trim()) {
            throw new Error('Message content cannot be empty');
        }

        if (!this.canSendMessage) {
            throw new Error('Please select a provider and model first');
        }

        // Add user message to model
        const userMessage = this.model.addUserMessage(messageContent);
        this.model.setLoading(true);
        this._notifyListeners({ type: 'messageSent', message: userMessage });

        // Create abort controller for this request
        const abortController = new AbortController();
        this.model.setAbortController(abortController);

        try {
            // Stream the response
            const messages = this.model.getConversationForApi();
            const streamingGenerator = this.apiService.streamChatMessage(
                this.selectedProvider,
                this.selectedModel,
                messages,
                abortController.signal
            );

            let fullResponse = '';
            let assistantMessage = null;

            for await (const update of streamingGenerator) {
                // Handle both Content/Content and content (backend sends PascalCase)
                const content = update.Content || update.content || '';
                const isFinal = update.IsFinal !== undefined ? update.IsFinal : update.isFinal;

                if (!isFinal && content) {
                    fullResponse += content;
                    if (!assistantMessage) {
                        assistantMessage = this.model.addAssistantMessage(fullResponse);
                        this._notifyListeners({
                            type: 'streamingStarted',
                            message: assistantMessage,
                            content: fullResponse
                        });
                    } else {
                        assistantMessage.content = fullResponse;
                        this._notifyListeners({
                            type: 'streamingUpdate',
                            message: assistantMessage,
                            content: fullResponse
                        });
                    }
                } else if (isFinal) {
                    if (assistantMessage) {
                        assistantMessage.content = fullResponse;
                        this._notifyListeners({
                            type: 'streamingComplete',
                            message: assistantMessage,
                            content: fullResponse
                        });
                    }
                }
            }

            // Finalize the assistant message
            if (assistantMessage) {
                assistantMessage.content = fullResponse;
                this._notifyListeners({
                    type: 'streamingComplete',
                    message: assistantMessage,
                    content: fullResponse
                });
            }

        } catch (error) {
            if (error.name === 'AbortError') {
                this._notifyListeners({ type: 'streamingAborted' });
            } else {
                console.error('Streaming failed:', error);
                this._notifyListeners({ type: 'error', error: error.message });

                // Add error message to conversation
                this.model.addAssistantMessage(`Error: ${error.message}`);
                this._notifyListeners({
                    type: 'errorMessage',
                    error: error.message
                });
            }
        } finally {
            this.model.setLoading(false);
            this.model.setAbortController(null);
            this._notifyListeners({ type: 'loadingComplete' });
        }
    }

    // Conversation management
    clearConversation() {
        this.model.clearConversation();
        this._notifyListeners({ type: 'conversationCleared' });
    }

    abortCurrentRequest() {
        if (this.model.abortController) {
            this.model.abortController.abort();
            this.model.setAbortController(null);
            this.model.setLoading(false);
            this._notifyListeners({ type: 'requestAborted' });
        }
    }

    // Observer pattern for reactive updates
    subscribe(listener) {
        this._listeners.push(listener);

        // Also subscribe to model changes
        const unsubscribeModel = this.model.subscribe(() => {
            this._notifyListeners({ type: 'modelUpdated' });
        });

        return () => {
            this._listeners = this._listeners.filter(l => l !== listener);
            unsubscribeModel();
        };
    }

    _notifyListeners(event) {
        this._listeners.forEach(listener => {
            try {
                listener(event);
            } catch (error) {
                console.error('Error in listener:', error);
            }
        });
    }

    // Utility methods
    getState() {
        return {
            conversationHistory: this.conversationHistory,
            selectedProvider: this.selectedProvider,
            selectedModel: this.selectedModel,
            isLoading: this.isLoading,
            canSendMessage: this.canSendMessage
        };
    }
}