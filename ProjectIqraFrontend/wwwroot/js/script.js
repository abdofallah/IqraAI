let CurrentTabHasHeader = false;

let lastRecordedWidth = $(window).width();
let lastRecordedHeight = $(window).height();

let resizeTimeout;

function setDynamicBodyHeight(containerId = null) {
	// Cache DOM lookups and jQuery objects
	const $body = $("body");
	const $window = $(window);
	const $header = $("#header");
	const $mainWrapper = $(".main-container-wrapper");

	// Set initial state
	$body.css("overflow", "hidden");

	// If no containerId provided, get from active nav
	containerId = containerId || $(".l-navbar .nav_link.active").attr("for");

	// Cache container elements
	const $container = $(`#${containerId}`);
	const $innerContainer = $container.find(".inner-container");
	const $headerContainer = $container.find(".inner-header-container");

	// Calculate static measurements
	const windowHeight = $window[0].innerHeight;
	const headerHeight = $header[0].clientHeight;
	const wrapperPadding = parseInt($mainWrapper.css("padding-top")) + parseInt($mainWrapper.css("padding-bottom"));

	// Calculate header text height
	const baseHeaderHeight = 60;
	const additionalHeaderHeight = !$headerContainer.length || $headerContainer.hasClass("d-none") ? 0 : $headerContainer[0].clientHeight;
	const headerTextHeight = baseHeaderHeight + additionalHeaderHeight;

	// Calculate final height
	const bodyCalculatedHeight = windowHeight - (headerHeight + headerTextHeight + wrapperPadding + 15);

	// Create a custom event
	const resizeEvent = new CustomEvent("containerResize", {
		detail: { containerId, targetHeight: bodyCalculatedHeight },
	});

	// Perform animation with progress callback
	$innerContainer.animate(
		{ "min-height": bodyCalculatedHeight },
		{
			duration: 300,
			progress: (animation, progress) => {
				const currentHeight = $container.find(".inner-container")[0].clientHeight;
				// Dispatch event with current height
				document.dispatchEvent(
					new CustomEvent("containerResizeProgress", {
						detail: {
							containerId,
							currentHeight,
							progress,
							targetHeight: bodyCalculatedHeight,
						},
					}),
				);
			},
			complete: () => {
				// Reset body overflow after slight delay to prevent flickering
				setTimeout(() => $body.css("overflow", "initial"), 10);
				// Dispatch completion event
				document.dispatchEvent(resizeEvent);
			},
		},
	);
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
		const hasHrefTag = event.currentTarget.hasAttribute("href");
		if (hasHrefTag) {
			return true;
		}

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

	$(window).on("resize", () => {
		// Clear the existing timeout if there is one
		if (resizeTimeout) {
			clearTimeout(resizeTimeout);
		}

		// Set a new timeout - this will only execute if no resize occurs for 250ms
		resizeTimeout = setTimeout(() => {
			const currentWidth = $(window).width();
			const currentHeight = $(window).height();

			// Only update if dimensions actually changed
			if (lastRecordedWidth !== currentWidth || lastRecordedHeight !== currentHeight) {
				lastRecordedWidth = currentWidth;
				lastRecordedHeight = currentHeight;

				const currentActiveTabId = $(".l-navbar .nav_link.active").attr("for");
				setDynamicBodyHeight(currentActiveTabId);

				console.log("Resize complete");
			}
		}, 250); // Wait for 250ms of no resize events before executing
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
