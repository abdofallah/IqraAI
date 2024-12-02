const switchBackToOuntboundCallTab = $("#switchBackToOuntboundCallTab");
const currentOutboundBulkCallName = $("#currentOutboundBulkCallName");

const conversationOutboundList = $("#conversationOutboundList");
const conversationOutboundBulkList = $("#conversationOutboundBulkList");

const switchBackToConversationsListTabButton = $("#switchBackToConversationsListTab");

const conversationListTab = $("#conversationListTab");
const conversationManageTab = $("#conversationManageTab");

switchBackToOuntboundCallTab.on("click", (event) => {
	event.preventDefault();

	conversationOutboundBulkList.removeClass("show");

	setTimeout(() => {
		conversationOutboundBulkList.addClass("d-none");
		conversationOutboundList.removeClass("d-none");

		setTimeout(() => {
			conversationOutboundList.addClass("show");
		}, 10);
	}, 300);
});

switchBackToConversationsListTabButton.on("click", (event) => {
	event.preventDefault();

	conversationManageTab.removeClass("show");

	setTimeout(() => {
		conversationManageTab.removeClass("d-none");
		conversationListTab.removeClass("d-none");

		setTimeout(() => {
			conversationListTab.addClass("show");
		}, 10);
	}, 300);
});

function CreateConversationWavesurfer(containerId) {
	let waveSurferConversation = WaveSurfer.create({
		container: containerId,
		waveColor: "#5f6833",
		progressColor: "#CBE54E",
		height: 70,
		barWidth: 2,
		barHeight: 0.7,
		url: "/assets/surah-nasr.mp3",
		fillParent: true,
		audioRate: 1,
		plugins: [
			WaveSurfer.Hover.create({
				lineColor: "#fff",
				lineWidth: 2,
				labelBackground: "#555",
				labelColor: "#fff",
				labelSize: "11px",
			}),
		],
	});
	waveSurferConversation.load();

	let audioPlayPauseButton = $(containerId).parent().find('.audio-controller button[button-type="start-stop-audio"]');
	audioPlayPauseButton.on("click", (event) => {
		waveSurferConversation.playPause();

		let currentMode = $(event.currentTarget).attr("mode");

		if (currentMode === "play") {
			$(event.currentTarget).attr("mode", "pause");
			$(event.currentTarget).find("i").removeClass("fa-play").addClass("fa-pause");
		} else {
			$(event.currentTarget).attr("mode", "play");
			$(event.currentTarget).find("i").removeClass("fa-pause").addClass("fa-play");
		}
	});

	waveSurferConversation.on("ready", (duration) => {
		audioPlayPauseButton.prop("disabled", false);
	});
}

CreateConversationWavesurfer("#waveform-conversation-ai");
CreateConversationWavesurfer("#waveform-conversation-user");
