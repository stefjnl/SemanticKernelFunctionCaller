/**
 * ChatModel - Handles application state and data structures
 * Encapsulates conversation history, provider/model selection, and state management
 */
export class ChatModel {
    constructor() {
        this._conversationHistory = [];
        this._selectedProvider = localStorage.getItem('selectedProvider') || '';
        this._selectedModel = localStorage.getItem('selectedModel') || '';
        this._isLoading = false;
        this._abortController = null;
        this._listeners = [];
    }

    // Getters
    get conversationHistory() {
        return [...this._conversationHistory];
    }

    get selectedProvider() {
        return this._selectedProvider;
    }

    get selectedModel() {
        return this._selectedModel;
    }

    get isLoading() {
        return this._isLoading;
    }

    get abortController() {
        return this._abortController;
    }

    // Setters with state management
    setSelectedProvider(providerId) {
        this._selectedProvider = providerId;
        localStorage.setItem('selectedProvider', providerId);
        this._notifyListeners();
    }

    setSelectedModel(modelId) {
        this._selectedModel = modelId;
        localStorage.setItem('selectedModel', modelId);
        this._notifyListeners();
    }

    setLoading(isLoading) {
        this._isLoading = isLoading;
        this._notifyListeners();
    }

    setAbortController(abortController) {
        this._abortController = abortController;
    }

    // Conversation management
    addUserMessage(content) {
        const message = {
            id: `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            role: 'User',
            content: content,
            timestamp: new Date()
        };
        this._conversationHistory.push(message);
        this._notifyListeners();
        return message;
    }

    addAssistantMessage(content) {
        const message = {
            id: `assistant_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            role: 'Assistant',
            content: content,
            timestamp: new Date()
        };
        this._conversationHistory.push(message);
        this._notifyListeners();
        return message;
    }

    clearConversation() {
        this._conversationHistory = [];
        if (this._abortController) {
            this._abortController.abort();
            this._abortController = null;
        }
        this._notifyListeners();
    }

    // Observer pattern for reactive updates
    subscribe(listener) {
        this._listeners.push(listener);
        return () => {
            this._listeners = this._listeners.filter(l => l !== listener);
        };
    }

    _notifyListeners() {
        this._listeners.forEach(listener => listener(this));
    }

    // Utility methods
    getLastMessage() {
        return this._conversationHistory[this._conversationHistory.length - 1];
    }

    getConversationForApi() {
        return this._conversationHistory.map(msg => ({
            role: msg.role,
            content: msg.content
        }));
    }

    canSendMessage() {
        return this._selectedProvider && this._selectedModel && !this._isLoading;
    }
}