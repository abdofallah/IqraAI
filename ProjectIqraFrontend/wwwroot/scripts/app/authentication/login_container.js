$(document).ready(() => {
    $('#loginForm').on('submit', (event) => {
        event.preventDefault();

        var email = $('.login-container #email').val();
        var password = $('.login-container #password').val();

        if (email && password) {
            $.ajax({
                url: '/auth/login',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ email: email, password: password }),
                success: (response) => {
                    // Handle successful login
                    console.log('Login successful');

                    // Save session ID and authentication key in cookies
                    setCookie('userEmail', email, 24);
                    setCookie('sessionId', response.sessionId, 24);
                    setCookie('authKey', response.authKey, 24);

                    // Redirect to a logged-in page or update the UI accordingly
                    window.location.href = '/';
                },
                error: (xhr, status, error) => {
                    console.error('Login error:', error);
                    // Display an error message to the user
                    $('.login-container #errorMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.login-container #errorMessage').addClass('show').html('<span>Invalid email or password.</span>');
                    }, 10);
                }
            });
        } else {
            $('#loginForm').addClass('was-validated');
        }
    });

    $('.login-container #email, .login-container #password').on('input', (event) => {
        $('#loginForm').removeClass('was-validated');
        $(event.currentTarget).removeClass('is-invalid');

        $('.login-container #errorMessage').removeClass('show');
        setTimeout(() => {
            $('.login-container #errorMessage').addClass('d-none');
        }, 200);

        $('.login-container #successMessage').removeClass('show');
        setTimeout(() => {
            $('.login-container #successMessage').addClass('d-none');
        }, 200);
    });
});