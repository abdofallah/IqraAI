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
                    if (response.success) {
                        setCookie('userEmail', email, 24);
                        setCookie('sessionId', response.data.sessionId, 24);
                        setCookie('authKey', response.data.authKey, 24);

                        setTimeout(() => {
                            window.location.href = '/';
                        }, 10);
                    }
                    else {
                        $('.login-container #errorMessage').removeClass('d-none');
                        setTimeout(() => {
                            $('.login-container #errorMessage').addClass('show').html('<span>' + response.message + '</span>');
                        }, 10);
                    }
                },
                error: (xhr, status, error) => {
                    console.error('Login error occured:', error);
                    $('.login-container #errorMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.login-container #errorMessage').addClass('show').html('<span>Error occured while logging in.<br>Check console logs.</span>');
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