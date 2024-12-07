let CurrentTabHasHeader = false;

function setDynamicBodyHeight(containerId = null) {
	$("body").css("overflow", "hidden");

	setTimeout(() => {
		if (containerId == null) {
			containerId = $(".l-navbar .nav_link.active").attr("for");
		}

		const activeTabContainer = $(`#${containerId}`).find(".inner-container");
		const activeTabHeaderContainer = $(`#${containerId}`).find(".inner-header-container");

		const windowHeight = $(window)[0].innerHeight;
		const headerHeight = $("#header")[0].clientHeight;
		const mainContainerWrapperPaddingHeight = parseInt($(".main-container-wrapper").css("padding-top")) + parseInt($(".main-container-wrapper").css("padding-bottom"));

		let headerTextHeight = 60; // get this dynamically but 50 should always be good
		if (activeTabHeaderContainer.length > 0 && activeTabHeaderContainer.hasClass("d-none") === false) {
			headerTextHeight += activeTabHeaderContainer[0].clientHeight;
		}

		const bodyCalculatedHeight = windowHeight - (headerHeight + headerTextHeight + mainContainerWrapperPaddingHeight + 15); // 15 to make sure no random scroll - find out why this is even needed

		activeTabContainer.animate(
			{
				"min-height": `${bodyCalculatedHeight}px`,
			},
			300,
		);

		setTimeout(() => {
			$("body").css("overflow", "initial");
		}, 310);
	}, 10);
}

function setDynamicSidebarHeight() {
	$("body").css("overflow", "hidden");

	setTimeout(() => {
		const windowHeight = $(window)[0].innerHeight;
		const upperNavHeight = $(".upper-navigation")[0].clientHeight;
		const lowerNavHeight = $(".bottom-navigation")[0].clientHeight;

		const totalNavHeight = upperNavHeight + lowerNavHeight;

		if (totalNavHeight > windowHeight) {
			$(".l-navbar").css("max-height", `${windowHeight}px`).css("height", "").css("overflow-y", "scroll");

			$(".bottom-navigation").css("margin-top", "2em");
		} else {
			$(".l-navbar").css("max-height", "").css("height", "100vh").css("overflow-y", "hidden");

			$(".bottom-navigation").css("margin-top", "");
		}

		$("body").css("overflow", "initial");
	}, 10);
}

function changeActiveSidebarLink(toggleId, navId, bodyId, headerId) {
	const toggle = document.getElementById(toggleId);
	const nav = document.getElementById(navId);
	const bodypd = document.getElementById(bodyId);
	const headerpd = document.getElementById(headerId);

	// Validate that all variables exist
	if (toggle && nav && bodypd && headerpd) {
		toggle.addEventListener("click", () => {
			// show navbar
			nav.classList.toggle("show");
			// change icon
			toggle.classList.toggle("fa-xmark");
			// add padding to body
			bodypd.classList.toggle("body-pd");
			// add padding to header
			headerpd.classList.toggle("header-body-pd");
		});
	}
}

$(document).ready(() => {
	$(document).on("click", ".l-navbar .nav_link", async (event) => {
		event.preventDefault();

		const currentElement = $(event.currentTarget);
		const forTab = currentElement.attr("for");

		const activeElement = $(".l-navbar .nav_link.active");
		const activeElementFor = activeElement.attr("for");

		if (activeElementFor === forTab) {
			return;
		}

		if (currentElement.hasClass("disabled")) {
			return;
		}

		const tabChangeEvent = new CustomEvent("tabChange", {
			bubbles: true,
			cancelable: true,
			detail: {
				from: activeElementFor,
				to: forTab,
			},
		});

		// Dispatch event and wait for all listeners to complete
		const listeners = jQuery._data($("#nav-bar")[0], "events")?.tabChange || [];
		const results = await Promise.all(
			listeners.map(
				(listener) =>
					new Promise((resolve) => {
						const result = listener.handler(tabChangeEvent);
						if (result instanceof Promise) {
							result.then(() => resolve(!tabChangeEvent.defaultPrevented));
						} else {
							resolve(!tabChangeEvent.defaultPrevented);
						}
					}),
			),
		);

		// Check if any listener prevented default
		const shouldProceed = results.every((result) => result);

		// If the event was cancelled (preventDefault was called), don't proceed
		if (!shouldProceed) {
			return;
		}

		// hide previous tab and link
		activeElement.removeClass("active");
		$("#tabs-list .main-container.show").each((index, element) => {
			$(element).removeClass("show");
			setTimeout(() => {
				$(element).addClass("d-none");
			}, 150);
		});

		// enable new link
		const newTabElement = $(`#${forTab}`);
		setTimeout(() => {
			currentElement.addClass("active");

			newTabElement.removeClass("d-none");
			setTimeout(() => {
				$(`#${forTab}`).addClass("show");

				setTimeout(() => {
					setDynamicBodyHeight(forTab);
				}, 10);
			}, 10);
		}, 150);
	});

	$(window).on("resize", (event) => {
		setTimeout(() => {
			const currentActiveTabId = $(".l-navbar .nav_link.active").attr("for");
			setDynamicBodyHeight(currentActiveTabId);
		}, 100);
	});

	// Init
	changeActiveSidebarLink("header-toggle", "nav-bar", "body-pd", "header");

	$(document)
		.find(".l-navbar .nav_link")
		.each((index, element) => {
			const containerId = $(element).attr("for");

			setDynamicBodyHeight(containerId);
		});
});
