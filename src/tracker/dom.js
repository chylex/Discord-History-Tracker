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
  
  var createElement = (tag, parent) => {
    var ele = document.createElement(tag);
    parent.appendChild(ele);
    return ele;
  };
  
  return {
    /*
     * Returns a child element by its ID. Parent defaults to the entire document.
     */
    id: (id, parent) => (parent || document).getElementById(id),
    
    /*
     * Returns an array of all child elements containing the specified class. Parent defaults to the entire document.
     */
    cls: (cls, parent) => Array.prototype.slice.call((parent || document).getElementsByClassName(cls)),
    
    /*
     * Returns an array of all child elements that have the specified tag. Parent defaults to the entire document.
     */
    tag: (tag, parent) => Array.prototype.slice.call((parent || document).getElementsByTagName(tag)),
    
    /*
     * Returns the first child element containing the specified class. Parent defaults to the entire document.
     */
    fcls: (cls, parent) => (parent || document).getElementsByClassName(cls)[0],
    
    /*
     * Returns the first child element that has the specified tag. Parent defaults to the entire document.
     */
    ftag: (tag, parent) => (parent || document).getElementsByTagName(tag)[0],
    
    /*
     * Creates an element, adds it to the DOM, and returns it.
     */
    createElement: (tag, parent) => createElement(tag, parent),
    
    /*
     * Removes an element from the DOM.
     */
    removeElement: (ele) => ele.parentNode.removeChild(ele),
    
    /*
     * Creates a new style element with the specified CSS contents and returns it.
     */
    createStyle: (styles) => {
      var ele = createElement("style", document.head);
      styles.forEach(rule => ele.sheet.insertRule(rule, 0));
      return ele;
    },
    
    /*
     * Runs a callback function after a set amount of time. Returns an object which contains several functions and properties for easy management.
     */
    setTimer: (callback, timeout) => setupTimerFunction(window.setTimeout, window.clearTimeout, callback, timeout),
    
    /*
     * Convenience addEventListener function to save space after minification.
     */
    listen: (ele, event, callback) => ele.addEventListener(event, callback),
    
    /*
     * Triggers a UTF-8 text file download.
     */
    downloadTextFile: (fileName, fileContents) => {
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
