var GUI = (function(){
  var controller;
  
  var root = {
    showController: function(){
      controller = {};
      
      // styles
      
      controller.styles = DOM.createStyle([
        ".app, .connecting { bottom: 48px !important; }",
        "#dht-ctrl { position: absolute; bottom: 0; width: 100%; height: 48px; background-color: #fff; }",
        "#dht-ctrl button { height: 32px; margin: 8px 0 8px 8px; font-size: 18px; padding: 0 12px; background-color: #adf; }",
        "#dht-ctrl button:disabled { background-color: #d0d0d0; cursor: default; }",
        "#dht-ctrl p { display: inline-block; margin: 14px 12px; }",
        "#dht-ctrl input { display: none; }"
      ]);
      
      // main
      
      controller.ele = DOM.createElement("div", document.body);
      controller.ele.id = "dht-ctrl";
      
      controller.ele.innerHTML = [
        "<button id='dht-ctrl-upload'>Upload Previous File</button>",
        "<button id='dht-ctrl-track'>Start Tracking</button>",
        "<button id='dht-ctrl-download'>Download</button>",
        "<button id='dht-ctrl-reset'>Reset</button>",
        "<p id='dht-ctrl-status'></p>",
        "<input id='dht-ctrl-upload-input' type='file' multiple>"
      ].join("");
      
      // elements
      
      controller.ui = {
        btnUpload: DOM.id("dht-ctrl-upload"),
        btnToggleTracking: DOM.id("dht-ctrl-track"),
        btnDownload: DOM.id("dht-ctrl-download"),
        btnReset: DOM.id("dht-ctrl-reset"),
        textStatus: DOM.id("dht-ctrl-status"),
        inputUpload: DOM.id("dht-ctrl-upload-input")
      };
      
      // events
      
      controller.ui.btnUpload.addEventListener("click", () => {
        controller.ui.inputUpload.click();
      });
      
      controller.ui.btnToggleTracking.addEventListener("click", () => {
        STATE.toggleTracking();
      });
      
      controller.ui.btnDownload.addEventListener("click", () => {
        STATE.downloadSavefile();
      });
      
      controller.ui.btnReset.addEventListener("click", () => {
        STATE.resetState();
      });
      
      controller.ui.inputUpload.addEventListener("change", () => {
        for(var file of controller.ui.inputUpload.files){
          var reader = new FileReader();
          
          reader.onload = function(){
            var obj = {};

            try{
              obj = JSON.parse(reader.result); // TODO check content validity
            }catch(e){
              alert("Could not parse '"+file.name+"', see console for details.");
              console.error(e);
              return;
            }
            
            if (SAVEFILE.isValid(obj)){
              STATE.uploadSavefile(new SAVEFILE(obj));
            }
            else{
              alert("File '"+file.name+"' has an invalid format.");
            }
          };
          
          reader.readAsText(file, "UTF-8");
        }

        controller.ui.inputUpload.value = null;
      });
      
      STATE.onStateChanged(() => {
        if (controller){
          controller.ui.btnDownload.disabled = controller.ui.btnReset.disabled = !STATE.hasSavedData();
          controller.ui.btnToggleTracking.innerHTML = STATE.isTracking() ? "Pause Tracking" : "Start Tracking";
        }
      }, true);
    },
    
    hideController: function(){
      if (controller){
        DOM.removeElement(controller.ele);
        DOM.removeElement(controller.styles);
        controller = null;
      }
    }
  };
  
  return root;
})();
