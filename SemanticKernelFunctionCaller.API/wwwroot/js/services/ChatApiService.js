/**
 * ChatApiService - Handles all API communication
 * Separates API concerns from business logic and UI
 */
export class ChatApiService {
    constructor(baseUrl = '/api/chat') {
        this.baseUrl = baseUrl;
        this.providersBaseUrl = '/api/providers';
    }

    /**
     * Fetches available providers from the API
     * @returns {Promise<Array>} Array of provider objects
     */
    async fetchProviders() {
        try {
            const response = await fetch(`${this.providersBaseUrl}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch providers: ${response.status} ${response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('Error fetching providers:', error);
            throw new Error(`Unable to load providers: ${error.message}`);
        }
    }

    /**
     * Fetches models for a specific provider
     * @param {string} providerId - The provider identifier
     * @returns {Promise<Array>} Array of model objects
     */
    async fetchModels(providerId) {
        if (!providerId) {
            throw new Error('Provider ID is required');
        }

        try {
            const response = await fetch(`${this.providersBaseUrl}/${providerId}/models`);
            if (!response.ok) {
                throw new Error(`Failed to fetch models: ${response.status} ${response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('Error fetching models:', error);
            throw new Error(`Unable to load models for provider ${providerId}: ${error.message}`);
        }
    }

    /**
     * Fetches available plugins from the API
     * @returns {Promise<Array>} Array of plugin objects
     */
    async fetchPlugins() {
        try {
            const response = await fetch(`${this.baseUrl}/plugins`);
            if (!response.ok) {
                throw new Error(`Failed to fetch plugins: ${response.status} ${response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('Error fetching plugins:', error);
            throw new Error(`Unable to load plugins: ${error.message}`);
        }
    }

    /**
     * Sends a chat message and returns the full response
     * @param {string} providerId - The provider identifier
     * @param {string} modelId - The model identifier
     * @param {Array} messages - Array of message objects
     * @param {AbortSignal} signal - Optional abort signal for cancellation
     * @returns {Promise<Object>} Chat response object
     */
    async sendChatMessage(providerId, modelId, messages, signal = null) {
        if (!providerId || !modelId) {
            throw new Error('Provider ID and Model ID are required');
        }

        if (!messages || messages.length === 0) {
            throw new Error('Messages array cannot be empty');
        }

        try {
            const response = await fetch(`${this.baseUrl}/send`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    providerId: providerId,
                    modelId: modelId,
                    messages: messages
                }),
                signal: signal
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
            }

            return await response.json();
        } catch (error) {
            if (error.name === 'AbortError') {
                throw error; // Re-throw abort errors as-is
            }
            console.error('Error sending chat message:', error);
            throw new Error(`Failed to send message: ${error.message}`);
        }
    }

    /**
     * Streams a chat response
     * @param {string} providerId - The provider identifier
     * @param {string} modelId - The model identifier
     * @param {Array} messages - Array of message objects
     * @param {AbortSignal} signal - Optional abort signal for cancellation
     * @param {boolean} useTools - Whether to use tools (Semantic Kernel)
     * @returns {AsyncGenerator} Async generator yielding streaming updates
     */
    async *streamChatMessage(providerId, modelId, messages, signal = null, useTools = false) {
        if (!providerId || !modelId) {
            throw new Error('Provider ID and Model ID are required');
        }

        if (!messages || messages.length === 0) {
            throw new Error('Messages array cannot be empty');
        }

        const endpoint = useTools ? `${this.baseUrl}/stream-with-tools` : `${this.baseUrl}/stream`;
        const requestBody = {
            providerId: providerId,
            modelId: modelId,
            messages: messages
        };

        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestBody),
                signal: signal
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            try {
                while (true) {
                    const { done, value } = await reader.read();
                    if (done) {
                        // Process any remaining data in buffer
                        if (buffer.trim()) {
                            console.log('Processing final buffer:', buffer);
                            yield* processLines(buffer);
                        }
                        break;
                    }

                    // Decode without streaming option to ensure complete UTF-8 sequences
                    const chunk = decoder.decode(value, { stream: false });
                    buffer += chunk;

                    // Process complete lines
                    const lines = buffer.split('\n');
                    buffer = lines.pop(); // Keep the last partial line

                    for (const line of lines) {
                        if (line.trim()) {
                            yield* processLines(line);
                        }
                    }
                }

                function* processLines(line) {
                    if (line.startsWith('data: ')) {
                        const json = line.substring(6).trim();
                        if (json && json !== '[DONE]') {
                            try {
                                const data = JSON.parse(json);
                                if (data.error) {
                                    throw new Error(data.error);
                                }
                                console.log('Streaming data received:', data);
                                yield data;
                            } catch (parseError) {
                                console.warn('Failed to parse streaming data:', json, parseError);
                            }
                        }
                    }
                }
            } finally {
                reader.releaseLock();
            }
        } catch (error) {
            if (error.name === 'AbortError') {
                throw error; // Re-throw abort errors as-is
            }
            console.error('Error streaming chat message:', error);
            throw new Error(`Streaming failed: ${error.message}`);
        }
    }
}