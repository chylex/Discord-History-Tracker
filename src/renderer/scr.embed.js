var EMBED = (function(){
  var enabled = false;
  
  var html;
  var generated;
  
  var downloadTextFile = function(fileName, fileContents){
    var blob = new Blob([fileContents], { "type": "octet/stream" });
    
    if ("msSaveBlob" in window.navigator){
      return window.navigator.msSaveBlob(blob, fileName);
    }
    
    var url = window.URL.createObjectURL(blob);
    
    var ele = DOM.createElement("a", document.body);
    ele.href = url;
    ele.download = fileName;
    ele.style.display = "none";
    
    ele.click();
    
    document.body.removeChild(ele);
    window.URL.revokeObjectURL(url);
  };
  
  var utoa = function(str){
    return window.btoa(unescape(encodeURIComponent(str)));
  };
  
  var atou = function(str){
    return decodeURIComponent(escape(window.atob(str)));
  };
  
  return {
    setup: function(){
      enabled = true;
      html = "<!DOCTYPE html>\n" + document.documentElement.outerHTML;
      
      DOM.id("btn-upload-file").insertAdjacentHTML("afterend", `<button id="btn-embed-file" disabled>Embed File</button>`);
      DOM.id("btn-embed-file").addEventListener("click", () => downloadTextFile("embed.html", generated));
    },
    
    onFileRead: function(json){
      if (!enabled){
        return;
      }
      
      DOM.id("btn-embed-file").disabled = false;
      generated = html.replace("</title>", `</title>\n<script type="text/javascript">window.DHT_EMBEDDED = "${utoa(json)}";<\/script>`).replace(`<${document.body.tagName.toLowerCase()}>`, `<body class="embedded">`);
    },
    
    getEmbeddedJSON: function(){
      var embed = window.DHT_EMBEDDED;
      return embed ? atou(embed) : null;
    }
  };
})();
