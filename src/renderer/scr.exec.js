document.addEventListener("DOMContentLoaded", () => {
  DISCORD.setup();
  
  var CURRENTFILE;
  
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
          CURRENTFILE = new SAVEFILE(obj);
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
    
    eleChannels.innerHTML = "";
    eleMessages.innerHTML = "";
    
    CURRENTFILE.getChannelList().forEach(channel => {
      eleChannels.innerHTML += DISCORD.getChannelHTML(channel);
    });
    
    Array.prototype.forEach.call(eleChannels.children, ele => {
      ele.addEventListener("click", e => {
        var currentChannel = DOM.cls("active", eleChannels)[0];
        
        if (currentChannel){
          currentChannel.classList.remove("active");
          eleMessages.innerHTML = "";
        }
        
        ele.classList.add("active");
        
        var html = [];
        var messages = CURRENTFILE.getChannelMessageObject(ele.getAttribute("data-channel"));
        
        for(var key of Object.keys(messages).sort()){
          html.push(DISCORD.getMessageHTML(CURRENTFILE.getMessage(messages[key])));
        }
        
        eleMessages.innerHTML = html.join("");
      });
    });
  };
});
