import state from "./state.mjs";

const servers = (() => {
    let currentServerId = "0";

    function getIcon(name) {
        return name.split(" ").map(word => {
            if (word.startsWith("[") && word.length > 1) return word[1];
            return word[0] || "";
        }).join("");
    }

    function update() {
        const channels = document.querySelectorAll("#channels .channel");
        const serversMap = new Map();

        // Check if there are any channels with server-id 0 (DM)
        const hasDMChannels = Array.from(channels).some(channel => {
            return channel.getAttribute("server-id") === "0";
        });

        if (hasDMChannels) {
            serversMap.set("0", {
                id: "0",
                name: "DM",
                icon: "DM",
            });
        }

        channels.forEach(channel => {
            const serverId = channel.getAttribute("server-id") || "0";
            const serverType = channel.getAttribute("server-type");
            const serverName = channel.getAttribute("server-name");

            if (serverType === "server" && !serversMap.has(serverId)) {
                serversMap.set(serverId, {
                    id: serverId,
                    name: serverName,
                    icon: getIcon(serverName),
                });
            }
        });

        const serversDiv = document.getElementById("servers");
        serversDiv.innerHTML = "";

        if (hasDMChannels) {
            const dmServer = serversMap.get("0");
            const dmElement = document.createElement("div");
            dmElement.className = `Server${dmServer.id === currentServerId ? " active" : ""}`;
            dmElement.id = "DM";
            dmElement.dataset.serverId = dmServer.id;
            dmElement.innerHTML = `
                <div class="icon">DM</div>
                <div class="name" title="Direct Messages">Direct Messages</div>
            `;
            dmElement.addEventListener("click", () => selectServer(dmServer.id));
            serversDiv.appendChild(dmElement);
        }

        serversMap.forEach(server => {
            if (server.id === "0") return; // Skip DM since it's already added

            const serverElement = document.createElement("div");
            serverElement.className = `Server${server.id === currentServerId ? " active" : ""}`;
            serverElement.dataset.serverId = server.id;
            serverElement.innerHTML = `
                <div class="icon">${server.icon}</div>
                <div class="name" title="${server.name}">${server.name}</div>
            `;
            serverElement.addEventListener("click", () => selectServer(server.id));
            serversDiv.appendChild(serverElement);
        });

        if (!serversMap.has(currentServerId)) {
            currentServerId = "0";
        }

        updateChannelVisibility();
    }

    function selectServer(serverId) {
        // Remove active class from all servers
        document.querySelectorAll("#servers .Server").forEach(server => {
            server.classList.remove("active");
        });

        // Add active class to the selected server
        const selectedServer = document.querySelector(`#servers .Server[data-server-id="${serverId}"]`);
        if (selectedServer) {
            selectedServer.classList.add("active");
        }

        currentServerId = serverId;
        updateChannelVisibility();
        state.selectChannel(null);
    }

    function updateChannelVisibility() {
        document.querySelectorAll("#channels .channel").forEach(channel => {
            const channelServerId = channel.getAttribute("server-id") || "0";
            channel.classList.toggle("visible", channelServerId === currentServerId);
        });
    }

    return { update };
})();

export default servers;