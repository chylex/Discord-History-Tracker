var HOOKS = (function(){
  return {
    /*
     * Intercepts an AJAX response, and passes the original request open() arguments and the request object itself to the provided function.
     */
    onAjaxResponse: function(func){
      var proto = XMLHttpRequest.prototype;
      var prevOpen = proto.open;
      var prevSend = proto.send;
      
      proto.open = function(){
        this.$requestArguments = arguments;
        return prevOpen.apply(this, arguments);
      };
      
      proto.send = function(){
        var req = this;
        var prevEvent = this.onreadystatechange;
        
        this.onreadystatechange = function(){
          if (req.readyState === 4 && req.status === 200){
            func(this.$requestArguments, req);
          }
          
          return prevEvent.apply(this, arguments);
        };
        
        return prevSend.apply(this, arguments);
      };
    }
  };
})();
