$(document).ready(() => {
    // --- Step 1: Get email and token from URL ---
    const urlParams = new URLSearchParams(window.location.search);
    const email = urlParams.get('email');
    const token = urlParams.get('token');

    const verifyContainer = $('.verify-container');
    const verifyForm = $('#verifyForm');
    const errorMessageDiv = verifyContainer.find('#errorMessage');
    const successMessageDiv = verifyContainer.find('#successMessage');
    const verifyButton = verifyContainer.find('button[type="submit"]');
    const buttonSpinner = verifyButton.find('.spinner-border');

    // --- Step 2: Pre-flight check for required parameters ---
    if (!email || !token) {
        errorMessageDiv.html('<span>Invalid or expired account verification link. Please request a new one.</span>');
        errorMessageDiv.removeClass('d-none').addClass('show');
        verifyButton.prop('disabled', true);
        return; // Stop execution if parameters are missing
    }

    // --- Step 3: Handle form submission ---
    verifyForm.on('submit', (event) => {
        event.preventDefault();

        // Clear previous validation states
        errorMessageDiv.removeClass('show').addClass('d-none');
        successMessageDiv.removeClass('show').addClass('d-none');

        // --- Step 4: Perform AJAX request ---
        verifyButton.prop('disabled', true);
        buttonSpinner.removeClass('d-none');

        const requestData = {
            email: email,
            token: token
        };

        $.ajax({
            url: '/auth/verify',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(requestData),
            success: (response) => {
                if (response.success) {
                    // --- Step 5: Handle Success ---
                    successMessageDiv.html('<span>Your account has been verified successfully! You can now <a href="/login">log in</a>.</span>');
                    successMessageDiv.removeClass('d-none').addClass('show');

                    // Hide the form fields and button to prevent re-submission
                    verifyForm.find('.mb-2, .mb-4').hide();
                    verifyButton.hide();
                } else {
                    // --- Step 6: Handle Failure ---
                    errorMessageDiv.html('<span>' + (response.message || 'An error occurred.') + '</span>');
                    errorMessageDiv.removeClass('d-none').addClass('show');
                    verifyButton.prop('disabled', false);
                    buttonSpinner.addClass('d-none');
                }
            },
            error: (xhr, status, error) => {
                console.error('Verify account error:', error);
                errorMessageDiv.html('<span>An unexpected error occurred. Please try again later.</span>');
                errorMessageDiv.removeClass('d-none').addClass('show');
                verifyButton.prop('disabled', false);
                buttonSpinner.addClass('d-none');
            }
        });
    });
});