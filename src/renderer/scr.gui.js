var GUI = (function(){
  var eventOnFileUploaded;
  var eventOnOptionMessagesPerPageChanged;
  var eventOnNavigationButtonClicked;
  
  return {
    /*
     * Hooks all event listeners into the DOM.
     */
    setup: function(){
      var inputUploadedFile = DOM.id("uploaded-file");
      var optMessagesPerPage = DOM.id("opt-messages-per-page");

      DOM.id("upload-file").addEventListener("click", () => {
        inputUploadedFile.click();
      });
      
      inputUploadedFile.addEventListener("change", () => {
        if (eventOnFileUploaded && eventOnFileUploaded(inputUploadedFile.files)){
          inputUploadedFile.value = null;
        }
      });
      
      optMessagesPerPage.addEventListener("change", () => {
        eventOnOptionMessagesPerPageChanged && eventOnOptionMessagesPerPageChanged();
      });
      
      Array.prototype.forEach.call(DOM.tag("button", DOM.cls("nav")[0]), button => {
        button.disabled = true;
        
        button.addEventListener("click", () => {
          eventOnNavigationButtonClicked && eventOnNavigationButtonClicked(button.getAttribute("data-nav"));
        });
      });
    },
    
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
      eventOnOptionMessagesPerPageChanged = callback;
    },
    
    /*
     * Sets a callback for when the user clicks a navigation button. The callback is passed one of the following strings: first, prev, next, last.
     */
    onNavigationButtonClicked: function(callback){
      eventOnNavigationButtonClicked = callback;
    },
    
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
    
    /*
     * Updates the message list.
     */
    updateMessageList: function(messages){
      DOM.id("messages").innerHTML = messages ? messages.map(message => DISCORD.getMessageHTML(message)).join("") : "";
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
    
    /*
     * Scrolls the message div to the top.
     */
    scrollMessagesToTop: function(){
      DOM.id("messages").scrollTop = 0;
    },
    
    /*
     * Returns the selected amount of messages per page.
     */
    getOptionMessagesPerPage: function(){
      return parseInt(DOM.id("opt-messages-per-page").value, 10);
    }
  };
})();