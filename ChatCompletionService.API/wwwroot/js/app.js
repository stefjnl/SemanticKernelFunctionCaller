document.addEventListener('DOMContentLoaded', () => {
    const providerSelect = document.getElementById('provider-select');
    const modelSelect = document.getElementById('model-select');
    const chatWindow = document.getElementById('chat-window');
    const messageInput = document.getElementById('message-input');
    const sendButton = document.getElementById('send-button');
    const clearButton = document.getElementById('clear-button');

    let conversationHistory = [];
    let abortController = null;

    const state = {
        selectedProvider: localStorage.getItem('selectedProvider') || '',
        selectedModel: localStorage.getItem('selectedModel') || ''
    };

    async function fetchProviders() {
        try {
            const response = await fetch('/api/chat/providers');
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            const providers = await response.json();
            providerSelect.innerHTML = '';
            providers.forEach(provider => {
                const option = document.createElement('option');
                option.value = provider.id;
                option.textContent = provider.displayName;
                providerSelect.appendChild(option);
            });
            if (state.selectedProvider) {
                providerSelect.value = state.selectedProvider;
            }
            await fetchModels();
        } catch (error) {
            console.error('Error fetching providers:', error);
        }
    }

    async function fetchModels() {
        const providerId = providerSelect.value;
        if (!providerId) return;
        try {
            const response = await fetch(`/api/chat/providers/${providerId}/models`);
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            const models = await response.json();
            modelSelect.innerHTML = '';
            models.forEach(model => {
                const option = document.createElement('option');
                option.value = model.id;
                option.textContent = model.displayName;
                modelSelect.appendChild(option);
            });
            if (state.selectedModel && state.selectedProvider === providerId) {
                modelSelect.value = state.selectedModel;
            }
            saveSelection();
        } catch (error) {
            console.error('Error fetching models:', error);
        }
    }

    function saveSelection() {
        state.selectedProvider = providerSelect.value;
        state.selectedModel = modelSelect.value;
        localStorage.setItem('selectedProvider', state.selectedProvider);
        localStorage.setItem('selectedModel', state.selectedModel);
    }

    function addMessage(role, content) {
        const messageElement = document.createElement('div');
        messageElement.classList.add('p-2', 'rounded-lg', 'mb-2', 'max-w-lg', 'break-words');
        if (role === 'user') {
            messageElement.classList.add('bg-indigo-500', 'text-white', 'self-end', 'ml-auto');
        } else {
            messageElement.classList.add('bg-gray-200', 'text-gray-800', 'self-start', 'mr-auto');
        }
        messageElement.textContent = content;
        chatWindow.appendChild(messageElement);
        chatWindow.scrollTop = chatWindow.scrollHeight;
        return messageElement;
    }

    async function handleSendMessage() {
        const message = messageInput.value.trim();
        if (!message) return;

        addMessage('user', message);
        conversationHistory.push({ role: 'User', content: message });
        messageInput.value = '';
        sendButton.disabled = true;

        const assistantMessageElement = addMessage('assistant', '...');
        let fullResponse = '';
        abortController = new AbortController();

        try {
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    providerId: state.selectedProvider,
                    modelId: state.selectedModel,
                    messages: conversationHistory
                }),
                signal: abortController.signal
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop(); // Keep the last partial line

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const json = line.substring(6);
                        if (json) {
                            const data = JSON.parse(json);
                            if (data.error) {
                                throw new Error(data.error);
                            }
                            if (!data.isFinal) {
                                fullResponse += data.content;
                                assistantMessageElement.textContent = fullResponse;
                                chatWindow.scrollTop = chatWindow.scrollHeight;
                            }
                        }
                    }
                }
            }

            conversationHistory.push({ role: 'Assistant', content: fullResponse });

        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error('Streaming failed:', error);
                assistantMessageElement.textContent = `Error: ${error.message}`;
            }
        } finally {
            sendButton.disabled = false;
            abortController = null;
        }
    }

    function clearConversation() {
        conversationHistory = [];
        chatWindow.innerHTML = '';
        if (abortController) {
            abortController.abort();
        }
    }

    providerSelect.addEventListener('change', fetchModels);
    modelSelect.addEventListener('change', saveSelection);
    sendButton.addEventListener('click', handleSendMessage);
    messageInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSendMessage();
        }
    });
    clearButton.addEventListener('click', clearConversation);

    fetchProviders();
});