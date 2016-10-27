document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  
  var btnUploadFile = DOM.id("upload-file");
  var inputUploadedFile = DOM.id("uploaded-file");
  
  btnUploadFile.addEventListener("click", () => {
    inputUploadedFile.click();
  });
  
  inputUploadedFile.addEventListener("change", () => {
    if (inputUploadedFile.files.length === 1){
      var reader = new FileReader();
      
      reader.onload = function(){
        var obj = {};
        
        try{
          obj = JSON.parse(reader.result);
        }catch(e){
          alert("Could not parse '"+file.name+"', see console for details.");
          console.error(e);
          return;
        }
        
        if (SAVEFILE.isValid(obj)){
          STATE.uploadFile(new SAVEFILE(obj));
          reset();
        }
        else{
          alert("File '"+file.name+"' has an invalid format.");
        }
      };
      
      reader.readAsText(inputUploadedFile.files[0], "UTF-8");
    }

    inputUploadedFile.value = null;
  });
  
  var reset = function(){
    var eleChannels = DOM.id("channels");
    var eleMessages = DOM.id("messages");
    
    eleChannels.innerHTML = STATE.getChannelList().map(channel => DISCORD.getChannelHTML(channel)).join("");
    eleMessages.innerHTML = "";
    
    Array.prototype.forEach.call(eleChannels.children, ele => {
      ele.addEventListener("click", e => {
        var currentChannel = DOM.cls("active", eleChannels)[0];
        
        if (currentChannel){
          currentChannel.classList.remove("active");
          eleMessages.innerHTML = "";
        }
        
        ele.classList.add("active");
        
        STATE.selectChannel(ele.getAttribute("data-channel"));
        eleMessages.innerHTML = STATE.getMessageList(0).map(message => DISCORD.getMessageHTML(message)).join("");
      });
    });
  };
});
