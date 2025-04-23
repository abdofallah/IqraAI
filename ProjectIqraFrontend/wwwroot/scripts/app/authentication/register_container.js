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
                if (response.success) {
                    $("#registerForm").addClass("d-none");

                    $('.register-container #errorMessage').addClass('d-none');
                    $(".register-container #successMessage").removeClass("d-none");
                    setTimeout(() => {
                        $(".register-container #successMessage").addClass("show").html('<span>Thank you for registering!<br><br>Please verify your email before you can login.</span>');
                    }, 10);
                }
                else {
                    $('.register-container #errorMessage').removeClass('d-none');
                    setTimeout(() => {
                        $('.register-container #errorMessage').addClass('show').html('<span>' + response.message + '</span>');
                    }, 10);
                }
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