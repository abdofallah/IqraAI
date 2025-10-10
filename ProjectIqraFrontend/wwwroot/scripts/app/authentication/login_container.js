$(document).ready(() => {
    $('#loginForm').on('submit', (event) => {
        event.preventDefault();

        const loginButton = $('.login-container button');
        const buttonSpinner = loginButton.find('.spinner-border');

        loginButton.prop('disabled', true);
        buttonSpinner.removeClass('d-none');

        var email = $('.login-container #email').val();
        var password = $('.login-container #password').val();

        if (email && password) {
            var trackEvent = {
                email: email
            }
            const source = getCookie('source');
            if (source && source.trim() !== '') {
                trackEvent.source = source;
            }

            if (umami) {
                try {
                    umami.track('Authentication | Login Function', { loginEvent: trackEvent });
                }
                catch { }
            }

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
                            window.location.href = redirectUrl;
                        }, 10);
                    }
                    else {
                        $('.login-container #errorMessage').removeClass('d-none');
                        setTimeout(() => {
                            $('.login-container #errorMessage').addClass('show').html('<span>' + response.message + '</span>');
                        }, 10);
                        loginButton.prop('disabled', false);
                        buttonSpinner.addClass('d-none');
                    }
                },
                error: (xhr, status, error) => {
                    console.error('Login error occured:', error);
                    $('.login-container #errorMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.login-container #errorMessage').addClass('show').html('<span>Error occured while logging in.<br>Check console logs.</span>');
                    }, 10);

                    loginButton.prop('disabled', false);
                    buttonSpinner.addClass('d-none');
                }
            });
        } else {
            $('#loginForm').addClass('was-validated');

            loginButton.prop('disabled', false);
            buttonSpinner.addClass('d-none');
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

    const eyeIconPath = '/libs/fontawesome-pro-6.5.1-web-dist/svgs/solid/eye.svg';
    const eyeSlashIconPath = '/libs/fontawesome-pro-6.5.1-web-dist/svgs/solid/eye-slash.svg';
    $('.login-container .toggle-password-icon').on('click', function () {
        const passwordField = $('#password');
        const icon = $(this);

        if (passwordField.attr('type') === 'password') {
            passwordField.attr('type', 'text');
            icon.attr('src', eyeIconPath);
            icon.attr('alt', 'Hide password');
        } else {
            passwordField.attr('type', 'password');
            icon.attr('src', eyeSlashIconPath);
            icon.attr('alt', 'Show password');
        }
    });
});