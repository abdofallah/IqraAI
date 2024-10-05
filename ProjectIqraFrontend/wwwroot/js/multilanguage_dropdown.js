class MultiLanguageDropdown {
    constructor(containerId, languages) {
        this.$container = $(`#${containerId}`);
        this.languages = languages;
        this.selectedLanguage = languages[0];
        this.incompleteCount = 0;
        this.render();
    }

    render() {
        const dropdownHtml = `
            <div class="dropdown multilanguage-dropdown">
                <button class="btn btn-dark dropdown-toggle d-flex align-items-center" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                    <span class="incomplete-number bg-danger ms-auto position-absolute p-1 rounded-4 top-0 start-100 translate-middle badge" style="display: none;"></span>
                    <i class="complete-number bg-success fa-solid fa-check ms-auto position-absolute p-1 rounded-4 top-0 start-100 translate-middle" style="display: none; line-height: 13px; font-size: 14px; padding: 5px;"></i>
                    <span class="me-2">${this.selectedLanguage.localeName} | ${this.selectedLanguage.name}</span>
                    <i class="fa-solid fa-circle-check text-success me-auto selected-status"></i>
                </button>
                <ul class="dropdown-menu">
                    ${this.languages.map(lang => `
                        <li>
                            <a class="dropdown-item d-flex align-items-center ${lang.id === this.selectedLanguage.id ? 'active' : ''}" href="#" data-lang-id="${lang.id}">
                                <span class="me-2">${lang.localeName} | ${lang.name}</span>
                                <span class="ms-auto status-indicator" data-status="incomplete">
                                    <i class="fa-solid fa-circle-xmark text-danger"></i>
                                </span>
                            </a>
                        </li>
                    `).join('')}
                </ul>
            </div>
        `;

        this.$container.html(dropdownHtml);
        this.attachEventListeners();
        this.updateIncompleteCount();
    }

    attachEventListeners() {
        this.$container.find('.dropdown-item').on('click', (e) => {
            e.preventDefault();
            const $item = $(e.currentTarget);
            const langId = $item.data('lang-id');
            const previousLanguage = this.selectedLanguage;
            this.selectedLanguage = this.languages.find(lang => lang.id === langId);
            this.updateSelectedLanguage();
            this.updateActiveItem($item);

            if (previousLanguage.id !== this.selectedLanguage.id) {
                this.$container.trigger('languageChanged', [this.selectedLanguage]);
            }
        });
    }

    updateSelectedLanguage() {
        const $button = this.$container.find('.dropdown-toggle');
        const selectedStatus = this.$container.find(`[data-lang-id="${this.selectedLanguage.id}"] .status-indicator`).attr('data-status');
        const statusIcon = selectedStatus === 'complete' 
            ? '<i class="fa-solid fa-circle-check text-success me-auto selected-status"></i>'
            : '<i class="fa-solid fa-circle-xmark text-danger me-auto selected-status"></i>';

        $button.html(`
            <span class="incomplete-number bg-danger ms-auto position-absolute p-1 rounded-4 top-0 start-100 translate-middle badge" style="display: none;"></span>
            <i class="complete-number bg-success fa-solid fa-check ms-auto position-absolute p-1 rounded-4 top-0 start-100 translate-middle" style="display: none; line-height: 13px; font-size: 14px; padding: 5px;"></i>
            <span class="me-2">${this.selectedLanguage.localeName} | ${this.selectedLanguage.name}</span>
            ${statusIcon}
        `);
        this.updateIncompleteCount();
    }

    updateActiveItem($newActiveItem) {
        this.$container.find('.dropdown-item').removeClass('active');
        $newActiveItem.addClass('active');
    }

    setLanguageStatus(langId, status) {
        const $item = this.$container.find(`[data-lang-id="${langId}"]`);
        if ($item.length) {
            const $statusIndicator = $item.find('.status-indicator');
            $statusIndicator.html(status === 'complete' 
                ? '<i class="fa-solid fa-circle-check text-success"></i>'
                : '<i class="fa-solid fa-circle-xmark text-danger"></i>');
            $statusIndicator.attr('data-status', status);
        }
        if (langId === this.selectedLanguage.id) {
            this.updateSelectedLanguage();
        }
        this.updateIncompleteCount();
    }

    updateIncompleteCount() {
        const $incompleteItems = this.$container.find('.status-indicator[data-status="incomplete"]');
        this.incompleteCount = $incompleteItems.length;

        const $badge = this.$container.find('.incomplete-number');
        const $checkmark = this.$container.find('.complete-number');

        if (this.incompleteCount > 0) {
            $badge.text(this.incompleteCount).show();
            $checkmark.hide();
        } else {
            $badge.hide();
            $checkmark.show();
        }
    }

    getSelectedLanguage() {
        return this.selectedLanguage;
    }

    onLanguageChange(callback) {
        this.$container.on('languageChanged', (event, language) => {
            callback(language);
        });
    }
}