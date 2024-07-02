$(document).ready(() => {
    $('#forgetForm').on('submit', (event) => {
        event.preventDefault();

        var email = $('.forget-container #email').val();

        if (email) {
            $.ajax({
                url: '/auth/request-reset-password',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ email: email }),
                success: (response) => {
                    // Handle password reset email sent

                    $('.forget-container #successMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.forget-container #successMessage').addClass('show').html('<span>A password reset email has been sent to you.</span>');
                    }, 10);
                },
                error: (xhr, status, error) => {
                    console.error('Password reset error:', error);

                    // Display an error message to the user
                    $('.forget-container #errorMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.forget-container #errorMessage').addClass('show').html('<span>Password reset email could not be sent. Please try again.</span>');
                    }, 10);
                }
            });
        }
        else {
            $('.forget-container #email').addClass('is-invalid');
        }
    });
});