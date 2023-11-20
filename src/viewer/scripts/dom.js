var DOM = (function(){
  var createElement = (tag, parent) => {
    var ele = document.createElement(tag);
    parent.appendChild(ele);
    return ele;
  };
  
  var entityMap = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': '&quot;',
    "'": '&#39;'
  };
  
  var entityRegex = /[&<>"']/g;
  
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
     * Creates an element, adds it to the DOM, and returns it.
     */
    createElement: (tag, parent) => createElement(tag, parent),
    
    /*
     * Removes an element from the DOM.
     */
    removeElement: (ele) => ele.parentNode.removeChild(ele),
    
    /*
     * Converts characters to their HTML entity form.
     */
    escapeHTML: (html) => String(html).replace(entityRegex, s => entityMap[s]),
    
    /*
     * Triggers a UTF-8 text file download.
     */
    downloadTextFile: (fileName, fileContents) => {
      var blob = new Blob([fileContents], { "type": "octet/stream" });
      
      if ("msSaveBlob" in window.navigator){
        return window.navigator.msSaveBlob(blob, fileName);
      }
      
      var url = window.URL.createObjectURL(blob);
      
      var ele = createElement("a", document.body);
      ele.href = url;
      ele.download = fileName;
      ele.style.display = "none";
      
      ele.click();
      
      document.body.removeChild(ele);
      window.URL.revokeObjectURL(url);
    }
  };
})();
