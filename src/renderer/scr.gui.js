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
      
      Array.prototype.forEach.call(DOM.tag("button", DOM.cls("nav")[0]), button => {
        button.disabled = true;
        
        button.addEventListener("click", () => {
          eventOnNavButtonClicked && eventOnNavButtonClicked(button.getAttribute("data-nav"));
        });
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
            var currentChannel = DOM.cls("active", eleChannels)[0];

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