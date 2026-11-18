class BootstrapAlertManager {
    constructor(containerId = null) {
        if (containerId) {
            this.container = $(`#${containerId}`);
        } else {
            this.container = $('#BootstrapAlertManager');
            if (this.container.length === 0) {
                this.container = $('<div>', {
                    id: 'BootstrapAlertManager'
                }).appendTo('body');
            }
        }
    }

    createAlert(options = {}) {
        const {
            type = 'danger',
            message = '',
            resultMessage = '',
            fade = true,
            timeout = 0,
            customClassName = '',
            enableDismiss = true
        } = options;

        const alertElement = $('<div>', {
            class: `alert alert-${type} alert-dismissible ${fade ? 'fade' : ''} ${customClassName} d-none`,
            role: 'alert'
        });

        this.appendText(alertElement, message);

        if (resultMessage) {
            const resultElement = $('<code>', {
                class: 'bg-dark p-2 rounded border mt-2',
                style: 'display: block;'
            });
            this.appendText(resultElement, resultMessage);
            alertElement.append(resultElement);
        }

        if (enableDismiss) {
            alertElement.append('<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>');
        }

        this.container.append(alertElement);

        alertElement.removeClass('d-none');
        if (fade) {
            setTimeout(() => {            
                alertElement.addClass('show'); 
            }, 10);
        }

        if (timeout > 0) {
            setTimeout(() => {
                this.dismissAlert(alertElement);
            }, (fade ? (timeout + 300) : timeout));
        }

        return alertElement;
    }

    appendText(element, text) {
        String(text ?? '').split(/<br\s*\/?>|\r?\n/i).forEach((line, index) => {
            if (index > 0) element.append($('<br>'));
            element.append(document.createTextNode(line));
        });
    }

    dismissAlert(alertElement) {
        alertElement.removeClass('show');
        setTimeout(() => {
            alertElement.remove();
        }, 300);
    }

    clearAllAlerts() {
        this.container.find('.alert').each((index, alert) => {
            this.dismissAlert($(alert));
        });
    }
}