import discord from "./discord.mjs";
import dom from "./dom.mjs";
import template from "./template.mjs";
import settings from "./settings.mjs";
import state from "./state.mjs";

export default (function() {
	const regex = {
		formatBold: /\*\*([\s\S]+?)\*\*(?!\*)/g,
		formatItalic1: /\*([\s\S]+?)\*(?!\*)/g,
		formatItalic2: /_([\s\S]+?)_(?!_)\b/g,
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
	
	const fileUrlProcessor = function(serverToken) {
		if (typeof serverToken === "string") {
			return url => "/get-downloaded-file/" + encodeURIComponent(url) + "?token=" + encodeURIComponent(serverToken);
		}
		else {
			return url => url;
		}
	}(window.DHT_SERVER_TOKEN);
	
	const getEmoji = function(name, id, extension) {
		const tag = ":" + name + ":";
		return "<img src='" + fileUrlProcessor("https://cdn.discordapp.com/emojis/" + id + "." + extension) + "' alt='" + tag + "' title='" + tag + "' class='emoji'>";
	};
	
	const processMessageContents = function(contents) {
		let processed = dom.escapeHTML(contents.replace(regex.formatUrlNoEmbed, "$1"));
		
		if (settings.enableFormatting) {
			const escapeHtmlMatch = (full, match) => "&#" + match.charCodeAt(0) + ";";
			
			processed = processed
				.replace(regex.specialEscapedBacktick, "&#96;")
				.replace(regex.formatCodeBlock, (full, ignore, match) => "<code class='block'>" + match.replace(regex.specialUnescaped, escapeHtmlMatch) + "</code>")
				.replace(regex.formatCodeInline, (full, ignore, match) => "<code class='inline'>" + match.replace(regex.specialUnescaped, escapeHtmlMatch) + "</code>")
				.replace(regex.specialEscapedSingle, escapeHtmlMatch)
				.replace(regex.specialEscapedDouble, full => full.replace(/\\/g, "").replace(/(.)/g, escapeHtmlMatch))
				.replace(regex.formatBold, "<b>$1</b>")
				.replace(regex.formatUnderline, "<u>$1</u>")
				.replace(regex.formatItalic1, "<i>$1</i>")
				.replace(regex.formatItalic2, "<i>$1</i>")
				.replace(regex.formatStrike, "<s>$1</s>");
		}
		
		const animatedEmojiExtension = settings.enableAnimatedEmoji ? "gif" : "webp";
		
		// noinspection HtmlUnknownTarget
		processed = processed
			.replace(regex.formatUrl, "<a href='$1' target='_blank' rel='noreferrer'>$1</a>")
			.replace(regex.mentionChannel, (full, match) => "<span class='link mention-chat'>#" + state.getChannelName(match) + "</span>")
			.replace(regex.mentionUser, (full, match) => "<span class='link mention-user' title='" + state.getUserName(match) + "'>@" + state.getUserDisplayName(match) + "</span>")
			.replace(regex.customEmojiStatic, (full, m1, m2) => getEmoji(m1, m2, "webp"))
			.replace(regex.customEmojiAnimated, (full, m1, m2) => getEmoji(m1, m2, animatedEmojiExtension));
		
		return "<p>" + processed + "</p>";
	};
	
	const getAvatarUrlObject = function(avatar) {
		return { url: fileUrlProcessor("https://cdn.discordapp.com/avatars/" + avatar.id + "/" + avatar.path + ".webp") };
	};
	
	const getImageEmbed = function(url, image) {
		if (!settings.enableImagePreviews) {
			return "";
		}
		
		if (image.width && image.height) {
			return templateEmbedImageWithSize.apply({ url: fileUrlProcessor(url), src: fileUrlProcessor(image.url), width: image.width, height: image.height });
		}
		else {
			return templateEmbedImage.apply({ url: fileUrlProcessor(url), src: fileUrlProcessor(image.url) });
		}
	};
	
	const isImageUrl = function(url) {
		const dot = url.pathname.lastIndexOf(".");
		const ext = dot === -1 ? "" : url.pathname.substring(dot).toLowerCase();
		return ext === ".png" || ext === ".gif" || ext === ".jpg" || ext === ".jpeg";
	};
	
	return {
		setup() {
			templateChannelServer = new template([
				"<div class='channel ServerChannel' data-channel='{id}' server-id='{serverId}' server-name='{server.name}' server-type='{server.type}'>",
				"<div class='info' title='{topic}'><strong class='name'>#{name}</strong>{nsfw}<span class='tag'>{msgcount}</span></div>",
				"<!--<span class='server'>{server.name} ({server.type})</span>-->",
				"</div>"
			].join(""));
			
			templateChannelPrivate = new template([
				"<div class='channel UserChannel' data-channel='{id}' server-id='0' server-name='{server.name}' server-type='{server.type}'>",
				"<div class='avatar'>{icon}</div>",
				"<div class='info'><strong class='name'>{name}</strong><span class='tag'>{msgcount}</span></div>",
				"<!--<span class='server'>{server.name} ({server.type})</span>-->",
				"</div>"
			].join(""));
			
			templateMessageNoAvatar = new template([
				"<div>",
				"<div class='reply-message'>{reply}</div>",
				"<h2><strong class='username' title='{user.name}'>{user.displayName}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
				"<div class='message'>{contents}{embeds}{attachments}</div>",
				"{reactions}",
				"</div>"
			].join(""));
			
			templateMessageWithAvatar = new template([
				"<div>",
				"<div class='reply-message reply-message-with-avatar'>{reply}</div>",
				"<div class='avatar-wrapper'>",
				"<div class='avatar'>{avatar}</div>",
				"<div>",
				"<h2><strong class='username' title='{user.name}'>{user.displayName}</strong><span class='info time'>{timestamp}</span>{edit}{jump}</h2>",
				"<div class='message'>{contents}{embeds}{attachments}</div>",
				"{reactions}",
				"</div>",
				"</div>",
				"</div>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateUserAvatar = new template([
				"<img src='{url}' alt=''>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateAttachmentDownload = new template([
				"<a href='{url}' class='embed download'>Download {name}</a>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedImage = new template([
				"<a href='{url}' class='embed thumbnail loading'><img src='{src}' alt='' onload='window.DISCORD.handleImageLoad(this)' onerror='window.DISCORD.handleImageLoadError(this)'></a><br>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedImageWithSize = new template([
				"<a href='{url}' class='embed thumbnail loading'><img src='{src}' width='{width}' alt='' onload='window.DISCORD.handleImageLoad(this)' onerror='window.DISCORD.handleImageLoadError(this)'></a><br>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedRich = new template([
				"<div class='embed download'><a href='{url}' class='title'>{title}</a><p class='desc'>{description}</p></div>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedRichNoDescription = new template([
				"<div class='embed download'><a href='{url}' class='title'>{title}</a></div>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateEmbedUrl = new template([
				"<a href='{url}' class='embed download'>{url}</a>"
			].join(""));
			
			templateEmbedUnsupported = new template([
				"<div class='embed download'><p>(Unsupported embed)</p></div>"
			].join(""));
			
			templateReaction = new template([
				"<span class='reaction-wrapper'><span class='reaction-emoji'>{n}</span><span class='count'>{c}</span></span>"
			].join(""));
			
			// noinspection HtmlUnknownTarget
			templateReactionCustom = new template([
				"<span class='reaction-wrapper'><img src='{url}' alt=':{n}:' title=':{n}:' class='reaction-emoji-custom'><span class='count'>{c}</span></span>"
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
			const url = dom.tryParseUrl(attachment.url);
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
			return (settings.enableUserAvatars ? templateMessageWithAvatar : templateMessageNoAvatar).apply(message, (property, value) => {
				if (property === "avatar") {
					return value ? templateUserAvatar.apply(getAvatarUrlObject(value)) : "";
				}
				else if (property === "user.displayName") {
					return value ? value : message.user.name;
				}
				else if (property === "timestamp") {
					return dom.getHumanReadableTime(value);
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
						const url = fileUrlProcessor(attachment.url);
						
						if (!discord.isImageAttachment(attachment) || !settings.enableImagePreviews) {
							return templateAttachmentDownload.apply({ url, name: attachment.name });
						}
						else if ("width" in attachment && "height" in attachment) {
							return templateEmbedImageWithSize.apply({ url, src: url, width: attachment.width, height: attachment.height });
						}
						else {
							return templateEmbedImage.apply({ url, src: url });
						}
					}).join("");
				}
				else if (property === "edit") {
					return value ? "<span class='info edited'>Edited " + dom.getHumanReadableTime(value) + "</span>" : "";
				}
				else if (property === "jump") {
					return state.hasActiveFilter ? "<span class='info jump' data-jump='" + value + "'>Jump to message</span>" : "";
				}
				else if (property === "reply") {
					if (!value) {
						return value === null ? "<span class='reply-contents reply-missing'>(replies to an unknown message)</span>" : "";
					}
					
					const user = "<span class='reply-username' title='" + value.user.name + "'>" + (value.user.displayName ?? value.user.name) + "</span>";
					const avatar = settings.enableUserAvatars && value.avatar ? "<span class='reply-avatar'>" + templateUserAvatar.apply(getAvatarUrlObject(value.avatar)) + "</span>" : "";
					const contents = value.contents ? "<span class='reply-contents'>" + processMessageContents(value.contents) + "</span>" : "";
					
					return "<span class='jump' data-jump='" + value.id + "'>Jump to reply</span><span class='user'>" + avatar + user + "</span>" + contents;
				}
				else if (property === "reactions") {
					if (!value) {
						return "";
					}
					
					return "<div class='reactions'>" + value.map(reaction => {
						if ("id" in reaction) {
							const ext = reaction.a && settings.enableAnimatedEmoji ? "gif" : "webp";
							const url = fileUrlProcessor("https://cdn.discordapp.com/emojis/" + reaction.id + "." + ext);
							// noinspection JSUnusedGlobalSymbols
							return templateReactionCustom.apply({ url, n: reaction.n, c: reaction.c });
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
