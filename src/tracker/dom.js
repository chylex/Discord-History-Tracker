var DOM = (function(){
  var setupTimerFunction = function(setFunc, clearFunc, callback, time){
    var obj = {
      cancel: function(){
        if (this.isActive){
          clearFunc(this.id);
          this.isActive = false;
        }
      },
      
      start: function(){
        if (!this.isActive){
          this.id = setFunc(this._callback.bind(obj), this._time);
          this.isActive = true;
        }
      },
      
      _callback: callback,
      _time: time
    };
    
    obj.start();
    return obj;
  };
  
  return {
    /*
     * Returns a child element by its ID. Parent defaults to the entire document.
     */
    id: function(id, parent){
      return (parent || document).getElementById(id);
    },
    
    /*
     * Returns an array of all child elements containing the specified class. Parent defaults to the entire document.
     */
    cls: function(cls, parent){
      return Array.prototype.slice.call((parent || document).getElementsByClassName(cls));
    },
    
    /*
     * Returns an array of all child elements that have the specified tag. Parent defaults to the entire document.
     */
    tag: function(tag, parent){
      return Array.prototype.slice.call((parent || document).getElementsByTagName(tag));
    },
    
    /*
     * Creates an element, adds it to the DOM, and returns it.
     */
    createElement: function(tag, parent){
      var ele = document.createElement(tag);
      parent.appendChild(ele);
      return ele;
    },
    
    /*
     * Removes an element from the DOM.
     */
    removeElement: function(ele){
      ele.parentNode.removeChild(ele);
    },
    
    /*
     * Creates a new style element with the specified CSS contents and returns it.
     */
    createStyle: function(styles){
      var ele = document.createElement("style");
      document.head.appendChild(ele);
      
      styles.forEach(function(style){
        ele.sheet.insertRule(style, 0);
      });
      
      return ele;
    },
    
    /*
     * Runs a callback function after a set amount of time. Returns an object which contains several functions and properties for easy management.
     */
    setTimer: function(callback, timeout){
      return setupTimerFunction(window.setTimeout, window.clearTimeout, callback, timeout);
    },
    
    /*
     * Runs a callback function periodically after a set amount of time. Returns an object which contains several functions and properties for easy management.
     */
    setInterval: function(callback, interval){
      return setupTimerFunction(window.setInterval, window.clearInterval, callback, interval);
    },
    
    /*
     * Triggers a UTF-8 text file download.
     */
    downloadTextFile: function(fileName, fileContents){
      var blob = new Blob([fileContents], { "type": "octet/stream" });
      var url = window.URL.createObjectURL(blob);
      
      var ele = document.createElement("a");
      ele.href = url;
      ele.download = fileName;
      ele.style.display = "none";
      
      document.body.appendChild(ele);
      ele.click();
      
      document.body.removeChild(ele);
      window.URL.revokeObjectURL(url);
    }
  };
})();
