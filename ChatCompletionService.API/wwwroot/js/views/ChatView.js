/**
 * ChatView - Handles DOM manipulation and rendering
 * Separates UI concerns from business logic
 */
export class ChatView {
    constructor() {
        this.providerSelect = document.getElementById('provider-select');
        this.modelSelect = document.getElementById('model-select');
        this.chatWindow = document.getElementById('chat-window');
        this.messageInput = document.getElementById('message-input');
        this.sendButton = document.getElementById('send-button');
        this.clearButton = document.getElementById('clear-button');

        this.messageElements = new Map(); // Track DOM elements for messages
    }

    // Provider and Model UI Management
    renderProviders(providers) {
        this.providerSelect.innerHTML = '';

        if (providers.length === 0) {
            this._addPlaceholderOption(this.providerSelect, 'No providers available');
            return;
        }

        providers.forEach(provider => {
            const option = document.createElement('option');
            option.value = provider.id;
            option.textContent = provider.displayName;
            this.providerSelect.appendChild(option);
        });
    }

    renderModels(models) {
        this.modelSelect.innerHTML = '';

        if (models.length === 0) {
            this._addPlaceholderOption(this.modelSelect, 'No models available');
            return;
        }

        models.forEach(model => {
            const option = document.createElement('option');
            option.value = model.id;
            option.textContent = model.displayName;
            this.modelSelect.appendChild(option);
        });
    }

    selectProvider(providerId) {
        this.providerSelect.value = providerId;
    }

    selectModel(modelId) {
        this.modelSelect.value = modelId;
    }

    // Message Rendering
    renderMessage(message, isUpdate = false) {
        const messageId = this._getMessageId(message);

        if (!isUpdate && this.messageElements.has(messageId)) {
            return; // Message already exists
        }

        let messageElement = this.messageElements.get(messageId);

        if (!messageElement) {
            messageElement = this._createMessageElement(message);
            this.messageElements.set(messageId, messageElement);
            this.chatWindow.appendChild(messageElement);
        } else {
            // Update existing message content
            this._updateMessageElement(messageElement, message);
        }

        this._scrollToBottom();
        return messageElement;
    }

    renderStreamingUpdate(message, content) {
        // For streaming updates, we need a message object to generate ID
        // If no message provided, create a temporary one for tracking
        const streamingMessage = message || {
            role: 'Assistant',
            content: content,
            timestamp: new Date(),
            isStreaming: true
        };

        const messageId = this._getMessageId(streamingMessage);
        let messageElement = this.messageElements.get(messageId);

        if (messageElement) {
            // Find the actual message bubble inside the container
            const messageBubble = messageElement.querySelector('div');
            if (messageBubble) {
                // For streaming updates, parse markdown if it's an assistant message
                if (streamingMessage.role === 'Assistant') {
                    messageBubble.innerHTML = marked.parse(content);
                    // Ensure prose classes are present
                    if (!messageBubble.classList.contains('prose')) {
                        messageBubble.classList.add('prose', 'prose-slate', 'prose-sm');
                    }
                    
                    // Highlight code blocks
                    messageBubble.querySelectorAll('pre code').forEach((block) => {
                        hljs.highlightElement(block);
                    });
                    
                    // Add copy buttons to code blocks
                    messageBubble.querySelectorAll('pre').forEach((pre) => {
                        const wrapper = document.createElement('div');
                        wrapper.className = 'relative group';
                        pre.parentNode.insertBefore(wrapper, pre);
                        wrapper.appendChild(pre);
                        
                        const copyBtn = document.createElement('button');
                        copyBtn.innerHTML = '📋';
                        copyBtn.className = 'absolute top-2 right-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs';
                        copyBtn.onclick = () => {
                            navigator.clipboard.writeText(pre.textContent);
                            copyBtn.innerHTML = '✓';
                            setTimeout(() => copyBtn.innerHTML = '📋', 2000);
                        };
                        wrapper.appendChild(copyBtn);
                    });
                } else {
                    // Security: Always use textContent for user messages to prevent XSS
                    messageBubble.textContent = content;
                }
            }
        } else {
            // Create temporary element for streaming
            messageElement = this._createMessageElement(streamingMessage);
            this.messageElements.set(messageId, messageElement);
            this.chatWindow.appendChild(messageElement);
        }

        this._scrollToBottom();
    }

    clearMessages() {
        this.chatWindow.innerHTML = '';
        this.messageElements.clear();
    }

    // UI State Management
    setLoading(isLoading) {
        this.sendButton.disabled = isLoading;
        this.sendButton.textContent = isLoading ? 'Sending...' : 'Send';

        if (isLoading) {
            this.sendButton.classList.add('opacity-50', 'cursor-not-allowed');
        } else {
            this.sendButton.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }

    focusMessageInput() {
        this.messageInput.focus();
    }

    clearMessageInput() {
        this.messageInput.value = '';
    }

    // Error Display
    showError(message) {
        const errorElement = document.createElement('div');
        errorElement.className = 'p-3 mb-2 bg-red-100 border border-red-400 text-red-700 rounded-lg';
        errorElement.textContent = `⚠️ ${message}`;

        // Insert at the top of chat window
        this.chatWindow.insertBefore(errorElement, this.chatWindow.firstChild);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (errorElement.parentNode) {
                errorElement.parentNode.removeChild(errorElement);
            }
        }, 5000);

        this._scrollToBottom();
    }

    // Event Binding
    bindProviderChange(handler) {
        this.providerSelect.addEventListener('change', (e) => {
            handler(e.target.value);
        });
    }

    bindModelChange(handler) {
        this.modelSelect.addEventListener('change', (e) => {
            handler(e.target.value);
        });
    }

    bindSendMessage(handler) {
        const sendHandler = () => {
            const message = this.messageInput.value.trim();
            if (message) {
                handler(message);
            }
        };

        this.sendButton.addEventListener('click', sendHandler);

        this.messageInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendHandler();
            }
        });
    }

    bindClearConversation(handler) {
        this.clearButton.addEventListener('click', handler);
    }

    // Private helper methods
    _createMessageElement(message) {
        const container = document.createElement('div');
        container.className = this._getMessageContainerClasses(message.role);
        
        const element = document.createElement('div');
        element.className = this._getMessageClasses(message.role);
        
        // Parse markdown for assistant messages, keep user messages as plain text
        if (message.role === 'Assistant') {
            // Use marked.js to parse markdown content
            element.innerHTML = marked.parse(message.content);
            // Add prose classes for typography
            element.classList.add('prose', 'prose-slate', 'prose-sm');
            
            // Highlight code blocks
            element.querySelectorAll('pre code').forEach((block) => {
                hljs.highlightElement(block);
            });
            
            // Add copy buttons to code blocks
            element.querySelectorAll('pre').forEach((pre) => {
                const wrapper = document.createElement('div');
                wrapper.className = 'relative group';
                pre.parentNode.insertBefore(wrapper, pre);
                wrapper.appendChild(pre);
                
                const copyBtn = document.createElement('button');
                copyBtn.innerHTML = '📋';
                copyBtn.className = 'absolute top-2 right-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs';
                copyBtn.onclick = () => {
                    navigator.clipboard.writeText(pre.textContent);
                    copyBtn.innerHTML = '✓';
                    setTimeout(() => copyBtn.innerHTML = '📋', 2000);
                };
                wrapper.appendChild(copyBtn);
            });
        } else {
            // Security: Always use textContent for user messages to prevent XSS
            element.textContent = message.content;
        }
        
        container.appendChild(element);
        return container;
    }

    _updateMessageElement(element, message) {
        // Find the actual message bubble inside the container
        const messageBubble = element.querySelector('div');
        if (messageBubble) {
            // Parse markdown for assistant messages, keep user messages as plain text
            if (message.role === 'Assistant') {
                messageBubble.innerHTML = marked.parse(message.content);
                // Ensure prose classes are present
                if (!messageBubble.classList.contains('prose')) {
                    messageBubble.classList.add('prose', 'prose-slate', 'prose-sm');
                }
                
                // Highlight code blocks
                messageBubble.querySelectorAll('pre code').forEach((block) => {
                    hljs.highlightElement(block);
                });
                
                // Add copy buttons to code blocks
                messageBubble.querySelectorAll('pre').forEach((pre) => {
                    const wrapper = document.createElement('div');
                    wrapper.className = 'relative group';
                    pre.parentNode.insertBefore(wrapper, pre);
                    wrapper.appendChild(pre);
                    
                    const copyBtn = document.createElement('button');
                    copyBtn.innerHTML = '📋';
                    copyBtn.className = 'absolute top-2 right-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs';
                    copyBtn.onclick = () => {
                        navigator.clipboard.writeText(pre.textContent);
                        copyBtn.innerHTML = '✓';
                        setTimeout(() => copyBtn.innerHTML = '📋', 2000);
                    };
                    wrapper.appendChild(copyBtn);
                });
            } else {
                // Security: Always use textContent for user messages to prevent XSS
                messageBubble.textContent = message.content;
            }
        }
    }

    _getMessageContainerClasses(role) {
        if (role === 'User') {
            return 'flex justify-end';
        } else {
            return 'flex justify-start';
        }
    }
    
    _getMessageClasses(role) {
        const baseClasses = 'p-3 rounded-lg mb-2 break-words';
        
        if (role === 'User') {
            return `${baseClasses} bg-indigo-500 text-white ml-auto max-w-2xl`;
        } else {
            return `${baseClasses} bg-gray-200 text-gray-800 mr-auto max-w-3xl`;
        }
    }

    _getMessageId(message) {
        // Create a stable ID based on timestamp and role only
        // Content changes during streaming, so we can't use it for ID
        if (message.id) {
            return message.id;
        }
        return `${message.role}_${message.timestamp.getTime()}`;
    }

    _addPlaceholderOption(selectElement, text) {
        const option = document.createElement('option');
        option.value = '';
        option.textContent = text;
        option.disabled = true;
        selectElement.appendChild(option);
    }

    _scrollToBottom() {
        this.chatWindow.scrollTop = this.chatWindow.scrollHeight;
    }

    // Typing indicator methods
    showTypingIndicator() {
        const indicator = document.createElement('div');
        indicator.id = 'typing-indicator';
        indicator.className = 'p-3 rounded-lg mb-2 bg-gray-200 max-w-xs mr-auto';
        indicator.innerHTML = '<span class="flex space-x-1"><span class="w-2 h-2 bg-gray-500 rounded-full animate-bounce"></span><span class="w-2 h-2 bg-gray-500 rounded-full animate-bounce" style="animation-delay: 0.1s"></span><span class="w-2 h-2 bg-gray-500 rounded-full animate-bounce" style="animation-delay: 0.2s"></span></span>';
        this.chatWindow.appendChild(indicator);
        this._scrollToBottom();
    }

    hideTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) indicator.remove();
    }

    // Accessibility helpers
    setAriaLabel(elementId, label) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-label', label);
        }
    }

    setAriaDisabled(elementId, disabled) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-disabled', disabled.toString());
        }
    }
}