class ModernChatPopup {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.chatId = `modernChatPopup_${Math.random().toString(36).substr(2, 9)}`;
        
        // Default options
        this.options = {
            headerText: 'Chat Support',
            sendButtonText: 'Send',
            placeholderText: 'Type your message...',
            onUserMessage: null,  // Callback function for user messages
            ...options  // Merge user-provided options
        };

        this.isDragging = false;
        this.dragOffset = { x: 0, y: 0 };
        this.continueButtonVisible = false;
        this.continueResolve = null;

        this.render();
        this.attachEventListeners();
    }

    render() {
        const chatHtml = `
            <div id="${this.chatId}" class="modern-chat-popup">
                <div class="modern-chat-header">
                    ${this.options.headerText}
                </div>
                <div class="modern-chat-body">
                    <!-- Messages will be added here -->
                </div>
                <div class="modern-chat-continue-area" style="display: none;">
                    <button class="btn btn-primary modern-chat-continue-button">Continue</button>
                </div>
                <div class="modern-chat-footer">
                    <div class="input-group">
                        <input type="text" class="form-control modern-chat-message-input" placeholder="${this.options.placeholderText}" disabled>
                        <button class="btn btn-primary modern-chat-send-button" type="button" disabled>${this.options.sendButtonText}</button>
                    </div>
                </div>
            </div>
        `;
        $(`#${this.containerId}`).html(chatHtml);
    }

    attachEventListeners() {
        $(`#${this.chatId} .modern-chat-send-button`).on('click', () => this.sendMessage());
        $(`#${this.chatId} .modern-chat-message-input`).on('keypress', (e) => {
            if (e.which === 13) {
                this.sendMessage();
            }
        });

        // Drag event listeners
        const $header = $(`#${this.chatId} .modern-chat-header`);
        $header.on('mousedown', (e) => this.startDragging(e));
        $(document).on('mousemove', (e) => this.drag(e));
        $(document).on('mouseup', () => this.stopDragging());
    }

    startDragging(e) {
        this.isDragging = true;
        const $chat = $(`#${this.chatId}`);
        const chatRect = $chat[0].getBoundingClientRect();
        this.dragOffset = {
            x: e.clientX - chatRect.left,
            y: e.clientY - chatRect.top
        };
    }

    drag(e) {
        if (!this.isDragging) return;
        const $chat = $(`#${this.chatId}`);
        const chatRect = $chat[0].getBoundingClientRect();
        const newX = e.clientX - this.dragOffset.x;
        const newY = e.clientY - this.dragOffset.y;
        
        const { x, y } = this.getPositionWithinBounds(newX, newY, chatRect.width, chatRect.height);
        
        $chat.css({
            left: `${x}px`,
            top: `${y}px`,
            right: 'auto',
            bottom: 'auto'
        });
    }

    getPositionWithinBounds(x, y, width, height) {
        const windowWidth = $(window).width();
        const windowHeight = $(window).height();

        x = Math.max(0, Math.min(x, windowWidth - width));
        y = Math.max(0, Math.min(y, windowHeight - height));

        return { x, y };
    }

    stopDragging() {
        this.isDragging = false;
    }

    createMessage(message, isUser = true)
    {
        const $messageElement = $('<div>')
            .addClass('modern-chat-message')
            .addClass(isUser ? 'modern-chat-user-message' : 'modern-chat-system-message')
            .html(message);

        return $messageElement;
    }

    addMessage(messageElement) {
        $(`#${this.chatId} .modern-chat-body`).append(messageElement);

        // Adjust scroll position to account for continue button if visible
        const scrollAdjustment = this.continueButtonVisible ? $(`#${this.chatId} .modern-chat-continue-area`).outerHeight() : 0;
        $(`#${this.chatId} .modern-chat-body`).scrollTop($(`#${this.chatId} .modern-chat-body`)[0].scrollHeight - scrollAdjustment);

        return messageElement;
    }

    sendMessage() {
        const $messageInput = $(`#${this.chatId} .modern-chat-message-input`);
        const message = $messageInput.val().trim();
        if (message) {
            this.SendUserMessage(message);
            $messageInput.val('');
            
            // Call the onUserMessage callback if it's defined
            if (typeof this.options.onUserMessage === 'function') {
                this.options.onUserMessage(message);
            }
        }
    }

    async SendUserMessage(message) {
        this.addMessage(this.createMessage(message), true);

        await this.wait(10);

        $(`#${this.chatId} .modern-chat-body`).scrollTop($(`#${this.chatId} .modern-chat-body`)[0].scrollHeight);
    }

    async SendResponseMessage(message, delay = 1300, startDelay = 1000) {
        await this.wait(startDelay);

        // Add typing indicator
        const $typingIndicator = $('<div style="opacity: 0;" class="typing-indicator"><span></span><span></span><span></span></div>');
        this.addMessage($typingIndicator, false);

        await this.wait(10);

        $(`#${this.chatId} .modern-chat-body`).scrollTop($(`#${this.chatId} .modern-chat-body`)[0].scrollHeight);

        $typingIndicator.css('opacity', '1');
        
        // Wait for the specified delay
        await this.wait(delay);

        // Replace typing indicator with the actual message
        const $messageElement = this.createMessage(message, false).hide();
        $typingIndicator.replaceWith($messageElement);
        $messageElement.fadeIn(300);  // Smooth fade in effect

        await this.wait(10);

        $(`#${this.chatId} .modern-chat-body`).scrollTop($(`#${this.chatId} .modern-chat-body`)[0].scrollHeight);

        await new Promise(resolve => setTimeout(resolve, 300)); // with the effect
    }

    async SendResponseMessageWithContinue(message, delay = 1300, startDelay = 1000, continueButtonText = 'Continue', validationFunc = null) {
        await this.SendResponseMessage(message, delay, startDelay);

        const $continueButton = $(`#${this.chatId} .modern-chat-continue-button`);
        const $continueArea = $(`#${this.chatId} .modern-chat-continue-area`);

        $continueButton.text(continueButtonText);
        $continueArea.show();
        this.continueButtonVisible = true;
        this.disableInputState(false);  // Enable user input

        await this.wait(10);
        $(`#${this.chatId} .modern-chat-body`).scrollTop($(`#${this.chatId} .modern-chat-body`)[0].scrollHeight);

        return new Promise(resolve => {
            this.continueResolve = resolve;  // Store the resolve function
            $continueButton.on('click', async () => {
                if (validationFunc && !await validationFunc()) {
                    // If validation fails, don't resolve the promise
                    return;
                }
                this.resolveContinue();
            });
        });
    }

    resolveContinue() {
        if (this.continueResolve) {
            const $continueButton = $(`#${this.chatId} .modern-chat-continue-button`);
            const $continueArea = $(`#${this.chatId} .modern-chat-continue-area`);

            $continueButton.prop('disabled', true);
            $continueArea.hide();
            this.continueButtonVisible = false;
            this.continueResolve();
            this.continueResolve = null;
        }
    }

    async wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    setOptions(newOptions) {
        this.options = {
            ...this.options,
            ...newOptions
        };
        this.updateUI();
    }

    updateUI() {
        $(`#${this.chatId} .modern-chat-header`).text(this.options.headerText);
        $(`#${this.chatId} .modern-chat-send-button`).text(this.options.sendButtonText);
        $(`#${this.chatId} .modern-chat-message-input`).attr('placeholder', this.options.placeholderText);
    }

    disableInputState(state) {
        $(`#${this.chatId} .modern-chat-message-input`).prop('disabled', state);
        $(`#${this.chatId} .modern-chat-send-button`).prop('disabled', state);
    }

    destroy() {
        $(`#${this.chatId} .modern-chat-send-button`).off('click');
        $(`#${this.chatId} .modern-chat-message-input`).off('keypress');
        $(`#${this.chatId} .modern-chat-header`).off('mousedown');
        $(document).off('mousemove', this.drag);
        $(document).off('mouseup', this.stopDragging);

        $(`#${this.chatId}`).remove();

        this.containerId = null;
        this.chatId = null;
        this.options = null;
    }
}