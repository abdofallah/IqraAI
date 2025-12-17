function showHideButtonSpinnerWithDisableEnable(button, show = true, disableEnable = true) {
    const spinner = `<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>`;
    if (show) {
        if (disableEnable) button.prop("disabled", true);
        button.prepend(spinner);
    }
    else {
        if (disableEnable) button.prop("disabled", false);
        button.find(".spinner-border").remove();
    }
}

/**
 * Creates a standardized card element with common styling and structure.
 *
 * @param {object} options - Configuration for the card.
 * @param {string} options.id - The unique ID for the item represented by the card.
 * @param {string} options.type - The type of item (e.g., 'business', 'agent', 'route', 'campaign', 'post-analysis'). Used for `data-item-type` and a specific class.
 * @param {string} [options.href=null] - Optional URL if the card itself should be a clickable link.
 * @param {number} [options.zIndex=999] - Z-index for stacking context.
 * @param {string} options.visualHtml - HTML string for the visual element (e.g., `<img src="...">` or `<span>${emoji}</span>`).
 * @param {string} options.titleHtml - HTML string for the main H4 title.
 * @param {string} [options.subTitleHtml=''] - Optional HTML string for a subtitle (e.g., H6) if `card-data` is used.
 * @param {string} [options.descriptionHtml=''] - Optional HTML string for a detailed description (H5 with `iqra-card-description` class).
 * @param {string[]} [options.infoLinesHtml=[]] - Array of HTML strings for additional H5 info lines (without `iqra-card-description` styling).
 * @param {string} [options.actionDropdownHtml=''] - Full HTML string for the action dropdown menu.
 * @returns {jQuery} A jQuery object representing the created card element.
 */
function createIqraCardElement(options) {
    const {
        id,
        type,
        href = null,
        zIndex = 999, // Assuming lastBusinessCardIndex-- logic would be handled before calling this if needed
        visualHtml,
        titleHtml,
        subTitleHtml = '',
        descriptionHtml = '',
        infoLinesHtml = [],
        actionDropdownHtml = '',
    } = options;

    const Tag = href ? 'a' : 'div';
    const linkClass = href ? 'iqra-card-link' : '';
    const typeSpecificClass = `${type}-card`; // e.g., 'business-card', 'agent-card'

    // Determine the structure of the header (title and optional subtitle)
    let headerTitleContent = '';
    if (subTitleHtml) {
        // Use .card-data for titles with subtitles (Routing, Campaigns, Post-Analysis)
        headerTitleContent = `
            <div class="card-data">
                <h4 class="iqra-card-title">${titleHtml}</h4>
                ${subTitleHtml}
            </div>
        `;
    } else {
        // Simple H4 title (Business, Agent)
        headerTitleContent = `<h4 class="iqra-card-title">${titleHtml}</h4>`;
    }

    // Generate description section if provided
    let descriptionSection = '';
    if (descriptionHtml) {
        descriptionSection = `
            <div>
                <h5 class="iqra-card-info-line iqra-card-description">
                    <span>${descriptionHtml}</span>
                </h5>
            </div>
        `;
    }

    // Generate additional info lines if provided (primarily for business card's extra details)
    let infoLinesSection = '';
    if (infoLinesHtml.length > 0) {
        infoLinesSection = infoLinesHtml.map(line => `<h5 class="iqra-card-info-line">${line}</h5>`).join('');
    }

    const element = `
        <div class="col-lg-4 col-md-6 col-12">
            <${Tag} class="iqra-card ${linkClass} ${typeSpecificClass} d-flex flex-column align-items-start justify-content-center"
                data-item-id="${id}"
                data-item-type="${type}"
                ${href ? `href="${href}"` : ''}
                style="z-index: ${zIndex};"
            >
                ${actionDropdownHtml}
                
                <div class="iqra-card-header">
                    <div class="iqra-card-visual">
                        ${visualHtml}
                    </div>
                    ${headerTitleContent}
                </div>

                ${descriptionSection}
                ${infoLinesSection}

            </${Tag}>
        </div>
    `;

    return $(element);
}