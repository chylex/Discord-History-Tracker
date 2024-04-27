/**
 * @name DiscordGuild
 * @property {String} id
 * @property {String} name
 */

/**
 * @name DiscordChannel
 * @property {String} id
 * @property {String} name
 * @property {Number} type
 * @property {String} [guild_id]
 * @property {String} [parent_id]
 * @property {Number} [position]
 * @property {String} [topic]
 * @property {Boolean} [nsfw]
 * @property {DiscordUser[]} [rawRecipients]
 */

/**
 * @name DiscordUser
 * @property {String} id
 * @property {String} username
 * @property {String} discriminator
 * @property {String} [globalName]
 * @property {String} [avatar]
 * @property {Boolean} [bot]
 */

/**
 * @name DiscordMessage
 * @property {String} id
 * @property {String} channel_id
 * @property {DiscordUser} author
 * @property {String} content
 * @property {Date} timestamp
 * @property {Date|null} editedTimestamp
 * @property {DiscordAttachment[]} attachments
 * @property {Object[]} embeds
 * @property {DiscordMessageReaction[]} [reactions]
 * @property {DiscordMessageReference} [messageReference]
 * @property {Number} type
 * @property {String} state
 */

/**
 * @name DiscordAttachment
 * @property {String} id
 * @property {String} filename
 * @property {String} [content_type]
 * @property {String} size
 * @property {String} url
 */

/**
 * @name DiscordMessageReaction
 * @property {DiscordEmoji} emoji
 * @property {Number} count
 */

/**
 * @name DiscordMessageReference
 * @property {String} [message_id]
 */

/**
 * @name DiscordEmoji
 * @property {String|null} id
 * @property {String|null} name
 * @property {Boolean} animated
 */

/**
 * @name MessageData
 * @type {Object}
 * @property {Boolean} ready
 * @property {Boolean} loadingMore
 * @property {Boolean} hasMoreAfter
 * @property {Boolean} hasMoreBefore
 * @property {Array<DiscordMessage>} _array
 */
