var DOM = (function(){
  var createElement = (tag, parent, id, html) => {
    var ele = document.createElement(tag);
    ele.id = id || "";
    ele.innerHTML = html || "";
    parent.appendChild(ele);
    return ele;
  };
  
  return {
    /*
     * Returns a child element by its ID. Parent defaults to the entire document.
     */
    id: (id, parent) => (parent || document).getElementById(id),
    
    /*
     * Returns the first child element containing the specified obfuscated class. Parent defaults to the entire document.
     */
    queryReactClass: (cls, parent) => (parent || document).querySelector(`[class*="${cls}-"]`),
    
    /*
     * Creates an element, adds it to the DOM, and returns it.
     */
    createElement: (tag, parent, id, html) => createElement(tag, parent, id, html),
    
    /*
     * Removes an element from the DOM.
     */
    removeElement: (ele) => ele.parentNode.removeChild(ele),
    
    /*
     * Creates a new style element with the specified CSS and returns it.
     */
    createStyle: (styles) => createElement("style", document.head, "", styles),
    
    /*
     * Convenience setTimeout function to save space after minification.
     */
    setTimer: (callback, timeout) => window.setTimeout(callback, timeout),
    
    /*
     * Convenience addEventListener function to save space after minification.
     */
    listen: (ele, event, callback) => ele.addEventListener(event, callback),
    
    /*
     * Utility function to save an object into a cookie.
     */
    saveToCookie: (name, obj, expiresInSeconds) => {
      var expires = new Date(Date.now()+1000*expiresInSeconds).toUTCString();
      document.cookie = name+"="+encodeURIComponent(JSON.stringify(obj))+";path=/;expires="+expires;
    },
    
    /*
     * Utility function to load an object from a cookie.
     */
    loadFromCookie: (name) => {
      var value = document.cookie.replace(new RegExp("(?:(?:^|.*;\\s*)"+name+"\\s*\\=\\s*([^;]*).*$)|^.*$"), "$1");
      return value.length ? JSON.parse(decodeURIComponent(value)) : null;
    },
    
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
