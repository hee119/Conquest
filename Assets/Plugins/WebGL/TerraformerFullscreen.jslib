mergeInto(LibraryManager.library, {
  TerraformerSetFullscreen: function (fullscreen) {
    var canvas = document.getElementById("unity-canvas");
    if (!canvas) return;

    if (fullscreen) {
      if (document.fullscreenElement === canvas) return;
      if (canvas.requestFullscreen) {
        var request = canvas.requestFullscreen();
        if (request && request.catch) request.catch(function () {});
      }
    } else if (document.fullscreenElement && document.exitFullscreen) {
      var exit = document.exitFullscreen();
      if (exit && exit.catch) exit.catch(function () {});
    }
  }
});
