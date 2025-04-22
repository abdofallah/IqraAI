var titleTemplate = '{{authType}} | Iqra AI';

var currentAuthType = 'login';

$(document).ready(() => {
    var URLPath = window.location.pathname;

    if (URLPath === '/register') {
        setAuthType('register');
    }
    else if (URLPath === '/forget') {
        setAuthType('forget');
    }
    else if (URLPath === '/login') {
        setAuthType('login');
    }
    else {
        setAuthType(currentAuthType);
    }

    $("#switchToRegister").on('click', (event) => {
        event.preventDefault();

        setAuthType('register');
    });

    $("#switchToLogin").on('click', (event) => {
        event.preventDefault();

        setAuthType('login');
    });

    $("#switchToLoginCancel").on('click', (event) => {
        event.preventDefault();

        setAuthType('login');
    });

    $("#switchToForget").on('click', (event) => {
        event.preventDefault();

        setAuthType('forget');
    });
});

function setAuthType(authType) {
    var containerId = '.' + authType + '-container';

    $(".main-container").children().each((index, element) => {
        if (element.classList.contains(containerId.replace('.', '')) || element.classList.contains('logo-container') || element.classList.contains('footer-container')) {
            return;
        }

        element.classList.remove('show');
        setTimeout(() => {
            element.classList.add('d-none');
        }, 100);
    });

    $(containerId + " #successMessage").addClass('d-none').removeClass('show');
    $(containerId + " #errorMessage").addClass('d-none').removeClass('show');

    $(containerId + " form").removeClass('d-none');
    $(containerId + " input").removeClass('is-valid').removeClass('is-invalid').val("");

    setTimeout(() => {
        $(containerId).removeClass('d-none');
        setTimeout(() => {
            $(containerId).addClass('show');
        }, 10)

        let currentTitle = titleTemplate.replace('{{authType}}', capitalizeFirstLetter(authType));
        window.history.pushState(authType, currentTitle, ('/' + authType));
        document.title = currentTitle;
    }, 100);

}