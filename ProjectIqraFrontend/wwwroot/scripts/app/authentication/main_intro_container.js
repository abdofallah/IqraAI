$(document).ready(function () {
    const canvasElement = $('#imageSequenceCanvas')[0];
    const totalFrames = 127;
    const basePath = '/img/AI/';

    const adjustedBasePath = basePath.endsWith('/') ? basePath : basePath + '/';

    const fileNamePrefix = 'frame';
    const fileExtension = '.jpg';
    const paddingLength = 4;

    const imagesArray = Array.from({ length: totalFrames }, (v, k) => {
        const frameNumber = k + 1;
        const paddedFrameNumber = String(frameNumber).padStart(paddingLength, '0');
        return `${adjustedBasePath}${fileNamePrefix}${paddedFrameNumber}${fileExtension}`;
    });


    // Define options for the AnimateImages instance
    const sequenceOptions = {
        images: imagesArray,     // Array of image URLs
        loop: true,              // Loop the animation
        fps: 30,                 // Play at 30 frames per second
        autoplay: true,          // Start playing automatically after loading
        preload: 'partial',       // Default is 'all', loads all images before playing
        preloadNumber: '1',     // Number of images to preload
        onLoadingProgress: function (progress) {
        },
        onPreloadFinished: function (instance) {
        },
        onAnimationEnd: function (instance) {
        }
    };

    if (typeof AnimateImages === 'undefined') {
        console.error('AnimateImages library not found. Make sure the CDN script is loaded correctly.');
        return;
    }

    try {
        const sequence = new AnimateImages(canvasElement, sequenceOptions);
    } catch (error) {
        console.error('Error initializing AnimateImages:', error);
    }
});