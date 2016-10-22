var HOOKS = (function(){
  return {
    /*
     * Intercepts an AJAX response, and passes the original request open() arguments and the request object itself to the provided function.
     */
    onAjaxResponse: function(func){
      var prevOpen = XMLHttpRequest.prototype.open;
      var prevSend = XMLHttpRequest.prototype.send;
      
      XMLHttpRequest.prototype.open = function(){
        this.$requestArguments = arguments;
        return prevOpen.apply(this, arguments);
      };
      
      XMLHttpRequest.prototype.send = function(){
        var req = this;
        var prevEvent = this.onreadystatechange;
        
        this.onreadystatechange = function(){
          if (req.readyState === XMLHttpRequest.DONE && req.status === 200){
            func(this.$requestArguments, req);
          }
          
          return prevEvent.apply(this, arguments);
        };
        
        return prevSend.apply(this, arguments);
      };
    }
  };
})();