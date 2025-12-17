class BootstrapConfirmDialog {
	constructor(options = {}) {
		this.options = Object.assign(
			{
				bodySelectorQuery: "body#body-pd",
				title: "Confirm Action",
				message: "Are you sure you want to proceed?",
				confirmText: "Confirm",
				cancelText: "Cancel",
				confirmButtonClass: "btn-primary",
				cancelButtonClass: "btn-secondary",
				hideCancel: false,
				modalClass: "",
				zIndex: 1051,
			},
			options,
		);

		this.modalId = "bootstrapConfirmDialog" + Math.random().toString(36).substr(2, 9);
		this.createModal();
	}

	createModal() {
		const modal = $(`
            <div class="modal fade ${this.options.modalClass}" id="${this.modalId}" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${this.options.title}</h5>
                        </div>
                        <div class="modal-body">
                            ${this.options.message}
                        </div>
                        <div class="modal-footer">
							${this.options.hideCancel ? "" : `<button type="button" class="btn ${this.options.cancelButtonClass}" id="${this.modalId}-cancel">${this.options.cancelText}</button>`}
                            <button type="button" class="btn ${this.options.confirmButtonClass}" id="${this.modalId}-confirm">${this.options.confirmText}</button>
                        </div>
                    </div>
                </div>
            </div>
        `);

		$(this.options.bodySelectorQuery).append(modal);

		const modalElement = document.getElementById(this.modalId);
		this.modal = new bootstrap.Modal(modalElement, {
			backdrop: "static",
			keyboard: false,
			zIndex: this.options.zIndex,
		});
	}

	show() {
		return new Promise((resolve) => {
			this.modal.show();

			$(`#${this.modalId}-confirm`).on("click", () => {
				this.modal.hide();
				resolve(true);
			});

			$(`#${this.modalId}-cancel`).on("click", () => {
				this.modal.hide();
				resolve(false);
			});

			$(`#${this.modalId}`).on("hidden.bs.modal", () => {
				this.destroy();
			});
		});
	}

	hide() {
		this.modal.hide();
	}

	destroy() {
		$(`#${this.modalId}`).remove();
	}

	getModalElement() {
        return document.getElementById(this.modalId);
    }
}
