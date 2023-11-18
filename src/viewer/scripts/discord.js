const DISCORD = (function() {
	const regex = {
		formatBold: /\*\*([\s\S]+?)\*\*(?!\*)/g,
		formatItalic: /(.)?([_*])([\s\S]+?)\2(?!\2)/g,
		formatUnderline: /__([\s\S]+?)__(?!_)/g,
		formatStrike: /~~([\s\S]+?)~~(?!~)/g,
		formatCodeInline: /(`+)\s*([\s\S]*?[^`])\s*\1(?!`)/g,
		formatCodeBlock: /```(?:([A-z0-9\-]+?)\n+)?\n*([^]+?)\n*```/g,
		formatUrl: /(\b(?:https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])/ig,
		formatUrlNoEmbed: /<(\b(?:https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])>/ig,
		specialEscapedBacktick: /\\`/g,
		specialEscapedSingle: /\\([*_\\])/g,
		specialEscapedDouble: /\\__|_\\_|\\_\\_|\\~~|~\\~|\\~\\~/g,
		specialUnescaped: /([*_~\\])/g,
		mentionRole: /&lt;@&(\d+?)&gt;/g,
		mentionUser: /&lt;@!?(\d+?)&gt;/g,
		mentionChannel: /&lt;#(\d+?)&gt;/g,
		customEmojiStatic: /&lt;:([^:]+):(\d+?)&gt;/g,
		customEmojiAnimated: /&lt;a:([^:]+):(\d+?)&gt;/g
	};
	
	let templateChannelServer;
	let templateChannelPrivate;
	let templateMessageNoAvatar;
	let templateMessageWithAvatar;
	let templateUserAvatar;
	let templateAttachmentDownload;
	let templateEmbedImage;
	let templateEmbedImageWithSize;
	let templateEmbedRich;
	let templateEmbedRichNoDescription;
	let templateEmbedUrl;
	let templateEmbedUnsupported;
	let templateReaction;
	let templateReactionCustom;
	
	const processMessageContents = function(contents) {
		let processed = DOM.escapeHTML(contents.replace(regex.formatUrlNoEmbed, "$1"));
		
		if (SETTINGS.enableFormatting) {
			const escapeHtmlMatch = (full, match) => "&#" + match.charCodeAt(0) + ";";
			
			processed = processed
				.replace(regex.specialEscapedBacktick, "&#96;")
				.replace(regex.formatCodeBlock, (full, ignore, match) => "<code class='block'>" + match.replace(regex.specialUnescaped, escapeHtmlMatch) + "</code>")
				.replace(regex.formatCodeInline, (full, ignore, match) => "<code class='inline'>" + match.replace(regex.specialUnescaped, escapeHtmlMatch) + "</code>")
				.replace(regex.specialEscapedSingle, escapeHtmlMatch)
				.replace(regex.specialEscapedDouble, full => full.replace(/\\/g, "").replace(/(.)/g, escapeHtmlMatch))
				.replace(regex.formatBold, "<b>$1</b>")
				.replace(regex.formatUnderline, "<u>$1</u>")
				.replace(regex.formatItalic, (full, pre, char, match) => pre === "\\" ? full : (pre || "") + "<i>" + match + "</i>")
				.replace(regex.formatStrike, "<s>$1</s>");
		}
		
		const animatedEmojiExtension = SETTINGS.enableAnimatedEmoji ? "gif" : "png";
		
		// noinspection HtmlUnknownTarget
		processed = processed
			.replace(regex.formatUrl, "<a href='$1' target='_blank' rel='noreferrer'>$1</a>")
			.replace(regex.mentionChannel, (full, match) => "<span class='link mention-chat'>#" + STATE.getChannelName(match) + "</span>")
			.replace(regex.mentionUser, (full, match) => "<span class='link mention-user' title='#" + (STATE.getUserTag(match) || "????") + "'>@" + STATE.getUserName(match) + "</span>")
			.replace(regex.customEmojiStatic, "<img src='https://cdn.discordapp.com/emojis/$2.png' alt=':$1:' title=':$1:' class='emoji'>")
			.replace(regex.customEmojiAnimated, "<img src='https://cdn.discordapp.com/emojis/$2." + animatedEmojiExtension + "' alt=':$1:' title=':$1:' class='emoji'>");
		
		return "<p>" + processed + "</p>";
	};
	
	const getImageEmbed = function(url, image) {
		if (!SETTINGS.enableImagePreviews) {
			return "";
		}
		
		if (image.width && image.height) {
			return templateEmbedImageWithSize.apply({ url, src: image.url, width: image.width, height: image.height });
		}
		else {
			return templateEmbedImage.apply({ url, src: image.url });
		}
	};
	
	const isImageUrl = function(url) {
		const dot = url.pathname.lastIndexOf(".");
		const ext = dot === -1 ? "" : url.pathname.substring(dot).toLowerCase();
		return ext === ".png" || ext === ".gif" || ext === ".jpg" || ext === ".jpeg";
	};
	
	return {
		setup() {
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
				"<div class='reply-message'>{reply}</div>",
				"<h2><strong class='username' title='#{user.tag}'>{user.name}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
				"<div class='message'>{contents}{embeds}{attachments}</div>",
				"{reactions}",
				"</div>"
			].join(""));
			
			templateMessageWithAvatar = new TEMPLATE([
				"<div>",
				"<div class='reply-message reply-message-with-avatar'>{reply}</div>",
				"<div class='avatar-wrapper'>",
				"<div class='avatar'>{avatar}</div>",
				"<div>",
				"<h2><strong class='username' title='#{user.tag}'>{user.name}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
				"<div class='message'>{contents}{embeds}{attachments}</div>",
				"{reactions}",
				"</div>",
				"</div>",
				"</div>"
			].join(""));
			
			templateUserAvatar = new TEMPLATE([
				"<img src='https://cdn.discordapp.com/avatars/{id}/{path}.webp?size=128' alt=''>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateAttachmentDownload = new TEMPLATE([
				"<a href='{url}' class='embed download'>Download {name}</a>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedImage = new TEMPLATE([
				"<a href='{url}' class='embed thumbnail loading'><img src='{src}' alt='' onload='DISCORD.handleImageLoad(this)' onerror='DISCORD.handleImageLoadError(this)'></a><br>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedImageWithSize = new TEMPLATE([
				"<a href='{url}' class='embed thumbnail loading'><img src='{src}' width='{width}' height='{height}' alt='' onload='DISCORD.handleImageLoad(this)' onerror='DISCORD.handleImageLoadError(this)'></a><br>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedRich = new TEMPLATE([
				"<div class='embed download'><a href='{url}' class='title'>{title}</a><p class='desc'>{description}</p></div>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedRichNoDescription = new TEMPLATE([
				"<div class='embed download'><a href='{url}' class='title'>{title}</a></div>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedUrl = new TEMPLATE([
				"<a href='{url}' class='embed download'>{url}</a>"
			].join(""));
			
			templateEmbedUnsupported = new TEMPLATE([
				"<div class='embed download'><p>(Unsupported embed)</p></div>"
			].join(""));
			
			templateReaction = new TEMPLATE([
				"<span class='reaction-wrapper'><span class='reaction-emoji'>{n}</span><span class='count'>{c}</span></span>"
			].join(""));
			
			templateReactionCustom = new TEMPLATE([
				"<span class='reaction-wrapper'><img src='https://cdn.discordapp.com/emojis/{id}.{ext}' alt=':{n}:' title=':{n}:' class='reaction-emoji-custom'><span class='count'>{c}</span></span>"
			].join(""));
		},
		
		handleImageLoad(ele) {
			ele.parentElement.classList.remove("loading");
		},
		
		handleImageLoadError(ele) {
			// noinspection JSUnusedGlobalSymbols
			ele.onerror = null;
			ele.parentElement.classList.remove("loading");
			ele.setAttribute("alt", "(image attachment not found)");
		},
		
		isImageAttachment(attachment) {
			const url = DOM.tryParseUrl(attachment.url);
			return url != null && isImageUrl(url);
		},
		
		getChannelHTML(channel) { // noinspection FunctionWithInconsistentReturnsJS
			return (channel.server.type === "server" ? templateChannelServer : templateChannelPrivate).apply(channel, (property, value) => {
				if (property === "nsfw") {
					return value ? "<span class='tag'>NSFW</span>" : "";
				}
			});
		},
		
		getMessageHTML(message) { // noinspection FunctionWithInconsistentReturnsJS
			return (SETTINGS.enableUserAvatars ? templateMessageWithAvatar : templateMessageNoAvatar).apply(message, (property, value) => {
				if (property === "avatar") {
					return value ? templateUserAvatar.apply(value) : "";
				}
				else if (property === "user.tag") {
					return value ? value : "????";
				}
				else if (property === "timestamp") {
					return DOM.getHumanReadableTime(value);
				}
				else if (property === "contents") {
					return value && value.length > 0 ? processMessageContents(value) : "";
				}
				else if (property === "embeds") {
					if (!value) {
						return "";
					}
					
					return value.map(embed => {
						if (!embed.url) {
							return templateEmbedUnsupported.apply(embed);
						}
						else if ("image" in embed && embed.image.url) {
							return getImageEmbed(embed.url, embed.image);
						}
						else if ("thumbnail" in embed && embed.thumbnail.url) {
							return getImageEmbed(embed.url, embed.thumbnail);
						}
						else if ("title" in embed && "description" in embed) {
							return templateEmbedRich.apply(embed);
						}
						else if ("title" in embed) {
							return templateEmbedRichNoDescription.apply(embed);
						}
						else {
							return templateEmbedUrl.apply(embed);
						}
					}).join("");
				}
				else if (property === "attachments") {
					if (!value) {
						return "";
					}
					
					return value.map(attachment => {
						if (!DISCORD.isImageAttachment(attachment) || !SETTINGS.enableImagePreviews) {
							return templateAttachmentDownload.apply(attachment);
						}
						else if ("width" in attachment && "height" in attachment) {
							return templateEmbedImageWithSize.apply({ url: attachment.url, src: attachment.url, width: attachment.width, height: attachment.height });
						}
						else {
							return templateEmbedImage.apply({ url: attachment.url, src: attachment.url });
						}
					}).join("");
				}
				else if (property === "edit") {
					return value ? "<span class='info edited'>Edited " + DOM.getHumanReadableTime(value) + "</span>" : "";
				}
				else if (property === "jump") {
					return STATE.hasActiveFilter ? "<span class='info jump' data-jump='" + value + "'>Jump to message</span>" : "";
				}
				else if (property === "reply") {
					if (!value) {
						return value === null ? "<span class='reply-contents reply-missing'>(replies to an unknown message)</span>" : "";
					}
					
					const user = "<span class='reply-username' title='#" + (value.user.tag ? value.user.tag : "????") + "'>" + value.user.name + "</span>";
					const avatar = SETTINGS.enableUserAvatars && value.avatar ? "<span class='reply-avatar'>" + templateUserAvatar.apply(value.avatar) + "</span>" : "";
					const contents = value.contents ? "<span class='reply-contents'>" + processMessageContents(value.contents) + "</span>" : "";
					
					return "<span class='jump' data-jump='" + value.id + "'>Jump to reply</span><span class='user'>" + avatar + user + "</span>" + contents;
				}
				else if (property === "reactions"){
					if (!value){
						return "";
					}
					
					return "<div class='reactions'>" + value.map(reaction => {
						if ("id" in reaction){
							// noinspection JSUnusedGlobalSymbols, JSUnresolvedVariable
							reaction.ext = reaction.a && SETTINGS.enableAnimatedEmoji ? "gif" : "png";
							return templateReactionCustom.apply(reaction);
						}
						else {
							return templateReaction.apply(reaction);
						}
					}).join("") + "</div>";
				}
			});
		}
	};
})();
