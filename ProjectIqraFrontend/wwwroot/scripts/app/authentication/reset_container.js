$(document).ready(() => {
    // --- Step 1: Get email and token from URL ---
    const urlParams = new URLSearchParams(window.location.search);
    const email = urlParams.get('email');
    const token = urlParams.get('token');

    const resetContainer = $('.reset-container');
    const resetForm = $('#resetForm');
    const errorMessageDiv = resetContainer.find('#errorMessage');
    const successMessageDiv = resetContainer.find('#successMessage');
    const resetButton = resetContainer.find('button[type="submit"]');
    const buttonSpinner = resetButton.find('.spinner-border');
    const passwordField = resetContainer.find('#password');
    const confirmPasswordField = resetContainer.find('#confirm-password');

    // --- Step 2: Pre-flight check for required parameters ---
    if (!email || !token) {
        errorMessageDiv.html('<span>Invalid or expired password reset link. Please request a new one.</span>');
        errorMessageDiv.removeClass('d-none').addClass('show');
        resetButton.prop('disabled', true);
        passwordField.prop('disabled', true);
        confirmPasswordField.prop('disabled', true);
        return; // Stop execution if parameters are missing
    }

    // --- Step 3: Handle form submission ---
    resetForm.on('submit', (event) => {
        event.preventDefault();

        // Clear previous validation states
        passwordField.removeClass('is-invalid');
        confirmPasswordField.removeClass('is-invalid');
        errorMessageDiv.removeClass('show').addClass('d-none');
        successMessageDiv.removeClass('show').addClass('d-none');

        const newPassword = passwordField.val();
        const confirmPassword = confirmPasswordField.val();

        // --- Step 4: Client-side validation ---
        if (newPassword.length < 8) {
            passwordField.addClass('is-invalid');
            return;
        }
        if (newPassword !== confirmPassword) {
            confirmPasswordField.addClass('is-invalid');
            return;
        }

        // --- Step 5: Perform AJAX request ---
        resetButton.prop('disabled', true);
        buttonSpinner.removeClass('d-none');

        const requestData = {
            email: email,
            token: token,
            newPassword: newPassword
        };

        $.ajax({
            url: '/auth/reset-password',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(requestData),
            success: (response) => {
                if (response.success) {
                    // --- Step 6: Handle Success ---
                    successMessageDiv.html('<span>Your password has been reset successfully! You can now <a href="/login">log in</a> with the new password.</span>');
                    successMessageDiv.removeClass('d-none').addClass('show');

                    // Hide the form fields and button to prevent re-submission
                    resetForm.find('.mb-2, .mb-4').hide();
                    resetButton.hide();
                } else {
                    // --- Step 7: Handle Failure ---
                    errorMessageDiv.html('<span>' + (response.message || 'An error occurred.') + '</span>');
                    errorMessageDiv.removeClass('d-none').addClass('show');
                    resetButton.prop('disabled', false);
                    buttonSpinner.addClass('d-none');
                }
            },
            error: (xhr, status, error) => {
                console.error('Password reset error:', error);
                errorMessageDiv.html('<span>An unexpected error occurred. Please try again later.</span>');
                errorMessageDiv.removeClass('d-none').addClass('show');
                resetButton.prop('disabled', false);
                buttonSpinner.addClass('d-none');
            }
        });
    });

    // --- Helper: Clear validation on input ---
    resetContainer.find('#password, #confirm-password').on('input', () => {
        passwordField.removeClass('is-invalid');
        confirmPasswordField.removeClass('is-invalid');
    });

    // --- Helper: Toggle password visibility ---
    const eyeIconPath = '/libs/fontawesome-pro-6.5.1-web-dist/svgs/solid/eye.svg';
    const eyeSlashIconPath = '/libs/fontawesome-pro-6.5.1-web-dist/svgs/solid/eye-slash.svg';
    resetContainer.find('.toggle-password-icon').on('click', function () {
        const field = $(this).prev('input');
        const icon = $(this);

        if (field.attr('type') === 'password') {
            field.attr('type', 'text');
            icon.attr('src', eyeIconPath);
        } else {
            field.attr('type', 'password');
            icon.attr('src', eyeSlashIconPath);
        }
    });
});