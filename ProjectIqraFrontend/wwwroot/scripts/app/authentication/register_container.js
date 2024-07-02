$('#registerForm').on('submit', (event) => {
    event.preventDefault();

    var firstName = $('.register-container #firstname').val();
    var lastName = $('.register-container #lastname').val();
    var email = $('.register-container #email').val();
    var password = $('.register-container #password').val();
    var confirmPassword = $('.register-container #confirm-password').val();

    if (
        firstName
        && lastName
        && email
        && password
        && confirmPassword
        && password === confirmPassword
        && password.length >= 8
    ) {
        $.ajax({
            url: '/auth/register',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ firstName: firstName, lastName: lastName, email: email, password: password }),
            success: (response) => {
                // Handle successful register
                console.log('register successful');

                // Save session ID and authentication key in cookies
                setCookie('userEmail', email, 24);
                setCookie('sessionId', response.sessionId, 24);
                setCookie('authKey', response.authKey, 24);

                // Redirect to a logged-in page or update the UI accordingly
                window.location.href = '/app';
            },
            error: (xhr, status, error) => {
                console.error('register error:', xhr);
                // Display an error message to the user
                $('.register-container #errorMessage').removeClass('d-none');
                setTimeout(() => {
                    $('.register-container #errorMessage').addClass('show').html('<span>Invalid email or password.</span>');
                }, 10);
            }
        });
    } else {
        if (!email) {
            $('.register-container #email').addClass('is-invalid');
        }
        else {
            $('.register-container #email').addClass('is-valid');
        }

        if (!password) {
            $('.register-container #password').addClass('is-invalid');
        }
        else {
            if (password.length < 8) {
                $('.register-container #password').addClass('is-invalid');
            }
            else {
                $('.register-container #password').addClass('is-valid');
            }
        }

        if (password !== confirmPassword) {
            $('.register-container #confirm-password').addClass('is-invalid');
        }
    }
});