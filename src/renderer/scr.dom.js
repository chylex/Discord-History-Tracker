var DOM = (function(){
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
    }
  };
})();
