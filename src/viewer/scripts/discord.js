var DISCORD = (function(){
  var REGEX = {
    formatBold: /\*\*([\s\S]+?)\*\*(?!\*)/g,
    formatItalic: /(.)?\*([\s\S]+?)\*(?!\*)/g,
    formatUnderline: /__([\s\S]+?)__(?!_)/g,
    formatStrike: /~~([\s\S]+?)~~(?!~)/g,
    formatCodeInline: /(`+)\s*([\s\S]*?[^`])\s*\1(?!`)/g,
    formatCodeBlock: /```(?:([A-z0-9\-]+?)\n+)?\n*([^]+?)\n*```/g,
    formatUrl: /(\b(?:https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])/ig,
    formatUrlNoEmbed: /<(\b(?:https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])>/ig,
    specialEscapedBacktick: /\\`/g,
    specialEscapedSingle: /\\([*\\])/g,
    specialEscapedDouble: /\\__|_\\_|\\_\\_|\\~~|~\\~|\\~\\~/g,
    specialUnescaped: /([*_~\\])/g,
    mentionRole: /&lt;@&(\d+?)&gt;/g,
    mentionUser: /&lt;@!?(\d+?)&gt;/g,
    mentionChannel: /&lt;#(\d+?)&gt;/g,
    customEmojiStatic: /&lt;:([^:]+):(\d+?)&gt;/g,
    customEmojiAnimated: /&lt;a:([^:]+):(\d+?)&gt;/g
  };
  
  var isImageAttachment = function(attachment){
    var dot = attachment.url.lastIndexOf(".");
    var ext = dot === -1 ? "" : attachment.url.substring(dot).toLowerCase();
    return ext === ".png" || ext === ".gif" || ext === ".jpg" || ext === ".jpeg";
  };
  
  var getHumanReadableTime = function(timestamp){
    var date = new Date(timestamp);
    return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
  };
  
  var templateChannelServer;
  var templateChannelPrivate;
  var templateMessageNoAvatar;
  var templateMessageWithAvatar;
  var templateUserAvatar;
  var templateEmbedImage;
  var templateEmbedRich;
  var templateEmbedRichNoDescription;
  var templateEmbedRichUnsupported;
  var templateEmbedDownload;
  
  return {
    setup: function(){
      templateChannelServer = new TEMPLATE([
        "<div data-channel='{id}'>",
        "<div class='info' title='{topic}'><strong class='name'>#{name}</strong>{nsfw}<span class='tag'>{msgcount}</span></div>",
        "<span class='server'>{server.name} ({server.type})</span>",
        "</div>"
      ].join(""));
      
      templateChannelPrivate = new TEMPLATE([
        "<div data-channel='{id}'>",
        "<div class='info'><strong class='name'>{name}</strong><span class='tag'>{msgcount}</span></div>",
        "<span class='server'>({server.type})</span>",
        "</div>"
      ].join(""));
      
      templateMessageNoAvatar = new TEMPLATE([
        "<div>",
        "<h2><strong class='username' title='#{user.tag}'>{user.name}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
        "<div class='message'>{contents}{embeds}{attachments}</div>",
        "</div>"
      ].join(""));
      
      templateMessageWithAvatar = new TEMPLATE([
        "<div class='avatar-wrapper'>",
        "<div class='avatar'>{avatar}</div>",
        "<div>",
        "<h2><strong class='username' title='#{user.tag}'>{user.name}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
        "<div class='message'>{contents}{embeds}{attachments}</div>",
        "</div>",
        "</div>"
      ].join(""));
      
      templateUserAvatar = new TEMPLATE([
        "<img src='https://cdn.discordapp.com/avatars/{id}/{path}.webp?size=128'>"
      ].join(""));
      
      templateEmbedImage = new TEMPLATE([
        "<a href='{url}' class='embed thumbnail'><img src='{url}' alt='(image attachment not found)'></a><br>"
      ].join(""));
      
      templateEmbedRich = new TEMPLATE([
        "<div class='embed download'><a href='{url}' class='title'>{t}</a><p class='desc'>{d}</p></div>"
      ].join(""));
      
      templateEmbedRichNoDescription = new TEMPLATE([
        "<div class='embed download'><a href='{url}' class='title'>{t}</a></div>"
      ].join(""));
      
      templateEmbedRichUnsupported = new TEMPLATE([
        "<div class='embed download'><p>(Formatted embeds are currently not supported)</p></div>"
      ].join(""));
      
      templateEmbedDownload = new TEMPLATE([
        "<a href='{url}' class='embed download'>Download {filename}</a>"
      ].join(""));
    },
    
    isImageAttachment: isImageAttachment,
    
    getChannelHTML: function(channel){
      return (channel.server.type === "SERVER" ? templateChannelServer : templateChannelPrivate).apply(channel, (property, value) => {
        if (property === "server.type"){
          switch(value){
            case "SERVER": return "server";
            case "GROUP": return "group";
            case "DM": return "user";
          }
        }
        else if (property === "nsfw"){
          return value ? "<span class='tag'>NSFW</span>" : "";
        }
      });
    },
    
    getMessageHTML: function(message){
      return (STATE.settings.enableUserAvatars ? templateMessageWithAvatar : templateMessageNoAvatar).apply(message, (property, value) => {
        if (property === "avatar"){
          return value ? templateUserAvatar.apply(value) : "";
        }
        else if (property === "user.tag"){
          return value ? value : "????";
        }
        else if (property === "timestamp"){
          return getHumanReadableTime(value);
        }
        else if (property === "contents"){
          if (value == null || value.length === 0){
            return "";
          }
          
          var processed = DOM.escapeHTML(value.replace(REGEX.formatUrlNoEmbed, "$1"));
          
          if (STATE.settings.enableFormatting){
            var escapeHtmlMatch = (full, match) => "&#"+match.charCodeAt(0)+";";
            
            processed = processed
              .replace(REGEX.specialEscapedBacktick, "&#96;")
              .replace(REGEX.formatCodeBlock, (full, ignore, match) => "<code class='block'>"+match.replace(REGEX.specialUnescaped, escapeHtmlMatch)+"</code>")
              .replace(REGEX.formatCodeInline, (full, ignore, match) => "<code class='inline'>"+match.replace(REGEX.specialUnescaped, escapeHtmlMatch)+"</code>")
              .replace(REGEX.specialEscapedSingle, escapeHtmlMatch)
              .replace(REGEX.specialEscapedDouble, full => full.replace(/\\/g, "").replace(/(.)/g, escapeHtmlMatch))
              .replace(REGEX.formatBold, "<b>$1</b>")
              .replace(REGEX.formatItalic, (full, pre, match) => pre === '\\' ? full : (pre || "")+"<i>"+match+"</i>")
              .replace(REGEX.formatUnderline, "<u>$1</u>")
              .replace(REGEX.formatStrike, "<s>$1</s>");
          }
          
          var animatedEmojiExtension = STATE.settings.enableAnimatedEmoji ? "gif" : "png";
          
          processed = processed
            .replace(REGEX.formatUrl, "<a href='$1' target='_blank' rel='noreferrer'>$1</a>")
            .replace(REGEX.mentionChannel, (full, match) => "<span class='link mention-chat'>#"+STATE.getChannelName(match)+"</span>")
            .replace(REGEX.mentionUser, (full, match) => "<span class='link mention-user' title='#"+(STATE.getUserTag(match) || "????")+"'>@"+STATE.getUserName(match)+"</span>")
            .replace(REGEX.customEmojiStatic, "<img src='https://cdn.discordapp.com/emojis/$2.png' alt=':$1:' title=':$1:' class='emoji'>")
            .replace(REGEX.customEmojiAnimated, "<img src='https://cdn.discordapp.com/emojis/$2."+animatedEmojiExtension+"' alt=':$1:' title=':$1:' class='emoji'>");
          
          return "<p>"+processed+"</p>";
        }
        else if (property === "embeds"){
          if (!value){
            return "";
          }
          
          return value.map(embed => {
            switch(embed.type){
              case "image":
                return STATE.settings.enableImagePreviews ? templateEmbedImage.apply(embed) : "";
                
              case "rich":
                return (embed.t ? (embed.d ? templateEmbedRich : templateEmbedRichNoDescription) : templateEmbedRichUnsupported).apply(embed);
            }
          }).join("");
        }
        else if (property === "attachments"){
          if (!value){
            return "";
          }
          
          return value.map(attachment => {
            if (isImageAttachment(attachment) && STATE.settings.enableImagePreviews){
              return templateEmbedImage.apply(attachment);
            }
            else{
              var sliced = attachment.url.split("/");

              return templateEmbedDownload.apply({
                "url": attachment.url,
                "filename": sliced[sliced.length-1]
              });
            }
          }).join("");
        }
        else if (property === "edit"){
          return value ? "<span class='info edited'>Edited" + (value > 1 ? " " + getHumanReadableTime(value) : "") + "</span>" : "";
        }
        else if (property === "jump"){
          return STATE.hasActiveFilter ? "<span class='info jump' data-jump='" + value + "'>Jump to message</span>" : "";
        }
      });
    }
  };
})();
