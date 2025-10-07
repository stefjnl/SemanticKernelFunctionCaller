/**
 * ChatView - Handles DOM manipulation and rendering
 * Separates UI concerns from business logic
 */
export class ChatView {
    constructor() {
        this.providerSelect = document.getElementById('provider-select');
        this.modelSelect = document.getElementById('model-select');
        this.useToolsCheckbox = document.getElementById('use-tools');
        this.chatWindow = document.getElementById('chat-window');
        this.messageInput = document.getElementById('message-input');
        this.sendButton = document.getElementById('send-button');
        this.clearButton = document.getElementById('clear-button');
        this.pluginsContainer = document.getElementById('plugins-container');

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
            option.value = provider.Id || provider.id;
            option.textContent = provider.DisplayName || provider.displayName;
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
            option.value = model.Id || model.id;
            option.textContent = model.DisplayName || model.displayName;
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
                    
                    // Highlight code blocks and add language badges
                    messageBubble.querySelectorAll('pre code').forEach((block) => {
                        hljs.highlightElement(block);
                        
                        // Add language badge
                        const language = block.className.replace('hljs language-', '').replace('hljs', '') || 'text';
                        const pre = block.parentElement;
                        const badge = document.createElement('div');
                        badge.className = 'absolute top-2 right-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded';
                        badge.textContent = language;
                        pre.appendChild(badge);
                    });
                    
                    // Add copy buttons to code blocks
                    messageBubble.querySelectorAll('pre').forEach((pre) => {
                        const wrapper = document.createElement('div');
                        wrapper.className = 'relative group';
                        pre.parentNode.insertBefore(wrapper, pre);
                        wrapper.appendChild(pre);
                        
                        const copyBtn = document.createElement('button');
                        copyBtn.innerHTML = '📋';
                        copyBtn.className = 'absolute top-2 left-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs transition-opacity';
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

    // Tool invocation display
    showToolInvocation(functionName, content) {
        const toolElement = document.createElement('div');
        toolElement.className = 'p-2 mb-2 bg-blue-50 border border-blue-200 text-blue-700 rounded-lg text-sm';
        toolElement.innerHTML = `<span class="font-medium">🔧 ${functionName}</span>: ${content}`;
        
        this.chatWindow.appendChild(toolElement);
        this._scrollToBottom();
        
        // Auto-remove after 3 seconds
        setTimeout(() => {
            if (toolElement.parentNode) {
                toolElement.parentNode.removeChild(toolElement);
            }
        }, 3000);
    }

    // Plugin display
    renderPlugins(plugins) {
        if (!this.pluginsContainer) return;

        this.pluginsContainer.innerHTML = '';

        if (plugins.length === 0) {
            this.pluginsContainer.innerHTML = '<p class="text-sm text-gray-500">No plugins available</p>';
            return;
        }

        const pluginsList = document.createElement('div');
        pluginsList.className = 'space-y-2';

        plugins.forEach(plugin => {
            const pluginElement = document.createElement('div');
            pluginElement.className = 'p-2 bg-gray-50 rounded border border-gray-200';
            
            const pluginName = document.createElement('div');
            pluginName.className = 'font-medium text-sm text-gray-800';
            pluginName.textContent = `${plugin.PluginName || plugin.pluginName || plugin.plugin || plugin.Plugin}.${plugin.FunctionName || plugin.functionName || plugin.function || plugin.Function}`;
            
            const pluginDesc = document.createElement('div');
            pluginDesc.className = 'text-xs text-gray-600 mt-1';
            pluginDesc.textContent = plugin.Description || plugin.description || 'No description available';
            
            if (plugin.Parameters && plugin.Parameters.length > 0) {
                const paramsElement = document.createElement('div');
                paramsElement.className = 'text-xs text-gray-500 mt-1';
                const params = plugin.Parameters.map(p => p.Name || p.name).join(', ');
                paramsElement.textContent = `Parameters: ${params}`;
                pluginElement.appendChild(paramsElement);
            }
            
            pluginElement.appendChild(pluginName);
            pluginElement.appendChild(pluginDesc);
            pluginsList.appendChild(pluginElement);
        });

        this.pluginsContainer.appendChild(pluginsList);
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

    bindToolsToggle(handler) {
        this.useToolsCheckbox.addEventListener('change', (e) => {
            handler(e.target.checked);
        });
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
            
            // Highlight code blocks and add language badges
            element.querySelectorAll('pre code').forEach((block) => {
                hljs.highlightElement(block);
                
                // Add language badge
                const language = block.className.replace('hljs language-', '').replace('hljs', '') || 'text';
                const pre = block.parentElement;
                const badge = document.createElement('div');
                badge.className = 'absolute top-2 right-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded';
                badge.textContent = language;
                pre.appendChild(badge);
            });
            
            // Add copy buttons to code blocks
            element.querySelectorAll('pre').forEach((pre) => {
                const wrapper = document.createElement('div');
                wrapper.className = 'relative group';
                pre.parentNode.insertBefore(wrapper, pre);
                wrapper.appendChild(pre);
                
                const copyBtn = document.createElement('button');
                copyBtn.innerHTML = '📋';
                copyBtn.className = 'absolute top-2 left-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs transition-opacity';
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
                
                // Highlight code blocks and add language badges
                messageBubble.querySelectorAll('pre code').forEach((block) => {
                    hljs.highlightElement(block);
                    
                    // Add language badge
                    const language = block.className.replace('hljs language-', '').replace('hljs', '') || 'text';
                    const pre = block.parentElement;
                    const badge = document.createElement('div');
                    badge.className = 'absolute top-2 right-2 text-xs text-gray-400 bg-gray-800 px-2 py-1 rounded';
                    badge.textContent = language;
                    pre.appendChild(badge);
                });
                
                // Add copy buttons to code blocks
                messageBubble.querySelectorAll('pre').forEach((pre) => {
                    const wrapper = document.createElement('div');
                    wrapper.className = 'relative group';
                    pre.parentNode.insertBefore(wrapper, pre);
                    wrapper.appendChild(pre);
                    
                    const copyBtn = document.createElement('button');
                    copyBtn.innerHTML = '📋';
                    copyBtn.className = 'absolute top-2 left-2 opacity-0 group-hover:opacity-100 bg-gray-700 text-white px-2 py-1 rounded text-xs transition-opacity';
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
        const baseClasses = 'px-4 py-3 rounded-lg mb-2 break-words';
        
        if (role === 'User') {
            return `${baseClasses} bg-indigo-500 text-white ml-auto max-w-2xl`;
        } else {
            return `${baseClasses} bg-gray-50 text-gray-800 mr-auto max-w-3xl shadow-sm border border-gray-100`;
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