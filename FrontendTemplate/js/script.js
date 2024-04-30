$(document).ready(() =>
               {

                    const showNavbar = (toggleId, navId, bodyId, headerId) =>
                    {
                         const toggle = document.getElementById(toggleId),
                              nav = document.getElementById(navId),
                              bodypd = document.getElementById(bodyId),
                              headerpd = document.getElementById(headerId);

                         // Validate that all variables exist
                         if (toggle && nav && bodypd && headerpd)
                         {
                              toggle.addEventListener('click', () =>
                              {
                                   // show navbar
                                   nav.classList.toggle('show');
                                   // change icon
                                   toggle.classList.toggle('bx-x');
                                   // add padding to body
                                   bodypd.classList.toggle('body-pd');
                                   // add padding to header
                                   headerpd.classList.toggle('header-body-pd');
                              });
                         }
                    };

                    showNavbar('header-toggle', 'nav-bar', 'body-pd', 'header');

                    /*===== LINK ACTIVE =====*/
                    $(document).on('click', '.l-navbar .nav_link', (event) =>
                    {
                         event.preventDefault();

                         let currentElement = $(event.currentTarget);
                         let forTab = currentElement.attr('for');

                         // hide previous tab and link
                         $('.l-navbar .nav_link').removeClass("active");
                         $("#tabs-list .main-container.show").each((index, element) =>
                         {
                              $(element).removeClass("show");
                              setTimeout(() =>
                              {
                                   $(element).addClass("d-none");
                              }, 150);
                         });

                         // enable new link
                         let newTabElement = $("#" + forTab);
                         setTimeout(() =>
                         {
                              currentElement.addClass("active");

                              newTabElement.removeClass("d-none");
                              setTimeout(() =>
                              {
                                   $("#" + forTab).addClass("show");
                              }, 10);
                         }, 150);
                    });
               });