var GUI = (function(){
  var eventOnFileUploaded;
  var eventOnOptMessagesPerPageChanged;
  var eventOnOptMessageFilterChanged;
  var eventOnNavButtonClicked;
  
  var getActiveFilter = function(){
    var active = DOM.fcls("active", DOM.id("opt-filter-list"));
    
    return active && active.value !== "" ? {
      "type": active.getAttribute("data-filter-type"),
      "value": active.value
    } : null;
  };
  
  var triggerFilterChanged = function(){
    var activeFilter = getActiveFilter();
    DOM.id("opt-save-filtered").classList.toggle("active", activeFilter != null);
    
    eventOnOptMessageFilterChanged && eventOnOptMessageFilterChanged(activeFilter);
  };
  
  var showModal = function(width, html){
    var dialog = DOM.id("dialog");
    dialog.innerHTML = html;
    dialog.style.width = width+"px";
    dialog.style.marginLeft = (-width/2)+"px";
    
    DOM.id("modal").classList.add("visible");
    return dialog;
  };
  
  // -------------
  // Modal dialogs
  // -------------
  
  var showSettingsModal = function(){
    showModal(560, `
<label><input id='dht-cfg-imgpreviews' type='checkbox'> Image Previews</label><br>
<label><input id='dht-cfg-formatting' type='checkbox'> Message Formatting</label><br>
<label><input id='dht-cfg-useravatars' type='checkbox'> User Avatars</label><br>
<label><input id='dht-cfg-animemoji' type='checkbox'> Animated Emoji</label><br>`);
    
    var setupCheckBox = function(id, settingName){
      var ele = DOM.id(id);
      ele.checked = STATE.settings[settingName];
      ele.addEventListener("change", () => STATE.settings[settingName] = ele.checked);
    };
    
    setupCheckBox("dht-cfg-imgpreviews", "enableImagePreviews");
    setupCheckBox("dht-cfg-formatting", "enableFormatting");
    setupCheckBox("dht-cfg-useravatars", "enableUserAvatars");
    setupCheckBox("dht-cfg-animemoji", "enableAnimatedEmoji");
  };
  
  var showInfoModal = function(){
    var linkGH = "https://github.com/chylex/Discord-History-Tracker";
    
    showModal(560, `
<p>Discord History Tracker is developed by <a href='https://chylex.com'>chylex</a> as an <a href='${linkGH}/blob/master/LICENSE.md'>open source</a> project.</p>
<sub>{{{version:full}}}</sub>
<p>Please, report any issues and suggestions to the <a href='${linkGH}/issues'>tracker</a>. If you want to support the development, please spread the word and consider <a href='https://www.patreon.com/chylex'>becoming a patron</a> or <a href='https://ko-fi.com/chylex'>buying me a coffee</a>. Any support is appreciated!</p>
<p><a href='${linkGH}/issues'>Issue Tracker</a> &nbsp;&mdash;&nbsp; <a href='${linkGH}'>GitHub Repository</a> &nbsp;&mdash;&nbsp; <a href='https://twitter.com/chylexmc'>Developer's Twitter</a></p>`);
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
      var inputMessageFilter = DOM.id("opt-messages-filter");
      var containerFilterList = DOM.id("opt-filter-list");
      
      var resetActiveFilter = function(){
        inputMessageFilter.value = "";
        inputMessageFilter.dispatchEvent(new Event("change"));
        
        DOM.id("opt-filter-contents").value = "";
        DOM.id("opt-save-filtered").classList.remove("active");
      };
      
      DOM.id("btn-upload-file").addEventListener("click", () => {
        inputUploadedFile.click();
      });
      
      inputUploadedFile.addEventListener("change", () => {
        if (eventOnFileUploaded && eventOnFileUploaded(inputUploadedFile.files)){
          inputUploadedFile.value = null;
          resetActiveFilter();
        }
      });
      
      inputMessageFilter.value = ""; // required to prevent browsers from remembering old value
      
      inputMessageFilter.addEventListener("change", () => {
        DOM.cls("active", containerFilterList).forEach(ele => ele.classList.remove("active"));
        
        if (inputMessageFilter.value){
          containerFilterList.querySelector("[data-filter-type='"+inputMessageFilter.value+"']").classList.add("active");
        }
        
        triggerFilterChanged();
      });
      
      Array.prototype.forEach.call(containerFilterList.children, ele => {
        ele.addEventListener(ele.tagName === "SELECT" ? "change" : "input", e => triggerFilterChanged());
      });
      
      DOM.id("opt-messages-per-page").addEventListener("change", () => {
        eventOnOptMessagesPerPageChanged && eventOnOptMessagesPerPageChanged();
      });
      
      DOM.id("btn-save-filtered").addEventListener("click", () => {
        if (confirm("Filtering only removes messages, all users and servers will remain in the new archive. Continue?")){
          STATE.saveFilteredMessages();
        }
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
      
      DOM.id("messages").addEventListener("click", e => {
        var jump = e.target.getAttribute("data-jump");
        
        if (jump){
          resetActiveFilter();
          
          var index = STATE.navigateToMessage(jump);
          DOM.id("messages").children[index].scrollIntoView();
        }
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
     * Sets a callback for when the user changes the active filter. The callback is passed either null or an object such as { type: <filter type>, value: <filter value> }.
     */
    onOptMessageFilterChanged: function(callback){
      eventOnOptMessageFilterChanged = callback;
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
      DOM.id("nav-pick").disabled = (totalPages || 0) <= 1;
      DOM.id("nav-next").disabled = currentPage === (totalPages || 1);
      DOM.id("nav-last").disabled = currentPage === (totalPages || 1);
    },
    
    // --------------
    // Updating lists
    // --------------
    
    /*
     * Updates the channel list and sets up their click events. The callback is triggered whenever a channel is selected, and takes the channel ID as its argument.
     */
    updateChannelList: function(channels, selected, callback){
      var eleChannels = DOM.id("channels");
      
      if (!channels){
        eleChannels.innerHTML = "";
      }
      else{
        if (getActiveFilter() != null){
          channels = channels.filter(channel => channel.msgcount > 0);
        }
        
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
        
        if (selected){
          var activeChannel = eleChannels.querySelector("[data-channel='"+selected+"']");
          activeChannel && activeChannel.classList.add("active");
        }
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
     * Updates the user filter list.
     */
    updateUserList: function(users){
      var eleSelect = DOM.id("opt-filter-user");
      
      while(eleSelect.length > 1){
        eleSelect.remove(1);
      }
      
      var options = [];
      
      for(var key of Object.keys(users)){
        var option = document.createElement("option");
        option.value = key;
        option.text = users[key].name;
        options.push(option);
      }
      
      options.sort((a, b) => a.text.toLocaleLowerCase().localeCompare(b.text.toLocaleLowerCase()));
      options.forEach(option => eleSelect.add(option));
    },

    /*
     * Scrolls the message div to the top.
     */
    scrollMessagesToTop: function(){
      DOM.id("messages").scrollTop = 0;
    }
  };
})();
