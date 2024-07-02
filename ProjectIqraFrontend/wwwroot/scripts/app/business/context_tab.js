const DAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

$(document).ready(() => {
    const tooltipTriggerList = document.querySelectorAll('#context-tab [data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

    const addNewBrandInformationButton = $("#addNewBrandInformation");
    const brandingBranchInformationList = $("#brandingBranchInformationList");

    const addNewBranchButton = $("#addNewBranchButton");
    const branchesListTab = $("#branchesListTab");
    const branchesManagerTab = $("#branchesManagerTab");

    const currentBranchName = $("#currentBranchName");

    const switchBackToBranchesTab = $("#switchBackToBranchesTab");

    const addContextBranchInformationButton = $("#addContextBranchInformation");
    const contextBranchInformationList = $("#contextBranchInformationList");

    const editBranchOpeningHoursInputsList = $("#editBranchOpeningHoursInputs");

    const editBranchAddTeamMember = $("#editBranchAddTeamMember");
    const editBranchTeamInputsList = $("#editBranchTeamInputs");

    const contextServicesListTab = $("#contextServicesListTab");
    const contextServicesManagerTab = $("#contextServicesManagerTab");

    const addNewServiceButton = $("#addNewServiceButton");

    const switchBackToContextServicesTab = $("#switchBackToContextServicesTab");
    const currentContextServiceName = $("#currentContextServiceName");

    const linkContextServiceBranchSelect = $("#linkContextServiceBranchSelect");
    const linkContextServiceBranchButton = $("#linkContextServiceBranch");
    const linkedContextServiceBranchesList = $("#linkedContextServiceBranchesList");

    const linkContextServiceProductSelect = $("#linkContextServiceProductSelect");
    const linkContextServiceProductButton = $("#linkContextServiceProduct");
    const linkedContextServiceProductsList = $("#linkedContextServiceProductsList");

    const addNewContextServiceInformationButton = $("#addNewContextServiceInformation");
    const contextServiceInformationList = $("#contextServiceInformationList");

    const contextProductsListTab = $("#contextProductsListTab");
    const contextProductsManagerTab = $("#contextProductsManagerTab");

    const addNewProductButton = $("#addNewProductButton");

    const switchBackToContextProductsTab = $("#switchBackToContextProductsTab");
    const currentContextProductName = $("#currentContextProductName");

    const linkContextProductBranchSelect = $("#linkContextProductBranchSelect");
    const linkedContextProductBranchesList = $("#linkedContextProductBranchesList");

    const addNewContextProductInformationButton = $("#addNewContextProductInformation");
    const contextProductInformationList = $("#contextProductInformationList");

    function resetOrClearBranchManager() {
        $('#editBranchUniqueIDInput').val("");
        $('#editBranchNameInput').val("");
        $('#editBranchAddressInput').val("");
        $('#editBranchPhoneInput').val("");
        $('#editBranchEmailInput').val("");
        $('#editBranchWebsiteInput').val("");

        contextBranchInformationList.children().remove();

        DAYS.forEach((day) => {
            editBranchOpeningHoursInputsList.find(`#editBranchOpeningHours${day}Input`).prop("checked", false);
            editBranchOpeningHoursInputsList.find(`[button-type="addBranchWorkingHour"][day-value="${day}"]`).prop("disabled", false);
            editBranchOpeningHoursInputsList.find(`.workingHoursTimingList[day-value="${day}"]`).children().remove();
        });

        editBranchTeamInputsList.children().remove();
        $("#branding-branch-manager-general-tab").click();
    }

    function resetOrClearServiceManager() {
        $("#inputContextServiceName").val("");
        $("#inputContextServiceShortDescription").val("");
        $("#inputContextServiceFullDescription").val("");

        linkContextServiceBranchSelect.children().remove();
        linkedContextServiceBranchesList.children().remove();

        linkContextServiceProductSelect.children().remove();
        linkedContextServiceProductsList.children().remove();

        contextServiceInformationList.children().remove();
    }

    function resetOrClearProductManager() {
        $("#inputContextProductName").val("");
        $("#inputContextProductShortDescription").val("");
        $("#inputContextProductFullDescription").val("");

        linkContextProductBranchSelect.children().remove();
        linkedContextProductBranchesList.children().remove();

        contextProductInformationList.children().remove();
    }

    DAYS.forEach((day) => {
        editBranchOpeningHoursInputsList.append(`
                         <div class="col-12">
                              <div class="mb-2 branchOpeningHoursDayBox">
                                   <div class="d-flex flex-row gap-2 align-items-center mb-2">
                                        <label class="form-label fw-bold my-auto">${day}</label>
                                        <div class="">
                                             <input type="checkbox" class="form-check-input" id="editBranchOpeningHours${day}Input" checkbox-type="branchWorkingHourIsClosed" day-value="${day}">
                                             <label for="editBranchOpeningHours${day}Input" class="form-check-label">is closed?</label>
                                        </div>
                                   </div>

                                   <label class="form-label d-block mb-1">Timings</label>
                                   <button class="btn btn-light btn-sm mb-2" button-type="addBranchWorkingHour" day-value="${day}">Add Timing</button>
                                   <div class="workingHoursTimingList" day-value="${day}">
                                   </div>
                              </div>
                         </div>
                         `);
    });

    addNewBrandInformationButton.on('click', (event) => {
        event.preventDefault();

        brandingBranchInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="brandingInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="brandingInformationRemove"]', (event) => {
        event.preventDefault();

        $(event.currentTarget).parent().parent().remove();
    });

    addNewBranchButton.on('click', (event) => {
        event.preventDefault();

        currentBranchName.text("New Branch");

        branchesListTab.removeClass("show");
        setTimeout(() => {
            branchesListTab.addClass("d-none");

            branchesManagerTab.removeClass("d-none");
            setTimeout(() => {
                branchesManagerTab.addClass("show");
            }, 10);
        }, 150);
    });

    switchBackToBranchesTab.on('click', (event) => {
        event.preventDefault();

        branchesManagerTab.removeClass("show");
        setTimeout(() => {
            resetOrClearBranchManager();

            branchesManagerTab.addClass("d-none");

            branchesListTab.removeClass("d-none");
            setTimeout(() => {
                branchesListTab.addClass("show");
            }, 10);
        }, 150);
    });

    addContextBranchInformationButton.on('click', (event) => {
        event.preventDefault();

        contextBranchInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextBranchInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="contextBranchInformationRemove"]', (event) => {
        event.preventDefault();

        $(event.currentTarget).parent().parent().remove();
    });

    $(document).on('click', '[checkbox-type="branchWorkingHourIsClosed"]', (event) => {
        let dayValue = $(event.currentTarget).attr('day-value');
        let isChecked = $(event.currentTarget).is(':checked');

        $(`[button-type="addBranchWorkingHour"][day-value="${dayValue}"]`).prop('disabled', isChecked);

        let dayTimingsList = $(`.workingHoursTimingList[day-value="${dayValue}"]`);
        if (isChecked) {
            dayTimingsList.addClass('d-none');
        }
        else {
            dayTimingsList.removeClass('d-none');
        }
    });

    $(document).on('click', '[button-type="addBranchWorkingHour"]', (event) => {
        let dayValue = $(event.currentTarget).attr('day-value');

        let dayTimingsList = $(`.workingHoursTimingList[day-value="${dayValue}"]`);

        dayTimingsList.append(`
                              <div class="d-flex flex-row mt-1">
                                   <input type="time" class="form-control" time-type="from" style="border-top-right-radius: 0; border-bottom-right-radius: 0">
                                   <input type="time" class="form-control" time-type="to" style="border-radius: 0; border-left: none;">
                                   <button class="btn btn-danger" button-type="removeBranchWorkingHour" style="border-top-left-radius: 0; border-bottom-left-radius: 0">
                                        <i class="fa-regular fa-trash"></i>
                                   </button>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="removeBranchWorkingHour"]', (event) => {
        $(event.currentTarget).parent().remove();
    });

    editBranchAddTeamMember.on('click', (event) => {
        event.preventDefault();

        editBranchTeamInputsList.append(`
                              <div class="col-12 col-md-6 mt-3">
                                   <div class="editBranchTeamBox">
                                        <div class="d-flex flex-row gap-2 mb-2">
                                             <div class="w-100">
                                                  <label for="editBranchTeamNameInput" class="form-label">Name</label>
                                                  <input type="text" class="form-control" id="editBranchTeamNameInput" placeholder="Name">
                                             </div>
                                             <div class="w-100">
                                                  <label for="editBranchTeamRoleInput" class="form-label">Role</label>
                                                  <input type="text" class="form-control" id="editBranchTeamRoleInput" placeholder="Role">
                                             </div>
                                        </div>
                                        <div class="d-flex flex-row gap-2 mb-2">
                                             <div class="w-100">
                                                  <label for="editBranchTeamEmailInput" class="form-label">Email</label>
                                                  <input type="text" class="form-control" id="editBranchTeamEmailInput" placeholder="Email">
                                             </div>
                                             <div class="w-100">
                                                  <label for="editBranchTeamPhoneInput" class="form-label">Phone</label>
                                                  <input type="tel" class="form-control" id="editBranchTeamPhoneInput" placeholder="Phone">
                                             </div>
                                        </div>

                                        <div class="mb-2">
                                             <label for="editBranchTeamInformationInput" class="form-label">Information</label>
                                             <textarea class="form-control" id="editBranchTeamInformationInput" placeholder="Information" style="min-height: 70px"></textarea>
                                        </div>

                                        <button class="btn btn-danger w-100" button-type="removeEditBranchTeam">
                                             <i class="fa-regular fa-trash"></i>
                                        </button>
                                   </div>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="removeEditBranchTeam"]', (event) => {
        $(event.currentTarget).parent().parent().remove();
    });

    addNewServiceButton.on('click', (event) => {
        event.preventDefault();

        currentContextServiceName.text("New Service");

        contextServicesListTab.removeClass("show");
        setTimeout(() => {
            contextServicesListTab.addClass("d-none");

            contextServicesManagerTab.removeClass("d-none");
            setTimeout(() => {
                contextServicesManagerTab.addClass("show");
            }, 10);
        }, 150);
    });

    switchBackToContextServicesTab.on('click', (event) => {
        event.preventDefault();

        contextServicesManagerTab.removeClass("show");
        setTimeout(() => {
            resetOrClearServiceManager();

            contextServicesManagerTab.addClass("d-none");

            contextServicesListTab.removeClass("d-none");
            setTimeout(() => {
                contextServicesListTab.addClass("show");
            }, 10);
        }, 150);
    });

    addNewContextServiceInformationButton.on('click', (event) => {
        event.preventDefault();

        contextServiceInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextServiceInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="contextServiceInformationRemove"]', (event) => {
        $(event.currentTarget).parent().parent().remove();
    });

    addNewProductButton.on('click', (event) => {
        event.preventDefault();

        currentContextProductName.text("New Product");

        contextProductsListTab.removeClass("show");
        setTimeout(() => {
            contextProductsListTab.addClass("d-none");

            contextProductsManagerTab.removeClass("d-none");
            setTimeout(() => {
                contextProductsManagerTab.addClass("show");
            }, 10);
        }, 150);
    });

    switchBackToContextProductsTab.on('click', (event) => {
        event.preventDefault();

        contextProductsManagerTab.removeClass("show");
        setTimeout(() => {
            resetOrClearProductManager();

            contextProductsManagerTab.addClass("d-none");

            contextProductsListTab.removeClass("d-none");
            setTimeout(() => {
                contextProductsListTab.addClass("show");
            }, 10);
        }, 150);
    });

    addNewContextProductInformationButton.on('click', (event) => {
        event.preventDefault();

        contextProductInformationList.append(`
                              <div class="mt-2">
                                   <div class="input-group">
                                        <input type="text" class="form-control" placeholder="Information Type" aria-label="Information Type" value="" style="border-bottom: none; border-bottom-left-radius: 0;">
                                        <button class="btn btn-danger" button-type="contextProductInformationRemove" style="border-bottom: none; border-bottom-right-radius: 0;">
                                             <i class='fa-regular fa-trash'></i>
                                        </button>
                                   </div>
                                   <textarea class="form-control" placeholder="Information" aria-label="Information" style="border-top-left-radius: 0; border-top-right-radius: 0"></textarea>
                              </div>
                         `);
    });

    $(document).on('click', '[button-type="contextProductInformationRemove"]', (event) => {
        $(event.currentTarget).parent().parent().remove();
    });
});