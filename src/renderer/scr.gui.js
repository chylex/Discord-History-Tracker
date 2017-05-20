var GUI = (function(){
  var eventOnFileUploaded;
  var eventOnOptMessagesPerPageChanged;
  var eventOnNavButtonClicked;
  
  var showModal = function(width, height, html){
    var dialog = DOM.id("dialog");
    dialog.innerHTML = html;
    
    dialog.style.width = width+"px";
    dialog.style.height = height+"px";
    dialog.style.marginLeft = (-width/2)+"px";
    dialog.style.marginTop = (-height/2)+"px";
    
    DOM.id("modal").classList.add("visible");
    return dialog;
  };
  
  // -------------
  // Modal dialogs
  // -------------
  
  var showSettingsModal = function(){
    showModal(560, 256, [
      "<label><input id='dht-cfg-imgpreviews' type='checkbox'> Image Previews</label><br>",
      "<label><input id='dht-cfg-formatting' type='checkbox'> Message Formatting</label><br>"
    ].join(""));
    
    var setupCheckBox = function(id, settingName){
      var ele = DOM.id(id);
      ele.checked = STATE.settings[settingName];
      ele.addEventListener("change", () => STATE.settings[settingName] = ele.checked);
    };
    
    setupCheckBox("dht-cfg-imgpreviews", "enableImagePreviews");
    setupCheckBox("dht-cfg-formatting", "enableFormatting");
  };
  
  var showInfoModal = function(){
    var linkGH = "https://github.com/chylex/Discord-History-Tracker";
    
    showModal(560, 128, [
      "<p>Discord History Tracker is developed by <a href='https://chylex.com'>chylex</a> as an <a href='"+linkGH+"/blob/master/LICENSE.md'>open source</a> project.</p>",
      "<p>Please, report any issues and suggestions to the <a href='"+linkGH+"/issues'>tracker</a>. If you want to support the development, please spread the word and consider <a href='https://www.patreon.com/chylex'>becoming a patron</a>. Any support is appreciated!</p>",
      "<p>",
      "<a href='"+linkGH+"/issues'>Issue Tracker</a> &nbsp;&mdash;&nbsp; ",
      "<a href='"+linkGH+"'>GitHub Repository</a> &nbsp;&mdash;&nbsp; ",
      "<a href='https://twitter.com/chylexmc'>Developer's Twitter</a>",
      "</p>"
    ].join(""));
  };
  
  return {
    // ---------
    // GUI setup
    // ---------
    
    /*
     * Hooks all event listeners into the DOM.
     */
    setup: function(){
      var inputUploadedFile = DOM.id("uploaded-file");

      DOM.id("btn-upload-file").addEventListener("click", () => {
        inputUploadedFile.click();
      });
      
      inputUploadedFile.addEventListener("change", () => {
        if (eventOnFileUploaded && eventOnFileUploaded(inputUploadedFile.files)){
          inputUploadedFile.value = null;
        }
      });
      
      DOM.id("opt-messages-per-page").addEventListener("change", () => {
        eventOnOptMessagesPerPageChanged && eventOnOptMessagesPerPageChanged();
      });
      
      DOM.tag("button", DOM.fcls("nav")).forEach(button => {
        button.disabled = true;
        
        button.addEventListener("click", () => {
          eventOnNavButtonClicked && eventOnNavButtonClicked(button.getAttribute("data-nav"));
        });
      });
      
      DOM.id("btn-settings").addEventListener("click", () => {
        showSettingsModal();
      });
      
      DOM.id("btn-about").addEventListener("click", () => {
        showInfoModal();
      });
      
      DOM.id("overlay").addEventListener("click", () => {
        DOM.id("modal").classList.remove("visible");
        DOM.id("dialog").innerHTML = "";
      });
    },
    
    // -----------------
    // Event registering
    // -----------------
    
    /*
     * Sets a callback for when a file is uploaded. The callback takes a single argument, which is the file object array, and should return true to reset the input.
     */
    onFileUploaded: function(callback){
      eventOnFileUploaded = callback;
    },
    
    /*
     * Sets a callback for when the user changes the messages per page option. The callback is not passed any arguments.
     */
    onOptionMessagesPerPageChanged: function(callback){
      eventOnOptMessagesPerPageChanged = callback;
    },
    
    /*
     * Sets a callback for when the user clicks a navigation button. The callback is passed one of the following strings: first, prev, next, last.
     */
    onNavigationButtonClicked: function(callback){
      eventOnNavButtonClicked = callback;
    },
    
    // ----------------------
    // Options and navigation
    // ----------------------
    
    /*
     * Returns the selected amount of messages per page.
     */
    getOptionMessagesPerPage: function(){
      return parseInt(DOM.id("opt-messages-per-page").value, 10);
    },
    
    /*
     * Updates the navigation text and buttons.
     */
    updateNavigation: function(currentPage, totalPages){
      DOM.id("nav-page-current").innerHTML = currentPage;
      DOM.id("nav-page-total").innerHTML = totalPages || "?";
      
      DOM.id("nav-first").disabled = currentPage === 1;
      DOM.id("nav-prev").disabled = currentPage === 1;
      DOM.id("nav-next").disabled = currentPage === (totalPages || 1);
      DOM.id("nav-last").disabled = currentPage === (totalPages || 1);
    },
    
    // ------------
    // Channel list
    // ------------
    
    /*
     * Updates the channel list and sets up their click events. The callback is triggered whenever a channel is selected, and takes the channel ID as its argument.
     */
    updateChannelList: function(channels, callback){
      var eleChannels = DOM.id("channels");
      
      if (!channels){
        eleChannels.innerHTML = "";
      }
      else{
        eleChannels.innerHTML = channels.map(channel => DISCORD.getChannelHTML(channel)).join("");
        
        Array.prototype.forEach.call(eleChannels.children, ele => {
          ele.addEventListener("click", e => {
            var currentChannel = DOM.fcls("active", eleChannels);

            if (currentChannel){
              currentChannel.classList.remove("active");
            }

            ele.classList.add("active");
            callback(ele.getAttribute("data-channel"));
          });
        });
      }
    },
    
    // ------------
    // Message list
    // ------------
    
    /*
     * Updates the message list.
     */
    updateMessageList: function(messages){
      DOM.id("messages").innerHTML = messages ? messages.map(message => DISCORD.getMessageHTML(message)).join("") : "";
    },
    
    /*
     * Scrolls the message div to the top.
     */
    scrollMessagesToTop: function(){
      DOM.id("messages").scrollTop = 0;
    }
  };
})();